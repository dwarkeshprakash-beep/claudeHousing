# 03 — Backend Build Prompt (.NET 8 + Dapper + Node.js Microservice)

## Stack

| Concern | Choice |
|---------|--------|
| Framework | ASP.NET Core 8 Minimal API + Controllers |
| DB Access | Dapper + Npgsql (raw SQL, no EF Core) |
| Database | PostgreSQL 16 on Supabase |
| Auth | JWT HS256, 7-day expiry (simple), refresh token in DB |
| Validation | Data Annotations + manual checks |
| Testing | xUnit (unit only, no Testcontainers) |
| Notification svc | Node.js Express (separate service) |
| Hosting | Railway or Render (free tier) |

**Why Dapper over EF Core**: Full SQL control, great interview talking point, simpler migrations (just run SQL files), faster for complex search queries.

---

## Solution Structure

```
backend/
├── ApnaNest.sln
├── src/
│   ├── ApnaNest.API/           # Web layer (Controllers, Middleware, Program.cs)
│   ├── ApnaNest.Services/      # Business logic (PropertyService, UserService, etc.)
│   └── ApnaNest.Data/          # DB access (Repositories using Dapper)
├── tests/
│   └── ApnaNest.Tests/         # xUnit unit tests
└── services/
    └── notification-svc/       # Node.js Express email service
```

---

## Setup Commands

```bash
# Create solution
dotnet new sln -n ApnaNest
dotnet new webapi -n ApnaNest.API     -o src/ApnaNest.API
dotnet new classlib -n ApnaNest.Services -o src/ApnaNest.Services
dotnet new classlib -n ApnaNest.Data     -o src/ApnaNest.Data
dotnet new xunit -n ApnaNest.Tests    -o tests/ApnaNest.Tests

dotnet sln add src/ApnaNest.API/ApnaNest.API.csproj
dotnet sln add src/ApnaNest.Services/ApnaNest.Services.csproj
dotnet sln add src/ApnaNest.Data/ApnaNest.Data.csproj
dotnet sln add tests/ApnaNest.Tests/ApnaNest.Tests.csproj

# Add packages
cd src/ApnaNest.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

cd src/ApnaNest.Data
dotnet add package Dapper
dotnet add package Npgsql

cd tests/ApnaNest.Tests
dotnet add package Moq
dotnet add package FluentAssertions
```

---

## Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(o => o.AddPolicy("AllowFrontend", p =>
    p.WithOrigins(builder.Configuration["AllowedOrigins"]!.Split(','))
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// DB connection factory
builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

// Repositories
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();

// Services
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILeadService, LeadService>();

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => {
        o.TokenValidationParameters = new() {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Global error middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
// Request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();
app.Run();
```

---

## DB Connection Factory

```csharp
// ApnaNest.Data/Infrastructure/NpgsqlConnectionFactory.cs
public interface IDbConnectionFactory {
    IDbConnection Create();
}
public class NpgsqlConnectionFactory(IConfiguration config) : IDbConnectionFactory {
    public IDbConnection Create() => 
        new NpgsqlConnection(config.GetConnectionString("Default"));
}
```

---

## Property Repository (Dapper)

```csharp
// ApnaNest.Data/Repositories/PropertyRepository.cs
public class PropertyRepository(IDbConnectionFactory db) : IPropertyRepository {

    public async Task<PropertyEntity?> GetByIdAsync(Guid id) {
        using var conn = db.Create();
        return await conn.QuerySingleOrDefaultAsync<PropertyEntity>(
            "SELECT * FROM properties WHERE id = @id AND deleted_at IS NULL", new { id });
    }

    public async Task<(IEnumerable<PropertyEntity> Items, int Total)> SearchAsync(PropertySearchFilter f) {
        using var conn = db.Create();
        var where = new List<string> { "p.deleted_at IS NULL", "p.is_active = TRUE" };
        var p = new DynamicParameters();

        if (f.CityId.HasValue)   { where.Add("p.city_id = @cityId");   p.Add("cityId", f.CityId); }
        if (f.LocalityId.HasValue){ where.Add("p.locality_id = @localityId"); p.Add("localityId", f.LocalityId); }
        if (f.Bhk.HasValue)      { where.Add("p.bhk = @bhk");          p.Add("bhk", f.Bhk); }
        if (f.MinPrice.HasValue) { where.Add("p.price >= @minPrice");   p.Add("minPrice", f.MinPrice); }
        if (f.MaxPrice.HasValue) { where.Add("p.price <= @maxPrice");   p.Add("maxPrice", f.MaxPrice); }
        if (f.TypeId.HasValue)   { where.Add("p.property_type_id = @typeId"); p.Add("typeId", f.TypeId); }
        if (f.IntentId.HasValue) { where.Add("p.listing_intent_id = @intentId"); p.Add("intentId", f.IntentId); }
        if (!string.IsNullOrEmpty(f.Query)) {
            where.Add("(p.title ILIKE @q OR p.description ILIKE @q)");
            p.Add("q", $"%{f.Query}%");
        }

        var whereClause = string.Join(" AND ", where);
        var offset = (f.Page - 1) * f.PageSize;
        p.Add("limit", f.PageSize); p.Add("offset", offset);

        var countSql = $"SELECT COUNT(*) FROM properties p WHERE {whereClause}";
        var dataSql  = $@"SELECT p.*, l.name locality_name, c.name city_name 
                          FROM properties p
                          JOIN localities l ON l.id = p.locality_id
                          JOIN cities c ON c.id = p.city_id
                          WHERE {whereClause}
                          ORDER BY p.is_featured DESC, p.created_at DESC
                          LIMIT @limit OFFSET @offset";

        var total = await conn.ExecuteScalarAsync<int>(countSql, p);
        var items = await conn.QueryAsync<PropertyEntity>(dataSql, p);
        return (items, total);
    }

    public async Task<Guid> CreateAsync(PropertyEntity prop) {
        using var conn = db.Create();
        var sql = @"INSERT INTO properties 
            (owner_id,city_id,locality_id,property_type_id,listing_intent_id,title,description,
             bhk,bathrooms,area_sqft,floor_number,total_floors,price,price_per_night,
             furnishing_status_id,possession_status_id,available_from,is_rera_verified,
             rera_number,is_zero_brokerage,latitude,longitude,pin_code,address_line)
            VALUES (@OwnerId,@CityId,@LocalityId,@PropertyTypeId,@ListingIntentId,@Title,@Description,
             @Bhk,@Bathrooms,@AreaSqft,@FloorNumber,@TotalFloors,@Price,@PricePerNight,
             @FurnishingStatusId,@PossessionStatusId,@AvailableFrom,@IsReraVerified,
             @ReraNumber,@IsZeroBrokerage,@Latitude,@Longitude,@PinCode,@AddressLine)
            RETURNING id";
        return await conn.ExecuteScalarAsync<Guid>(sql, prop);
    }

    public async Task SoftDeleteAsync(Guid id, Guid ownerId) {
        using var conn = db.Create();
        await conn.ExecuteAsync(
            "UPDATE properties SET deleted_at = NOW() WHERE id = @id AND owner_id = @ownerId",
            new { id, ownerId });
    }
}
```

---

## Auth Service

```csharp
public class AuthService(IUserRepository users, IConfiguration config) : IAuthService {

    // Dev mode: accept any 6-digit code
    public async Task<bool> VerifyOtpAsync(string phone, string code) {
        // TODO production: validate against otp_codes table
        // Dev: any 6-digit numeric string is valid
        return code.Length == 6 && code.All(char.IsDigit);
    }

    public string GenerateJwt(UserEntity user) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.MobilePhone, user.Phone),
            new Claim(ClaimTypes.Role, user.RoleId.ToString())
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),  // simple 7-day JWT
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## Middleware

```csharp
// Global error handler — no try/catch needed in controllers
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger) {
    public async Task InvokeAsync(HttpContext ctx) {
        try { await next(ctx); }
        catch (Exception ex) {
            logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { error = "Internal server error" });
        }
    }
}

// Request logging — prints method, path, status, duration
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger) {
    public async Task InvokeAsync(HttpContext ctx) {
        var sw = Stopwatch.StartNew();
        await next(ctx);
        logger.LogInformation("{Method} {Path} {Status} {Duration}ms",
            ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
    }
}
```

---

## Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class PropertiesController(IPropertyService svc) : ControllerBase {

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] PropertySearchFilter f) {
        var result = await svc.SearchAsync(f);
        return Ok(new { data = result.Items, total = result.Total, page = f.Page });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) {
        var prop = await svc.GetByIdAsync(id);
        return prop is null ? NotFound() : Ok(prop);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest req) {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var id = await svc.CreateAsync(userId, req);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id) {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await svc.DeleteAsync(id, userId);
        return NoContent();
    }
}

[ApiController] [Route("api/auth")]
public class AuthController(IAuthService auth, IUserRepository users) : ControllerBase {

    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequest req) {
        // Dev: log "OTP: 123456", show "Development in Progress" note
        await users.UpsertByPhoneAsync(req.Phone);
        return Ok(new { message = "OTP sent (dev mode: use any 6 digits)", devMode = true });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req) {
        var valid = await auth.VerifyOtpAsync(req.Phone, req.Code);
        if (!valid) return BadRequest(new { error = "Invalid OTP" });
        var user = await users.GetByPhoneAsync(req.Phone);
        var token = auth.GenerateJwt(user!);
        return Ok(new { token, user = new { user.Id, user.Name, user.RoleId } });
    }
}
```

---

## Full Endpoint Table

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | /api/auth/request-otp | None | Request OTP (dev: any 6 digits) |
| POST | /api/auth/verify-otp | None | Verify OTP, get JWT |
| GET | /api/auth/me | JWT | Current user profile |
| GET | /api/properties | None | Search/list properties |
| GET | /api/properties/{id} | None | Property detail |
| POST | /api/properties | JWT | Create listing |
| PUT | /api/properties/{id} | JWT (owner) | Update listing |
| DELETE | /api/properties/{id} | JWT (owner) | Soft delete listing |
| GET | /api/properties/my | JWT | Owner's own listings |
| POST | /api/properties/{id}/images | JWT (owner) | Upload images |
| DELETE | /api/properties/{id}/images/{imgId} | JWT (owner) | Remove image |
| GET | /api/cities | None | All cities list |
| GET | /api/cities/{slug}/localities | None | Localities in city |
| GET | /api/localities/{id} | None | Locality detail |
| POST | /api/leads | JWT | Submit contact/enquiry |
| GET | /api/leads/received | JWT (owner) | Owner's received leads |
| PUT | /api/leads/{id}/status | JWT (owner) | Update lead status |
| POST | /api/saved | JWT | Save a property |
| DELETE | /api/saved/{propertyId} | JWT | Unsave |
| GET | /api/saved | JWT | My saved properties |
| POST | /api/reviews | JWT | Add review |
| GET | /api/properties/{id}/reviews | None | Property reviews |
| GET | /api/users/profile | JWT | Get profile |
| PUT | /api/users/profile | JWT | Update profile |
| GET | /api/notifications | JWT | My notifications |
| PUT | /api/notifications/{id}/read | JWT | Mark read |
| GET | /api/amenities | None | All amenities |
| GET | /api/lookup/property-types | None | Enum: property types |
| GET | /api/lookup/furnishing | None | Enum: furnishing statuses |
| POST | /api/payments/initiate | JWT | Start payment (send QR email) |
| PUT | /api/payments/{id}/confirm | JWT (admin) | Admin confirms payment |

---

## Node.js Notification Microservice

```
backend/services/notification-svc/
├── index.js
├── routes/notify.js
├── templates/payment-request.html
├── .env
└── package.json
```

```js
// index.js
const express = require('express');
const app = express();
app.use(express.json());
app.use('/notify', require('./routes/notify'));
app.listen(3001, () => console.log('Notification service on :3001'));

// routes/notify.js
const nodemailer = require('nodemailer');
const router = express.Router();

const transporter = nodemailer.createTransport({
  service: 'gmail',
  auth: { user: process.env.GMAIL_USER, pass: process.env.GMAIL_APP_PASSWORD }
});

// POST /notify/email — generic email
router.post('/email', async (req, res) => {
  const { to, subject, html } = req.body;
  await transporter.sendMail({ from: process.env.GMAIL_USER, to, subject, html });
  res.json({ sent: true });
});

// POST /notify/lead — notify owner of new lead
router.post('/lead', async (req, res) => {
  const { ownerEmail, ownerName, propertyTitle, buyerName, buyerPhone, message } = req.body;
  await transporter.sendMail({
    from: `ApnaNest <${process.env.GMAIL_USER}>`,
    to: ownerEmail,
    subject: `New enquiry for "${propertyTitle}"`,
    html: `<p>Hi ${ownerName},</p><p><b>${buyerName}</b> (${buyerPhone}) enquired about your property: <b>${propertyTitle}</b>.</p><p>Message: ${message}</p>`
  });
  res.json({ sent: true });
});

// POST /notify/payment — send payment QR request email
router.post('/payment', async (req, res) => {
  const { email, name, amount, planName, upiId } = req.body;
  // UPI QR deep link — no API needed, standard format
  const upiLink = `upi://pay?pa=${upiId}&pn=ApnaNest&am=${amount}&cu=INR&tn=${planName}`;
  await transporter.sendMail({
    from: `ApnaNest <${process.env.GMAIL_USER}>`,
    to: email,
    subject: `Complete your ApnaNest payment — ₹${amount}`,
    html: `
      <h2>Hi ${name}, complete your payment</h2>
      <p>Plan: ${planName} — ₹${amount}</p>
      <p>Pay via UPI: <b>${upiId}</b></p>
      <p>Or use this link on mobile: <a href="${upiLink}">Pay Now</a></p>
      <p>After payment, reply to this email with your UTR number and we'll activate your account within 2 hours.</p>
    `
  });
  res.json({ sent: true });
});

module.exports = router;
```

```bash
# .env
GMAIL_USER=apnanest.noreply@gmail.com
GMAIL_APP_PASSWORD=xxxx   # Google App Password (not regular password)
```

The .NET API calls this service:
```csharp
// In PaymentService.cs
await httpClient.PostAsJsonAsync("http://notification-svc:3001/notify/payment", new {
    email = user.Email, name = user.Name,
    amount = plan.PriceInr, planName = plan.Label, upiId = "apnanest@upi"
});
```

---

## xUnit Tests — Simple Guide

xUnit tests are just C# methods with `[Fact]` attribute. No setup needed, just:

```csharp
// tests/ApnaNest.Tests/PropertyServiceTests.cs
public class PropertyServiceTests {

    [Fact]
    public async Task SearchAsync_WithCityFilter_ReturnsOnlyCityProperties() {
        // Arrange
        var mockRepo = new Mock<IPropertyRepository>();
        var cityId = Guid.NewGuid();
        mockRepo.Setup(r => r.SearchAsync(It.Is<PropertySearchFilter>(f => f.CityId == cityId)))
                .ReturnsAsync((new[] { new PropertyEntity { CityId = cityId } }, 1));

        var service = new PropertyService(mockRepo.Object);

        // Act
        var result = await service.SearchAsync(new PropertySearchFilter { CityId = cityId });

        // Assert
        result.Total.Should().Be(1);
        result.Items.Should().AllSatisfy(p => p.CityId.Should().Be(cityId));
    }

    [Fact]
    public void GenerateJwt_ValidUser_ReturnsToken() {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("Jwt:Secret", "supersecretkey1234567890123456") })
            .Build();
        var authService = new AuthService(Mock.Of<IUserRepository>(), config);
        var user = new UserEntity { Id = Guid.NewGuid(), Phone = "9876543210", RoleId = 1 };

        // Act
        var token = authService.GenerateJwt(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().Contain(".");  // JWT has 3 parts separated by dots
    }
}
```

Run tests:
```bash
dotnet test
```

Tests are passing when output shows: `Passed! - Failed: 0, Passed: 2`

---

## .env / appsettings

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "Default": "Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require"
  },
  "Jwt": { "Secret": "your-secret-key-minimum-32-characters" },
  "AllowedOrigins": "http://localhost:3000",
  "NotificationService": { "BaseUrl": "http://localhost:3001" }
}
```

---

## GitHub + Deployment

```bash
# Init repo
git init
echo "obj/ bin/ .env *.user appsettings.Development.json" >> .gitignore
git add . && git commit -m "feat: initial .NET backend scaffold"
gh repo create apnanest-backend --public --push

# Deploy to Railway
npm install -g @railway/cli
railway login
railway init      # select project
railway up        # deploys from current folder
# Set env vars in Railway dashboard → Variables tab
```

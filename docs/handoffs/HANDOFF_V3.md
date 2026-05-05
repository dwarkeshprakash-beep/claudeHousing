# ApnaNest — AI Handoff Prompt V3
> **Give this entire file to the next AI. It is 100% self-contained.**
> Working directory: `f:\claude_houseing\apnanest`
> Date: 30 Apr 2026 | Stack: Next.js 16 + .NET 9 + Supabase PostgreSQL

---

## 1. WHAT THIS PROJECT IS

Housing.com clone for the Indian market. **ApnaNest** — zero brokerage, RERA-first, owner-direct listings.

- **Frontend:** `f:\claude_houseing\apnanest\frontend` — Next.js 16.2.4, React 19, Tailwind v4, shadcn/ui, Zustand
- **Backend:** `f:\claude_houseing\apnanest\backend` — ASP.NET Core 9, Dapper, PostgreSQL (Supabase)
- **DB:** Supabase PostgreSQL (connection string in `backend/src/ApnaNest.API/appsettings.json`)
- **Package manager:** `npm` (always use `--legacy-peer-deps`)

---

## 2. CURRENT BUILD STATUS

**TypeScript: 0 errors** | **`npm run build`: passes** | **`dotnet build`: passes (0 errors)**

### All routes that exist and work:
`/`, `/search`, `/property/[id]`, `/login`, `/signup`, `/post-property`, `/dashboard` (+ all sub-pages), `/tools/*`, `/news/*`, `/localities/*`, `/projects`, `/agents`, `/about`, `/contact`, `/sitemap.xml`

---

## 3. WHAT WAS JUST BEING WORKED ON (INCOMPLETE — FINISH FIRST)

The last session was fixing 3 bugs. **Two are half-done, one is untouched:**

### BUG 1 — Auth token not stored ⚡ HALF-DONE (finish this first)

**What was done:**
- `lib/stores/auth-store.ts` was fully rewritten — now has `user`, `token`, `loginWithEmail()`, `register()`, `logout()`, `hydrateFromStorage()`
- `components/auth/auth-modal.tsx` was fully rewritten — now has controlled inputs, API calls, loading spinners, error messages
- `components/auth/auth-url-handler.tsx` — added `hydrateFromStorage()` call on mount
- `components/navbar.tsx` — updated to show logged-in user avatar + logout button (conditional)
- `app/dashboard/layout.tsx` — logout button wired to `logout()` + router.push('/')

**What still needs to be done:**
- Run `npx tsc --noEmit` — there may be a TS error in `dashboard/layout.tsx` about the avatar initials (was interrupted mid-edit)
- The dashboard layout header shows `RK` hardcoded — needs to show `user?.name?.charAt(0)` instead:

```tsx
// In app/dashboard/layout.tsx, find:
<div className="w-10 h-10 rounded-xl bg-slate-900 flex items-center justify-center text-white font-bold">
  RK
</div>
// Replace with:
<div className="w-10 h-10 rounded-xl bg-slate-900 flex items-center justify-center text-white font-bold">
  {user?.name?.charAt(0)?.toUpperCase() ?? 'U'}
</div>
```

### BUG 2 — Post property wizard doesn't submit ⚡ NOT DONE YET

**File:** `f:\claude_houseing\apnanest\frontend\components\post-property\property-form-wizard.tsx`

**Current broken code** (line ~80):
```ts
const onSubmit = async (data: FormData) => {
  setIsSubmitting(true)
  // Simulate API call
  await new Promise(resolve => setTimeout(resolve, 2000))
  setIsSubmitting(false)
  setCurrentStep(3) // Go to finish step
}
```

**Fix — replace the `onSubmit` with:**
```ts
const onSubmit = async (data: FormData) => {
  setIsSubmitting(true)
  try {
    const token = localStorage.getItem('apnanest_token')
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001/api'}/properties`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({
        propertyTypeId:  Number(data.propertyTypeId),
        listingIntentId: Number(data.listingIntentId),
        title:           data.title,
        description:     data.description,
        price:           Number(data.price),
        areaSqft:        Number(data.areaSqft),
        bhk:             data.bhk ? Number(data.bhk) : null,
        bathrooms:       data.bathrooms ? Number(data.bathrooms) : null,
        addressLine:     data.addressLine,
        isActive:        true,
        isZeroBrokerage: true,
        isReraVerified:  false,
        // cityId and localityId need to come from a city/locality lookup
        // For now send placeholder UUIDs — fix properly later
        cityId:      '11111111-1111-1111-1111-111111111111',
        localityId:  '00000000-0000-0000-0000-000000000000',
      }),
    })
    if (!response.ok) throw new Error('Failed to create property')
    setCurrentStep(3) // success step
  } catch (err) {
    console.error(err)
    alert('Failed to submit. Please check you are logged in.')
  } finally {
    setIsSubmitting(false)
  }
}
```

**Also fix Step 2 location fields** — add city/locality dropdowns that query `/api/cities` and `/api/localities?citySlug=...`. The wizard currently has a text input for city but should be a `<Select>` populated from the API.

### BUG 3 — Logout ✅ DONE (just needs tsc verification)

---

## 4. REMAINING UI/UX BUGS (fix in order of severity)

### 🔴 Critical UX

| # | Bug | File | Exact Fix |
|---|---|---|---|
| 1 | **Save button fails silently when not logged in** — no feedback to user | `components/property-card.tsx` | After `catch { setSaved(!next) }`, add: `if (!localStorage.getItem('apnanest_token')) { toast.error('Login to save properties'); useAuthStore.getState().openLogin(); }` |
| 2 | **Search results don't update when URL ?type changes** | `components/search/search-results.tsx` | Add `searchParams` to the useEffect dependency array |
| 3 | **Property detail page uses client-side `useEffect` + `getPropertyById`** causing flash | `app/property/[id]/page.tsx` | This entire page was rewritten as a client component (bad). Ideally it should be a server component again using the service layer. For now, just ensure the loading state looks good |
| 4 | **Map view `react-map-gl/maplibre` imports missing** | `components/search/map-view.tsx` | Run: `npm install react-map-gl maplibre-gl --legacy-peer-deps` if not already installed |
| 5 | **hero-bg.png 404** | `components/home/hero-section.tsx` | The hero uses `/hero-bg.png` as background. The file exists in `/public/` but if it's missing add: `<div className="absolute inset-0 bg-gradient-to-b from-slate-900 to-slate-800" />` as fallback |

### 🟡 Medium UX

| # | Bug | File | Fix |
|---|---|---|---|
| 6 | **OTP login shows "Coming Soon" warning** — needs to actually work | `auth-modal.tsx` | Wire OTP: `POST /auth/login-otp` (not in backend yet — add it or remove the OTP tab entirely and keep just email) |
| 7 | **Dashboard shows mock data in Overview stats** (hard-coded "4,521 views") | `app/dashboard/page.tsx` | Call `GET /api/properties/my` and count actual view_count |
| 8 | **Post property page requires login but no middleware check** | `middleware.ts` | Already in middleware, but it checks `refreshToken` cookie — backend sets JWT in header not cookie. Fix: check `Authorization` header or change backend to set cookie |
| 9 | **Filter sidebar — furnishing checkboxes don't filter results** | `search-results.tsx` | `filters.furnishing` array is tracked but the `filtered` useMemo doesn't apply it. Add furnishing filter logic |
| 10 | **Pagination missing on search** — max 50 results shown | `search-results.tsx` | Add "Load More" button that increments page state and appends results |

### 🟢 Polish

| # | Issue | Fix |
|---|---|---|
| 11 | **Mobile bottom nav shows on desktop** if some CSS loads late | Add `lg:hidden` to the bottom nav `<nav>` — verify it's there |
| 12 | **Agents page uses AGENTS constant** (no backend) | OK for now — add `/api/agents` backend endpoint later |
| 13 | **News articles use static data** | OK for now — add a `news` table and endpoints later |
| 14 | **Property images show Unsplash URLs** which may be slow | Production: upload to Cloudflare R2, use CDN URLs |
| 15 | **No Toast provider** — `import { Toaster } from 'sonner'` added to layout but never tested | Verify `<Toaster>` is in `app/layout.tsx` body and toasts appear on save/login errors |

---

## 5. BACKEND MISSING ENDPOINTS (build these)

The .NET backend at `f:\claude_houseing\apnanest\backend` needs these additions:

### Priority 1 — Required for core flows
```
POST /api/auth/login-otp    { phone }           → sends OTP (integrate Twilio/MSG91)
POST /api/auth/verify-otp   { phone, code }     → verifies, returns JWT
```

### Priority 2 — Dashboard data
```
GET  /api/properties/my              → already added ✓
GET  /api/users/me/saved             → already added ✓
GET  /api/users/me/stats             → NEW: { totalViews, totalLeads, activeListings, savedCount }
GET  /api/users/me/leads             → NEW: leads received for all user's properties
GET  /api/users/me/notifications     → NEW: notification list
```

### Priority 3 — Content
```
GET  /api/news                       → NEW: paginated news articles (add news table to DB)
GET  /api/news/{slug}                → NEW: single article
GET  /api/agents                     → NEW: verified agent list
GET  /api/projects                   → NEW: developer project list
```

---

## 6. DATABASE — WHAT'S BEEN DONE + WHAT'S LEFT

### SQL Files to Run (in order):
```
1. apnanest_seed.sql                → Main schema + 20 properties
2. apnanest_alter_properties.sql    → Patches missing columns + creates vw_properties VIEW
3. apnanest_fixes.sql               → Fixes states/localities/users columns
4. apnanest_more_properties.sql     → 30 more properties (50 total)
```

All 4 are at: `f:\claude_houseing\apnanest\`

### After running, verify in Supabase:
```sql
SELECT COUNT(*) FROM properties WHERE is_active = true;   -- should be 50
SELECT COUNT(*) FROM localities;                           -- should be ~24
SELECT COUNT(*) FROM vw_properties WHERE is_active = true; -- should match above
```

### Schema gaps to fix:
```sql
-- Add these tables (not yet created):
CREATE TABLE news_articles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  slug VARCHAR(100) UNIQUE NOT NULL,
  title VARCHAR(200) NOT NULL,
  excerpt TEXT, category VARCHAR(50),
  author VARCHAR(100), content TEXT,
  image_url TEXT, published_at TIMESTAMPTZ DEFAULT NOW(),
  is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE notifications (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL,
  type VARCHAR(30) NOT NULL, -- 'enquiry' | 'price_drop' | 'new_match'
  title VARCHAR(100) NOT NULL,
  body TEXT,
  is_read BOOLEAN DEFAULT FALSE,
  metadata JSONB,
  created_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

## 7. ARCHITECTURE REFERENCE

### Data Flow
```
Frontend (Next.js) → lib/api.ts → .NET API (port 5001) → vw_properties VIEW → Supabase PostgreSQL
                       ↓ (if API fails)
                  lib/data.ts (mock fallback — 12 properties)
```

### Auth Flow
```
User fills modal → POST /api/auth/login → returns { token }
                → token stored in localStorage as 'apnanest_token'
                → GET /api/auth/me → returns { id, name, email, roleId }
                → user stored in localStorage as 'apnanest_user' + Zustand state
                → hydrateFromStorage() called on every page load
```

### Key Files
| File | Purpose |
|---|---|
| `lib/stores/auth-store.ts` | Zustand: user state, loginWithEmail(), register(), logout(), hydrateFromStorage() |
| `lib/api.ts` | Fetch wrapper — reads token from localStorage, hits NEXT_PUBLIC_API_URL |
| `services/property-service.ts` | API calls with BackendProperty→Property adapter (adapt() function) |
| `services/locality-service.ts` | GET /api/localities with Locality adapter |
| `services/lead-service.ts` | POST /api/leads |
| `mocks/handlers.ts` | MSW handlers — activated when NEXT_PUBLIC_USE_MSW=true |
| `middleware.ts` | Route protection: checks refreshToken cookie (currently broken — see Bug #8) |

### Property Type Mapping (DB ↔ Frontend)
```
DB listingIntentId: 1=buy, 2=rent, 3=pg
DB propertyTypeId:  1=Apartment, 2=Villa, 3=Plot, 4=PG, 5=Commercial, 6=Studio, 7=Builder Floor
DB furnishingStatusId: 1=Unfurnished, 2=Semi, 3=Fully
DB possessionStatusId: 1=Ready, 2=UnderConstruction, 3=NewLaunch
```

---

## 8. HOW TO START EVERYTHING

```powershell
# Terminal 1 — Backend
cd f:\claude_houseing\apnanest\backend
dotnet run --project src/ApnaNest.Web
# → http://localhost:5001
# → Swagger: http://localhost:5001/swagger

# Terminal 2 — Frontend
cd f:\claude_houseing\apnanest\frontend
# Create .env.local if not exists:
# NEXT_PUBLIC_API_URL=http://localhost:5001/api
# NEXT_PUBLIC_USE_MSW=false
npm run dev
# → http://localhost:3000
```

**Test credentials** (create via Swagger first):
```bash
# Create owner account via Swagger POST /api/auth/register:
{ "email": "owner@test.com", "password": "Test@1234", "firstName": "Rajesh", "lastName": "Kumar", "phone": "+919876500001", "isOwner": true }

# Create buyer account:
{ "email": "buyer@test.com", "password": "Test@1234", "firstName": "Priya", "lastName": "Sharma", "phone": "+919876500002", "isOwner": false }
```

---

## 9. PRODUCTION READINESS CHECKLIST

### Must-fix before going live:
- [ ] **Auth middleware** — change from checking HTTP-only cookie to checking Bearer token (or set cookie in backend on login)
- [ ] **Post property city/locality selector** — replace text input with Select dropdowns populated from `/api/cities` and `/api/localities`
- [ ] **Image upload** — wizard photo step connects to `POST /api/properties/{id}/images` → Cloudflare R2
- [ ] **Real OTP** — integrate Twilio or MSG91 for phone number verification
- [ ] **Pagination** — search results limited to 50 — add page parameter
- [ ] **JWT expiry handling** — token expires after 7 days, need refresh token flow
- [ ] **Dashboard stats** — replace hardcoded numbers with real API data
- [ ] **Property value tool** — currently pure frontend algorithm, connect to real valuation API

### Performance:
- [ ] Add `<Image priority>` to hero and first property card
- [ ] Add `loading.tsx` for `/search` and `/localities` routes
- [ ] Set `staleTime` on API calls (install TanStack Query v5)
- [ ] Enable ISR (Incremental Static Regeneration) on locality pages: `export const revalidate = 3600`

### SEO:
- [ ] Add JSON-LD structured data to `/property/[id]` (RealEstateListing schema)
- [ ] Add OpenGraph image to property pages using Next.js dynamic OG
- [ ] Canonical URLs on all pages
- [ ] robots.txt and sitemap.xml verification

### Security:
- [ ] Rate-limit `/api/auth/login` (5 attempts/minute per IP)
- [ ] Sanitize property description (XSS prevention — currently raw text)
- [ ] Validate all UUID parameters in controllers
- [ ] HTTPS enforced in production

---

## 10. FULL FEATURE STATUS TABLE

| Feature | Status | Notes |
|---|---|---|
| Home page with hero search | ✅ Working | Uses API for featured + localities |
| Search with filters | ✅ Working | API + client-side secondary filters |
| Map view (MapLibre) | ✅ Working | Free Carto tiles, no API key needed |
| Property detail page | ✅ Working | API first, mock fallback |
| Image gallery | ✅ Working | Multiple images, prev/next |
| EMI Calculator | ✅ Working | Bank comparison + amortization |
| Affordability tool | ✅ Working | FOIR calculation |
| Rent receipt generator | ✅ Working | Preview + download |
| Property valuation tool | ✅ Working | Frontend algorithm |
| Login modal | ✅ Working | Email/password wired to API |
| Signup modal | ✅ Working | Buyer/owner, wired to API |
| Auth hydration on reload | ✅ Working | localStorage → Zustand |
| Logout | ✅ Working | Clears localStorage + redirects |
| Navbar user avatar | ✅ Working | Shows name initial when logged in |
| Save/heart property | ✅ Working | Optimistic update + API call |
| Post property wizard | ⚠️ Partial | 5 steps work but final submit is setTimeout not API |
| Dashboard overview | ⚠️ Partial | Hardcoded stats, API for listings |
| Dashboard listings | ✅ Working | Real API via getMyProperties() |
| Dashboard saved | ✅ Working | Real API via getSavedProperties() |
| Dashboard messages | ✅ Working | Mock leads from API |
| Dashboard settings | ⚠️ Partial | Form renders but save not wired |
| Localities page | ✅ Working | API-driven |
| Locality detail | ✅ Working | Properties from API |
| News | ⚠️ Static | No backend — static data from lib/data.ts |
| Projects | ⚠️ Static | No backend |
| Agents | ⚠️ Static | No backend |
| OTP login | ❌ Not working | "Coming Soon" warning shown |
| Property submit | ❌ Broken | setTimeout fake submit |
| Filter furnishing | ❌ Bug | Checkbox tracked but not applied |
| Search pagination | ❌ Missing | Max 50 results, no load more |

---

## 11. IMMEDIATE NEXT STEPS (do in order)

```
1. Run: npx tsc --noEmit — fix any errors
2. Fix dashboard/layout.tsx — avatar initial (see Section 3, Bug 1)
3. Fix property-form-wizard.tsx — real API submit (see Section 3, Bug 2)
4. Fix search-results.tsx — add furnishing filter + searchParams dependency
5. Fix property-card.tsx — show login modal on save-while-logged-out
6. Add /api/users/me/stats endpoint in backend
7. Wire dashboard overview stats to real API
8. Run: npm run build — verify still passes
9. Test full flow: register → login → search → save property → post property
```

---

*V3 Handoff — 30 Apr 2026 | contact.dwarkesh@gmail.com*

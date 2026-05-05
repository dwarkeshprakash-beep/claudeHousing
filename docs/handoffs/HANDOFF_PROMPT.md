# ApnaNest — Full AI Handoff Prompt
> Give this entire document to the next AI. It is a complete, self-contained brief.
> Working directory: `f:\claude_houseing\apnanest`

---

## 1. WHAT THIS PROJECT IS

**ApnaNest** is a full-stack Indian real estate platform — a feature-complete replica/competitor to Housing.com, MagicBricks, and NoBroker. The differentiators are:
- Zero brokerage by default
- RERA verification first-class
- Owner-direct listings
- Hyper-local neighbourhood data

**Stack:**
- **Frontend:** Next.js 16.2.4 + React 19 + TypeScript + Tailwind CSS v4 + shadcn/ui
- **Backend:** ASP.NET Core 8 (Clean Architecture) — already built, lives at `f:\claude_houseing\apnanest\backend`
- **DB:** PostgreSQL 16 + PostGIS
- **Auth:** JWT RS256 (15 min access) + refresh tokens (30 days, HTTP-only cookie)
- **State:** Zustand (client), TanStack Query (server)
- **Package manager:** `npm` (use `npm install --legacy-peer-deps` for all installs)

---

## 2. CURRENT BUILD STATE

### ✅ FULLY BUILT & COMPILING (92 source files, `npm run build` passes)

**Frontend lives at:** `f:\claude_houseing\apnanest\frontend`

**All routes that exist and work:**

| Route | Status |
|---|---|
| `/` | ✅ Home page — hero, stats, top-picks, localities, why-us, developers, tools, news, sell-banner |
| `/search` | ✅ Full filter sidebar, sort, list/map toggle, mobile filter drawer |
| `/property/[id]` | ✅ Gallery, tabs (overview/amenities/location/EMI), contact form, similar properties |
| `/post-property` | ✅ 5-step wizard (type → location → details → pricing → photos) |
| `/login` | ✅ Exists as page but needs to become a modal (see Section 3) |
| `/signup` | ✅ Exists as page but needs to become a modal (see Section 3) |
| `/dashboard` | ✅ Overview, listings, saved, messages (chat UI), searches, alerts, documents, settings |
| `/projects` | ✅ Project cards + developer grid |
| `/agents` | ✅ Agent cards with contact |
| `/localities` | ✅ Index + `[slug]` detail with price trend chart |
| `/news` | ✅ Index + `[slug]` article detail |
| `/tools` | ✅ Index + EMI calculator + affordability + rent receipt |
| `/about` | ✅ Story, values, team |
| `/contact` | ✅ Contact form |
| `/sitemap.xml` | ✅ Auto-generated |

**Service layer (connects to real .NET backend):**
- `lib/api.ts` — fetch wrapper, attaches JWT from localStorage, hits `localhost:5000/api`
- `services/property-service.ts` — `getFeaturedProperties()`, `getPropertyById()`, falls back to mock data
- `services/lead-service.ts` — `submitLead()`, `getLeadsForProperty()`

**Mock data lives in:** `lib/data.ts` — 12 properties, 6 localities, 6 news articles, 6 projects, 6 agents, 6 developers

**Zustand is installed** (`npm install zustand` already run). Store skeleton started at:
`lib/stores/auth-store.ts` — partially created, has `isOpen`, `view`, `loginTab`, `signupRole`, `openLogin()`, `openSignup()`, `close()` — **but not yet wired to any component.**

---

## 3. IMMEDIATE TASKS TO IMPLEMENT (Priority Order)

### TASK 1 — Auth Modal (Login/Signup as popup, not page) ⚡ HIGHEST PRIORITY

**The problem:** Login and signup are full pages (`/login`, `/signup`). Housing.com uses a modal overlay. The UX feels old with full-page auth.

**Exact implementation:**

**Step 1:** The Zustand store is already created at `lib/stores/auth-store.ts`. It's complete. Do not modify it.

**Step 2:** Create `components/auth/auth-modal.tsx`:
```
- Use the existing Dialog component from components/ui/dialog.tsx
- The modal has two views: "login" and "signup" — controlled by useAuthStore().view
- Login view has two tabs: "OTP" and "Email/Password" — use the Tabs component
- Signup view has two tabs: "Buyer/Tenant" and "Property Owner"
- Both views have a "switch" link at the bottom ("Don't have account? Sign up" / "Already have account? Login")
- Include Google OAuth button and Apple OAuth button at the bottom
- OTP flow: phone number input → Send OTP button → OTP input appears → Verify button
- Email flow: email + password with show/hide toggle
- Signup: name + email + phone + password fields
- The modal should be dismissible by clicking outside (Dialog's default behavior)
- Import and use useAuthStore from lib/stores/auth-store
- Style with the brand colors: primary teal (#14B8A6), clean white background
- Make it look like Housing.com's auth modal — modern, clean, no clutter
- Width: max-w-md on desktop, full screen on mobile (use sheet on mobile)
```

**Step 3:** Add the modal to `app/layout.tsx`:
```tsx
// Import AuthModal at the top
import { AuthModal } from '@/components/auth/auth-modal'

// Add inside the <body>, after {children}:
<AuthModal />
```

**Step 4:** Update `components/navbar.tsx` — find the Login button (Link href="/login") and replace with:
```tsx
// Add this import at top:
import { useAuthStore } from '@/lib/stores/auth-store'

// Inside Navbar function, add:
const { openLogin, openSignup } = useAuthStore()

// Replace the Login Link with a button:
<button
  onClick={openLogin}
  className="hidden md:flex items-center gap-1.5 px-3 py-2 rounded-xl border border-border text-sm font-medium hover:border-[var(--primary-500)] transition-colors"
  style={{ color: 'var(--text-secondary)' }}
>
  <User className="w-4 h-4" />
  Login
</button>

// Also add a Sign Up button next to it:
<button
  onClick={openSignup}
  className="hidden md:flex items-center gap-1.5 px-4 py-2.5 rounded-xl text-sm font-semibold text-white"
  style={{ background: 'var(--primary-500)' }}
>
  Sign Up
</button>
```

**Step 5:** Keep `/login` and `/signup` pages but make them redirect to home with modal open:
```tsx
// In app/login/page.tsx — make it a client component that opens the modal:
'use client'
import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '@/lib/stores/auth-store'
export default function LoginPage() {
  const router = useRouter()
  const { openLogin } = useAuthStore()
  useEffect(() => { router.push('/'); openLogin() }, [])
  return null
}
```

---

### TASK 2 — UI Polish Pass (Make it feel professional) ⚡ HIGH PRIORITY

The current UI works but feels flat/incomplete. Here are the specific things to fix:

#### 2A. Fix `globals.css` — add these improvements:
```css
/* Add to globals.css: */

/* Better font weights */
@layer base {
  h1, h2, h3 { letter-spacing: -0.025em; }
}

/* Property image fallback gradient */
.img-fallback {
  background: linear-gradient(135deg, #f0fdf4 0%, #e0f2fe 100%);
}

/* Better card hover */
.card-hover {
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}
.card-hover:hover {
  transform: translateY(-3px);
  box-shadow: 0 20px 40px rgba(28,25,23,0.10);
}

/* Pill badge */
.badge-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 10px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.02em;
}

/* Gradient text */
.gradient-text {
  background: linear-gradient(135deg, var(--primary-500), var(--primary-700));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}
```

#### 2B. Rewrite `components/home/hero-section.tsx` — make it Housing.com-like:
- **Background:** Dark navy gradient (`#0f172a` to `#1e293b`) instead of the light green — this is much more premium
- **Layout:** Full-width section, centered search box (not 2-column)
- **Search box:** Large white card, raised with shadow, tabs at top (Buy/Rent/PG/Plot/Commercial)
- **Search input:** Location input with a MapPin icon + autocomplete dropdown
- **Right of input:** A "Search" button in teal with a search icon
- **Below search:** "Popular searches:" with pill chips (Bopal Ahmedabad, Whitefield Bangalore, etc.)
- **Floating stats:** 3 small cards below the search box: "50,000+ Listings", "Zero Brokerage", "RERA Verified"
- **Background decoration:** Subtle building silhouette or city skyline SVG at the bottom of the hero

The current hero has an emoji 🏡 on the right side which looks completely unprofessional. Remove that entirely and make it a centered full-width hero.

#### 2C. Fix `components/property-card.tsx` — improve image handling:
Add `onError` handler to Next.js Image component:
```tsx
// Add state for image error:
const [imgError, setImgError] = useState(false)

// Replace the Image src:
src={imgError ? '' : (property.images[0] || '')}

// Add fallback div that shows when no image:
{(imgError || !property.images[0]) && (
  <div className="absolute inset-0 flex items-center justify-center"
    style={{ background: 'linear-gradient(135deg, var(--primary-50), var(--surface-2))' }}>
    <div className="text-center">
      <div className="text-4xl mb-2">🏠</div>
      <p className="text-xs" style={{ color: 'var(--text-tertiary)' }}>{property.type}</p>
    </div>
  </div>
)}

// Add to Image component:
onError={() => setImgError(true)}
```

Also improve the card design:
- Add a subtle bottom border accent in the primary color on hover
- Make the price font larger (text-2xl instead of text-xl for grid cards)
- Add a "New" badge for properties posted in last 24h
- Show "X days ago" more prominently

#### 2D. Improve `components/navbar.tsx`:
- The current navbar has a notification bell that does nothing — wire it to show notification count from auth store
- Add an active state indicator (bottom border) for current nav items
- On mobile, the bottom nav "Post" center button should have a proper floating action button style with a shadow ring

#### 2E. Fix Search Page `components/search/search-results.tsx`:
- The empty state needs a better illustration
- Add "Load more" button (or pagination) at the bottom — currently shows all properties with no pagination
- Add a "Showing X–Y of Z properties" counter
- The list view cards look good but the grid view on map toggle is too small — increase to min 280px per card

---

### TASK 3 — Property Value Tool

**Create these files:**

`app/tools/property-value/page.tsx`:
```tsx
import type { Metadata } from 'next'
import { Navbar } from '@/components/navbar'
import { Footer } from '@/components/footer'
import { PropertyValueClient } from './property-value-client'

export const metadata: Metadata = {
  title: 'Property Valuation — Instant AI Estimate',
  description: 'Get an instant AI-powered estimate of any property\'s current market value. Enter address, type, and size.',
}

export default function PropertyValuePage() {
  return (
    <div className="min-h-screen flex flex-col">
      <Navbar />
      <main className="flex-1 pb-16 lg:pb-0"><PropertyValueClient /></main>
      <Footer />
    </div>
  )
}
```

`app/tools/property-value/property-value-client.tsx` — full client component:
- **Inputs:** City (dropdown from CITIES array in lib/data), Locality (text input), Property Type (dropdown: Apartment/Villa/Plot/Builder Floor), Area (number input, sq.ft), BHK (1/2/3/4/5+), Age (0–1 yr / 1–3 yr / 3–5 yr / 5–10 yr / 10+ yr), Floor (number), Furnishing (dropdown)
- **Algorithm (frontend only, no API needed):**
  ```
  Base rates (₹/sq.ft) per city:
  Mumbai: 25000, Delhi: 18000, Bangalore: 12000, Pune: 9000,
  Hyderabad: 9500, Chennai: 9000, Ahmedabad: 7500, others: 6500

  Multipliers:
  - Villa: 1.4, Apartment: 1.0, Builder Floor: 0.9, Plot: 0.7
  - BHK 1: 0.85, 2: 1.0, 3: 1.1, 4: 1.2, 5+: 1.3
  - Age 0-1yr: 1.15, 1-3yr: 1.05, 3-5yr: 1.0, 5-10yr: 0.92, 10+yr: 0.85
  - Fully Furnished: 1.1, Semi: 1.0, Unfurnished: 0.95
  - Floor > 5 AND not top floor: +2% per floor above 5 (up to 15% max)

  Estimate = area × baseRate × type × bhk × age × furnishing × floor
  Range = ±8%
  ```
- **Output display:**
  - Large "Estimated Value" with range (e.g., "₹62.4 L – ₹72.8 L")
  - Center value prominently
  - "Market Rate" showing ₹/sq.ft for that city/type
  - A bar chart showing how this property compares to locality average (low/mid/high)
  - "Confidence: High/Medium" badge
  - Disclaimer: "This is an AI estimate based on market data. Get a professional appraisal for accurate valuation."
  - CTA: "Get a Free Professional Valuation" button → opens contact form
  - "Search similar properties" button → links to search with params

---

### TASK 4 — MSW Mock Service Worker (Local dev without backend)

**Purpose:** Allows frontend development without the .NET backend running. All API calls return realistic mock data.

**Install:** Already done via npm — `msw` package needs to be added:
```bash
npm install msw --save-dev --legacy-peer-deps
```

**Files to create:**

`mocks/data/properties.json` — a JSON file with 20+ mock properties (copy from lib/data.ts PROPERTIES array, convert to JSON)

`mocks/handlers/properties.ts`:
```ts
import { http, HttpResponse } from 'msw'
import { PROPERTIES, LOCALITIES, NEWS_ARTICLES, PROJECTS, AGENTS } from '@/lib/data'

export const propertyHandlers = [
  // GET /api/properties
  http.get('http://localhost:5000/api/properties', ({ request }) => {
    const url = new URL(request.url)
    const type = url.searchParams.get('type')
    const city = url.searchParams.get('city')
    const q = url.searchParams.get('q')
    
    let results = PROPERTIES
    if (type) results = results.filter(p => p.listingType === type)
    if (city) results = results.filter(p => p.city.toLowerCase() === city.toLowerCase())
    if (q) results = results.filter(p => 
      p.title.toLowerCase().includes(q.toLowerCase()) || 
      p.locality.toLowerCase().includes(q.toLowerCase())
    )
    
    return HttpResponse.json({
      items: results,
      total: results.length,
      page: 1,
      pageSize: 20
    })
  }),

  // GET /api/properties/:id
  http.get('http://localhost:5000/api/properties/:id', ({ params }) => {
    const property = PROPERTIES.find(p => p.id === params.id)
    if (!property) return HttpResponse.json({ error: 'Not found' }, { status: 404 })
    return HttpResponse.json(property)
  }),

  // POST /api/leads
  http.post('http://localhost:5000/api/leads', async ({ request }) => {
    const body = await request.json()
    return HttpResponse.json({ leadId: `lead_${Date.now()}`, ...body }, { status: 201 })
  }),

  // GET /api/localities
  http.get('http://localhost:5000/api/localities', () => {
    return HttpResponse.json(LOCALITIES)
  }),
]
```

`mocks/handlers/auth.ts`:
```ts
import { http, HttpResponse } from 'msw'

export const authHandlers = [
  http.post('http://localhost:5000/api/auth/login', async ({ request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      accessToken: 'mock_access_token_' + Date.now(),
      user: {
        id: 'user_1',
        name: 'Rajesh Kumar',
        email: body.email || 'rajesh@example.com',
        phone: '+91 98765 11111',
        role: 'buyer'
      }
    })
  }),

  http.post('http://localhost:5000/api/auth/register', async ({ request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      accessToken: 'mock_access_token_' + Date.now(),
      user: {
        id: 'user_' + Date.now(),
        name: body.name,
        email: body.email,
        phone: body.phone,
        role: body.role || 'buyer'
      }
    }, { status: 201 })
  }),

  http.post('http://localhost:5000/api/auth/refresh', () => {
    return HttpResponse.json({ accessToken: 'mock_refreshed_token_' + Date.now() })
  }),

  http.get('http://localhost:5000/api/auth/me', () => {
    return HttpResponse.json({
      id: 'user_1',
      name: 'Rajesh Kumar',
      email: 'rajesh@example.com',
      phone: '+91 98765 11111',
      role: 'buyer'
    })
  }),
]
```

`mocks/handlers.ts`:
```ts
import { propertyHandlers } from './handlers/properties'
import { authHandlers } from './handlers/auth'
export const handlers = [...propertyHandlers, ...authHandlers]
```

`mocks/browser.ts`:
```ts
import { setupWorker } from 'msw/browser'
import { handlers } from './handlers'
export const worker = setupWorker(...handlers)
```

`app/msw-init.tsx` (client component):
```tsx
'use client'
import { useEffect } from 'react'

export function MSWInit() {
  useEffect(() => {
    if (process.env.NODE_ENV === 'development' && process.env.NEXT_PUBLIC_USE_MSW === 'true') {
      import('../mocks/browser').then(({ worker }) => {
        worker.start({ onUnhandledRequest: 'bypass' })
      })
    }
  }, [])
  return null
}
```

Update `app/layout.tsx` — add MSWInit inside body:
```tsx
import { MSWInit } from './msw-init'
// Inside body:
<MSWInit />
{children}
```

Run to generate the service worker file:
```bash
npx msw init public/ --save
```

Add to `.env.local`:
```
NEXT_PUBLIC_USE_MSW=true
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_MAPBOX_TOKEN=pk.your_mapbox_token_here
```

---

### TASK 5 — Auth Middleware (Protect dashboard routes)

**Create `middleware.ts` at `f:\claude_houseing\apnanest\frontend\middleware.ts`:**

```ts
import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

const PROTECTED_ROUTES = [
  '/dashboard',
  '/post-property',
]

const AUTH_ROUTES = ['/login', '/signup']

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl
  
  // Check for auth token in cookie (set by backend on login)
  const token = request.cookies.get('apnanest_refresh')?.value
  const isAuthenticated = !!token
  
  // Redirect unauthenticated users away from protected routes
  const isProtected = PROTECTED_ROUTES.some(route => pathname.startsWith(route))
  if (isProtected && !isAuthenticated) {
    const loginUrl = new URL('/', request.url)
    loginUrl.searchParams.set('auth', 'login')
    loginUrl.searchParams.set('redirect', pathname)
    return NextResponse.redirect(loginUrl)
  }
  
  // Redirect authenticated users away from auth pages
  if (AUTH_ROUTES.includes(pathname) && isAuthenticated) {
    return NextResponse.redirect(new URL('/dashboard', request.url))
  }
  
  return NextResponse.next()
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico|images|icons).*)'],
}
```

Then update `app/layout.tsx` to handle the `?auth=login` query param and open the auth modal automatically:
```tsx
// In layout.tsx, add a client component that reads the URL param:
// Create components/auth/auth-url-handler.tsx:
'use client'
import { useEffect } from 'react'
import { useSearchParams, useRouter } from 'next/navigation'
import { useAuthStore } from '@/lib/stores/auth-store'

export function AuthUrlHandler() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const { openLogin, openSignup } = useAuthStore()
  
  useEffect(() => {
    const auth = searchParams.get('auth')
    if (auth === 'login') { openLogin(); router.replace(window.location.pathname) }
    if (auth === 'signup') { openSignup(); router.replace(window.location.pathname) }
  }, [searchParams])
  
  return null
}
```

---

### TASK 6 — Mapbox Integration (Property map view)

**Purpose:** Replace the placeholder in `components/search/map-view.tsx` with a real interactive map.

**Install:**
```bash
npm install mapbox-gl @types/mapbox-gl react-map-gl --legacy-peer-deps
```

**Create `components/search/map-view.tsx` (rewrite completely):**
```tsx
'use client'
import { useState, useCallback } from 'react'
import Map, { Marker, Popup, NavigationControl } from 'react-map-gl'
import 'mapbox-gl/dist/mapbox-gl.css'
import type { Property } from '@/lib/data'
import { formatPrice } from '@/lib/utils'
import Link from 'next/link'

interface MapViewProps {
  properties: Property[]
}

const MAPBOX_TOKEN = process.env.NEXT_PUBLIC_MAPBOX_TOKEN

export function MapView({ properties }: MapViewProps) {
  const [selectedProperty, setSelectedProperty] = useState<Property | null>(null)
  const [viewport, setViewport] = useState({
    latitude: 23.0225, longitude: 72.5714, zoom: 10
  })
  
  if (!MAPBOX_TOKEN) {
    return (
      <div className="w-full h-[480px] rounded-2xl flex items-center justify-center border border-dashed border-border bg-muted">
        <div className="text-center">
          <p className="font-semibold text-sm" style={{ color: 'var(--text-primary)' }}>Map view requires Mapbox</p>
          <p className="text-xs mt-1" style={{ color: 'var(--text-tertiary)' }}>Add NEXT_PUBLIC_MAPBOX_TOKEN to .env.local</p>
        </div>
      </div>
    )
  }
  
  return (
    <div className="relative w-full h-[480px] rounded-2xl overflow-hidden">
      <Map
        {...viewport}
        onMove={(evt) => setViewport(evt.viewState)}
        mapboxAccessToken={MAPBOX_TOKEN}
        mapStyle="mapbox://styles/mapbox/light-v11"
        style={{ width: '100%', height: '100%' }}
      >
        <NavigationControl position="top-right" />
        
        {properties.map((property) => (
          <Marker
            key={property.id}
            latitude={property.lat}
            longitude={property.lng}
            onClick={(e) => { e.originalEvent.stopPropagation(); setSelectedProperty(property) }}
          >
            <div
              className="px-2 py-1 rounded-full text-xs font-bold text-white cursor-pointer shadow-lg hover:scale-110 transition-transform"
              style={{ background: 'var(--primary-500)', whiteSpace: 'nowrap' }}
            >
              {formatPrice(property.price, property.purpose)}
            </div>
          </Marker>
        ))}
        
        {selectedProperty && (
          <Popup
            latitude={selectedProperty.lat}
            longitude={selectedProperty.lng}
            onClose={() => setSelectedProperty(null)}
            closeButton={true}
            maxWidth="260px"
          >
            <div className="p-2">
              <p className="font-semibold text-sm">{selectedProperty.title}</p>
              <p className="text-xs text-gray-500">{selectedProperty.locality}, {selectedProperty.city}</p>
              <p className="font-bold text-primary mt-1">{formatPrice(selectedProperty.price, selectedProperty.purpose)}</p>
              <Link href={`/property/${selectedProperty.id}`} className="mt-2 block text-xs text-center py-1.5 rounded-lg text-white" style={{ background: 'var(--primary-500)' }}>
                View Details
              </Link>
            </div>
          </Popup>
        )}
      </Map>
    </div>
  )
}
```

---

## 4. FUTURE DEVELOPMENT ROADMAP (Phase by Phase)

### Phase 2 — Backend Integration (2–4 weeks)

The .NET backend is already built at `f:\claude_houseing\apnanest\backend`. The database schema is at `docs/DATABASE_SCHEMA.md`.

**Wire up these real API endpoints:**

1. **Auth:** `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/auth/me`
   - On login success: store `accessToken` in memory (NOT localStorage), store refresh token in HTTP-only cookie (handled by backend)
   - Create `lib/stores/user-store.ts` with Zustand: `{ user, accessToken, setAuth, clearAuth }`
   - Wire `lib/api.ts` to use the in-memory access token

2. **Properties:** Wire `services/property-service.ts` to real API
   - Replace mock fallback with proper error boundaries
   - Add pagination to search results
   - Add TanStack Query hooks: `useProperties()`, `useProperty(id)`, `useInfiniteProperties()`

3. **Leads/Enquiries:** Wire the contact form in `property-detail.tsx` to actually call `submitLead()`
   - Currently `enquiryForm` state exists but `onSubmit` just does `e.preventDefault()`
   - Add form validation with Zod
   - Show success toast on submission

4. **User Dashboard:**
   - `GET /api/users/me/listings` → wire to dashboard/listings page
   - `GET /api/users/me/saved` → wire to dashboard/saved page
   - `POST /api/users/me/saved/:propertyId` → wire to heart button in property-card.tsx
   - `GET /api/users/me/leads` → wire to dashboard/messages page

5. **Post Property:**
   - Wire the 5-step wizard to `POST /api/properties`
   - Add image upload to Cloudflare R2 (backend already configured) via `PUT /api/properties/:id/images`

### Phase 3 — Performance & SEO (1–2 weeks)

1. **Next.js Image Optimization:**
   - Configure `remotePatterns` in `next.config.mjs` for Unsplash + R2 CDN domain
   - Add `priority` to hero and above-fold images
   - Add `blurDataURL` placeholders

2. **TanStack Query (React Query v5):**
   - Install: `npm install @tanstack/react-query --legacy-peer-deps`
   - Create `lib/query-client.ts`
   - Wrap `app/layout.tsx` with `QueryClientProvider`
   - Convert all client-side data fetching from direct `api.get()` calls to `useQuery` hooks
   - Add stale time of 5 minutes for property data

3. **SEO:**
   - Add JSON-LD structured data to property pages (`Product`, `RealEstateListing` schema)
   - Add JSON-LD to locality pages (`Place` schema)
   - Add `robots.txt`
   - Add OpenGraph images (use a Next.js `opengraph-image.tsx` dynamic image)

4. **i18n (Hindi):**
   - Install `next-intl`
   - Create `messages/en.json` and `messages/hi.json`
   - Wrap routing with locale prefix `/en/` and `/hi/`
   - Translate: navbar items, hero text, property card labels, footer

### Phase 4 — Premium Features (4–8 weeks)

1. **RERA Verification API:**
   - Integrate with state RERA portals (scraper or unofficial API)
   - Show real-time RERA status badge on property detail page
   - Store RERA status in DB and refresh weekly via Hangfire job

2. **AI-Powered Features:**
   - Property value estimator (currently frontend-only algorithm → upgrade to ML model)
   - Smart search: NLP query parsing ("3 BHK under 80 lakhs in Bopal" → structured search params)
   - Similar property recommendations (vector similarity on property embeddings)

3. **Boost/Monetization:**
   - "Boost Listing" feature for owners
   - Razorpay payment integration for boost credits
   - Priority search ranking for boosted listings

4. **Real-time Features:**
   - SignalR WebSocket for real-time messages in dashboard/messages
   - Live enquiry notifications (push via web push API)
   - Price change alerts via email (Hangfire + SendGrid)

5. **Progressive Web App (PWA):**
   - Add `public/manifest.json`
   - Add service worker for offline support
   - Add push notification support
   - Configure installable home screen app

6. **Neighbourhood Reviews:**
   - Allow verified residents to review localities
   - Star rating + text review form
   - Display aggregate ratings on locality pages

### Phase 5 — Infrastructure & Deployment

**Docker Compose (already scaffolded at `f:\claude_houseing\apnanest\infra\`):**
```yaml
# Verify docker-compose.yml has:
services:
  postgres:        # PostgreSQL 16 + PostGIS
  redis:           # Redis 7
  minio:           # S3-compatible local storage
  seq:             # Structured logging UI
  backend:         # ASP.NET Core API
  frontend:        # Next.js (production)
```

**Run local infra:**
```bash
docker compose up -d   # from f:\claude_houseing\apnanest\
```

**Backend runs at:** `http://localhost:5000`
**Frontend dev:** `cd frontend && npm run dev` → `http://localhost:3000`

**Production Deployment (recommended for Indian audience):**

Option A — **DigitalOcean App Platform** (cheapest, $24/month):
- Frontend: Next.js app (static export or Node.js)
- Backend: Dockerfile deploy
- DB: DigitalOcean Managed PostgreSQL ($15/mo)

Option B — **Hetzner Cloud** (best price-to-performance, €6/month for CX22):
- VPS with Docker Compose
- Nginx reverse proxy
- Let's Encrypt SSL via Certbot
- GitHub Actions CI/CD

Option C — **Azure** (if resume value matters):
- Azure App Service for backend
- Azure Static Web Apps for frontend
- Azure Database for PostgreSQL

**Recommended: Hetzner VPS** for this project (Indian real estate platform, cost-sensitive).

**CI/CD with GitHub Actions:**
Create `.github/workflows/deploy.yml`:
```yaml
name: Deploy
on:
  push:
    branches: [main]
jobs:
  build-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '22' }
      - run: cd frontend && npm ci && npm run build
  build-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0' }
      - run: cd backend && dotnet build && dotnet test
  deploy:
    needs: [build-frontend, build-backend]
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to server
        run: ssh user@server 'cd /app && git pull && docker compose up -d --build'
```

**Environment Variables needed for production:**
```
# Backend
ConnectionStrings__DefaultConnection=Host=...;Database=apnanest;Username=...;Password=...
JwtSettings__PrivateKey=<RS256 private key>
JwtSettings__PublicKey=<RS256 public key>
CloudflareR2__AccountId=...
CloudflareR2__AccessKeyId=...
CloudflareR2__SecretAccessKey=...
CloudflareR2__BucketName=apnanest-media
Redis__ConnectionString=...
SendGrid__ApiKey=...

# Frontend
NEXT_PUBLIC_API_URL=https://api.apnanest.in
NEXT_PUBLIC_MAPBOX_TOKEN=pk.eyJ1...
NEXT_PUBLIC_USE_MSW=false
```

---

## 5. KEY CONVENTIONS TO FOLLOW (Do Not Break These)

These come from `CLAUDE.md` and are non-negotiable:

1. **Components:** Named exports only, no default exports for components
2. **Client components:** Only add `'use client'` when actually needed (interactivity). Default to server.
3. **Imports:** Use `@/` prefix always. Order: react/next → external → `@/` → relative
4. **No `any`:** Use `unknown` and narrow. The only exception is the service layer cast for API compatibility.
5. **No `useEffect` + `fetch`:** Use TanStack Query hooks for all data fetching from client components
6. **Forms:** Always React Hook Form + Zod resolver
7. **Styling:** Tailwind classes inline, `cn()` for conditional classes, CSS variables for brand colors
8. **File placement:**
   - Types → `/types/` or alongside component
   - API constants → `/lib/api/endpoints.ts`
   - Hooks → `/lib/hooks/` or co-located
   - Stores → `/lib/stores/`
9. **Never hard-delete user/property data** — soft delete with `deleted_at`
10. **Never trust client-supplied user IDs** — derive from JWT always
11. **Run `npx tsc --noEmit` after every major change** to catch type errors
12. **Run `npm run build` to verify** no runtime errors before declaring done

---

## 6. HOW TO START THE DEV SERVER

```bash
# 1. Start infra (from apnanest root)
cd f:\claude_houseing\apnanest
docker compose up -d

# 2. Start backend
cd f:\claude_houseing\apnanest\backend
dotnet run --project src/ApnaNest.Web

# 3. Start frontend
cd f:\claude_houseing\apnanest\frontend
npm run dev
# → http://localhost:3000
```

**Without backend (MSW mock mode — after Task 4 above is complete):**
```bash
cd f:\claude_houseing\apnanest\frontend
NEXT_PUBLIC_USE_MSW=true npm run dev
```

---

## 7. CURRENT BUG LIST (Known Issues to Fix)

1. `property-detail.tsx` imports `submitLead` but the form submit handler does nothing — wire it up
2. `top-picks-section.tsx` casts service return as `unknown as Property[]` — this will break if real API returns different shape — needs a proper DTO mapper
3. Dashboard pages (listings, saved, messages, searches, alerts, documents) all use hard-coded mock data — need real API calls
4. The `app/login/page.tsx` and `app/signup/page.tsx` still exist as full pages — after auth modal is built, redirect them to home with modal open
5. No error boundary on dashboard layout — if one panel fails, whole dashboard breaks
6. Image `onError` handling missing in property-card.tsx, locality card, news card — they all just show broken images
7. Mobile bottom nav appears on desktop if screen width detection fails — ensure `lg:hidden` class is applied correctly
8. `SearchResults` component has `PROPERTIES` imported from `lib/data` directly — should use TanStack Query + service layer
9. `app/tools/property-value` route is linked from tools index but the page doesn't exist yet — users get 404
10. The `?auth=login` redirect from middleware doesn't yet trigger the modal — `AuthUrlHandler` component needs to be added to layout

---

## 8. FILE STRUCTURE REFERENCE

```
f:\claude_houseing\apnanest\
├── frontend/                          ← Next.js app
│   ├── app/
│   │   ├── layout.tsx                 ← Root layout (ADD AuthModal, MSWInit, AuthUrlHandler here)
│   │   ├── page.tsx                   ← Home
│   │   ├── globals.css                ← Brand CSS variables + utilities
│   │   ├── sitemap.ts
│   │   ├── dashboard/                 ← All dashboard pages + layout + sidebar
│   │   ├── search/                    ← Search page + loading/error
│   │   ├── property/[id]/             ← Property detail + loading/error
│   │   ├── post-property/             ← 5-step wizard
│   │   ├── login/ signup/             ← Pages to CONVERT to modal redirects
│   │   ├── news/ localities/ projects/ agents/ about/ contact/
│   │   └── tools/ (emi-calculator, affordability, rent-receipt, property-value [MISSING])
│   ├── components/
│   │   ├── navbar.tsx                 ← UPDATE: Login button → openLogin() modal
│   │   ├── footer.tsx
│   │   ├── property-card.tsx          ← UPDATE: image error handling
│   │   ├── auth/                      ← CREATE: auth-modal.tsx, auth-url-handler.tsx
│   │   ├── home/                      ← hero (REWRITE), stats, top-picks, etc.
│   │   ├── property/property-detail.tsx  ← UPDATE: wire submitLead
│   │   ├── search/                    ← search-results, filter-sidebar, map-view (REWRITE)
│   │   └── ui/                        ← shadcn components (DO NOT MODIFY)
│   ├── lib/
│   │   ├── api.ts                     ← Fetch wrapper → backend
│   │   ├── data.ts                    ← Mock data (12 properties, 6 localities, etc.)
│   │   ├── utils.ts                   ← cn(), formatPrice(), etc.
│   │   └── stores/
│   │       └── auth-store.ts          ← Zustand auth modal state (ALREADY CREATED)
│   ├── services/
│   │   ├── property-service.ts        ← API calls with mock fallback
│   │   └── lead-service.ts            ← Lead/enquiry submission
│   ├── mocks/                         ← CREATE: MSW handlers
│   │   ├── handlers.ts
│   │   ├── handlers/
│   │   │   ├── properties.ts
│   │   │   └── auth.ts
│   │   └── browser.ts
│   ├── middleware.ts                   ← CREATE: route protection
│   ├── package.json
│   ├── next.config.mjs
│   └── tsconfig.json
├── backend/                           ← ASP.NET Core 8 (already built)
├── docs/
│   ├── PRD.md
│   ├── ARCHITECTURE.md
│   ├── DATABASE_SCHEMA.md
│   └── prompts/
│       ├── 02_FRONTEND_REACT_PROMPT.md
│       └── 03_BACKEND_API_PROMPT.md
└── infra/
    └── docker-compose.yml
```

---

## 9. VALIDATION COMMANDS (Run After Every Task)

```bash
# From frontend directory:
npx tsc --noEmit                        # Must show 0 errors
npm run build                           # Must complete successfully
npm run lint                            # Must show 0 errors (fix any before committing)
```

---

## 10. SUMMARY: WHAT TO DO FIRST

When you pick this up, execute in this exact order:

1. **Task 1** — Auth Modal (biggest UX win, 1–2 hours work)
   - Create `components/auth/auth-modal.tsx`
   - Update `app/layout.tsx` to include it
   - Update `components/navbar.tsx` Login button to trigger modal
   
2. **Task 2** — UI Polish (makes the app feel professional, 2–3 hours)
   - Fix `globals.css`
   - Rewrite hero section
   - Fix property card image handling

3. **Task 3** — Property Value Tool (1 hour, completes the tools section)
   - Create `app/tools/property-value/page.tsx` + client

4. **Task 4** — MSW Mock Handlers (1 hour, enables dev without backend)
   - Install msw, create handlers, init in layout

5. **Task 5** — Auth Middleware (30 mins, protects dashboard)
   - Create `middleware.ts`

6. **Task 6** — Mapbox (requires NEXT_PUBLIC_MAPBOX_TOKEN env var)
   - Rewrite `components/search/map-view.tsx`

After all 6 tasks: run `npm run build` — should compile with 0 errors.

---

*Last updated: 30 Apr 2026. Project owner: contact.dwarkesh@gmail.com*
*This document was auto-generated as a handoff brief.*

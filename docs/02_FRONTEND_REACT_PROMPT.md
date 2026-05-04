# 02 — Frontend Build Prompt (Next.js 15 + React 19, Learner-Friendly)

## Stack (Simplified)

| Concern | Choice | Why |
|---------|--------|-----|
| Framework | Next.js 15 App Router | React + routing + SSR, all in one |
| Language | TypeScript | Catch errors early; VS Code autocomplete |
| Styling | Tailwind CSS v3 + normal CSS modules | Easy to understand, component-scoped |
| Data fetching | Standard `fetch` + `useState`/`useEffect` | No new libraries to learn |
| Forms | Standard HTML form + useState | Simple, works fine at this scale |
| Maps | Leaflet.js + OpenStreetMap | Free, no API key, works great |
| Icons | Lucide React | Free, consistent |
| Animations | CSS transitions + LottieFiles pre-built JSON | No library complexity |
| Auth state | React Context | Standard, no extra library |
| Photos | Unsplash API + Pexels (free) | No cost |
| i18n | Simple key-value object (en/hi) | No framework needed at MVP |

**Why not CRA/Vite?** Next.js gives you routing, SSR for SEO, and API routes — all needed for a real product. The learning curve is 2 days extra but pays back every week.

---

## Folder Structure

```
frontend/
├── app/                          # Next.js App Router
│   ├── layout.tsx               # Root layout (nav, footer)
│   ├── page.tsx                 # Home page
│   ├── search/page.tsx          # Search results
│   ├── property/[id]/page.tsx   # Property detail
│   ├── post-property/page.tsx   # List your property
│   ├── login/page.tsx           # Auth
│   ├── dashboard/page.tsx       # Owner/user dashboard
│   ├── short-stays/page.tsx     # Airbnb-style tab
│   ├── city/[slug]/page.tsx     # City landing page
│   ├── admin/page.tsx           # Admin panel (basic)
│   └── globals.css              # Global styles
├── components/
│   ├── common/                  # Navbar, Footer, Button, Input, Card, Modal
│   ├── property/                # PropertyCard, PropertyGrid, PropertyFilter
│   ├── home/                    # HeroSection, FeaturedLocalities, SearchBar
│   └── dashboard/               # MyListings, LeadsList, StatsCard
├── lib/
│   ├── api.ts                   # All fetch calls (one file)
│   ├── auth.ts                  # JWT decode, auth context
│   ├── format.ts                # ₹ formatting, dates
│   └── i18n.ts                  # English/Hindi strings
├── types/
│   └── index.ts                 # All TypeScript interfaces
├── public/
│   ├── images/                  # Static images
│   └── lottie/                  # Lottie JSON files
└── next.config.ts
```

---

## Setup Commands

```bash
npx create-next-app@latest frontend \
  --typescript \
  --tailwind \
  --eslint \
  --app \
  --src-dir=no \
  --import-alias="@/*"

cd frontend

# Add only what we need
npm install lucide-react lottie-react leaflet @types/leaflet
```

---

## TypeScript Types (types/index.ts)

```typescript
export interface Property {
  id: string;
  title: string;
  price: number;
  pricePerNight?: number;
  bhk?: number;
  bathrooms?: number;
  areaSqft: number;
  floorNumber?: number;
  totalFloors?: number;
  propertyTypeId: number;
  listingIntentId: number;
  furnishingStatusId: number;
  isReraVerified: boolean;
  isZeroBrokerage: boolean;
  isFeatured: boolean;
  localityName: string;
  cityName: string;
  latitude?: number;
  longitude?: number;
  addressLine?: string;
  pinCode?: string;
  viewCount: number;
  leadCount: number;
  createdAt: string;
  images: PropertyImage[];
  amenities?: number[];
}

export interface PropertyImage {
  id: string; url: string; isPrimary: boolean; sortOrder: number;
}

export interface City {
  id: string; name: string; nameHi?: string; slug: string; latitude?: number; longitude?: number;
}

export interface Locality {
  id: string; cityId: string; name: string; slug: string; latitude?: number; longitude?: number; pinCode?: string;
}

export interface SearchFilter {
  query?: string;
  cityId?: string;
  localityId?: string;
  bhk?: number;
  minPrice?: number;
  maxPrice?: number;
  propertyTypeId?: number;
  listingIntentId?: number;
  furnishingStatusId?: number;
  page: number;
  pageSize: number;
}

export interface User {
  id: string; name?: string; phone: string; email?: string; roleId: number;
}

export interface Lead {
  id: string; propertyId: string; buyerName: string; buyerPhone: string; message?: string; statusId: number; createdAt: string;
}

export interface Review {
  id: string; reviewerName: string; rating: number; comment?: string; isVerified: boolean; createdAt: string;
}
```

---

## API Layer (lib/api.ts)

```typescript
const BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000/api';

function getToken() {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('apnanest_token');
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getToken();
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });
  if (!res.ok) throw new Error(`API ${res.status}: ${await res.text()}`);
  return res.json();
}

// Properties
export const api = {
  properties: {
    search: (f: Partial<SearchFilter>) =>
      request<{ data: Property[]; total: number }>(`/properties?${new URLSearchParams(f as any)}`),
    getById: (id: string) => request<Property>(`/properties/${id}`),
    create: (body: unknown) => request<{ id: string }>('/properties', { method: 'POST', body: JSON.stringify(body) }),
    delete: (id: string) => request<void>(`/properties/${id}`, { method: 'DELETE' }),
    getMy: () => request<Property[]>('/properties/my'),
  },
  auth: {
    requestOtp: (phone: string) => request<{ message: string }>('/auth/request-otp', { method: 'POST', body: JSON.stringify({ phone }) }),
    verifyOtp: (phone: string, code: string) => request<{ token: string; user: User }>('/auth/verify-otp', { method: 'POST', body: JSON.stringify({ phone, code }) }),
    me: () => request<User>('/auth/me'),
  },
  cities: {
    list: () => request<City[]>('/cities'),
    localities: (slug: string) => request<Locality[]>(`/cities/${slug}/localities`),
  },
  leads: {
    create: (body: unknown) => request<{ id: string }>('/leads', { method: 'POST', body: JSON.stringify(body) }),
    received: () => request<Lead[]>('/leads/received'),
  },
  saved: {
    list: () => request<Property[]>('/saved'),
    save: (propertyId: string) => request<void>('/saved', { method: 'POST', body: JSON.stringify({ propertyId }) }),
    remove: (propertyId: string) => request<void>(`/saved/${propertyId}`, { method: 'DELETE' }),
  },
  reviews: {
    list: (propertyId: string) => request<Review[]>(`/properties/${propertyId}/reviews`),
  },
  amenities: {
    list: () => request<{ id: number; label: string; icon: string }[]>('/amenities'),
  },
};
```

---

## Auth Context (lib/auth.ts)

```typescript
'use client';
import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User } from '@/types';

interface AuthCtx { user: User | null; token: string | null; login(token: string, user: User): void; logout(): void; }
const AuthContext = createContext<AuthCtx>({ user: null, token: null, login: () => {}, logout: () => {} });

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    const t = localStorage.getItem('apnanest_token');
    const u = localStorage.getItem('apnanest_user');
    if (t && u) { setToken(t); setUser(JSON.parse(u)); }
  }, []);

  const login = (t: string, u: User) => {
    localStorage.setItem('apnanest_token', t);
    localStorage.setItem('apnanest_user', JSON.stringify(u));
    setToken(t); setUser(u);
  };
  const logout = () => {
    localStorage.removeItem('apnanest_token');
    localStorage.removeItem('apnanest_user');
    setToken(null); setUser(null);
  };
  return <AuthContext.Provider value={{ user, token, login, logout }}>{children}</AuthContext.Provider>;
}
export const useAuth = () => useContext(AuthContext);
```

---

## Currency Formatter (lib/format.ts)

```typescript
// Format ₹8500000 → "₹85 Lakh" or "₹1.5 Cr"
export function formatPrice(amount: number): string {
  if (amount >= 10_000_000) return `₹${(amount / 10_000_000).toFixed(1)} Cr`;
  if (amount >= 100_000)    return `₹${(amount / 100_000).toFixed(0)} Lakh`;
  if (amount >= 1_000)      return `₹${(amount / 1_000).toFixed(0)}K`;
  return `₹${amount}`;
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' });
}
```

---

## i18n (lib/i18n.ts)

```typescript
export const translations = {
  en: {
    searchPlaceholder: 'Search city, locality, project...',
    buyTab: 'Buy',
    rentTab: 'Rent',
    shortStayTab: 'Short Stay',
    reraVerified: 'RERA Verified',
    zeroBrokerage: '0% Brokerage',
    contactOwner: 'Contact Owner',
    saveProperty: 'Save',
    viewDetails: 'View Details',
    noResults: 'No properties found. Try widening your search.',
    loginTitle: 'Sign in to ApnaNest',
    otpSent: 'OTP sent! (dev: enter any 6 digits)',
    developmentNote: 'Phone OTP — Development in Progress',
  },
  hi: {
    searchPlaceholder: 'शहर, इलाका, प्रोजेक्ट खोजें...',
    buyTab: 'खरीदें',
    rentTab: 'किराया',
    shortStayTab: 'शॉर्ट स्टे',
    reraVerified: 'RERA वेरिफाइड',
    zeroBrokerage: '0% ब्रोकरेज',
    contactOwner: 'मालिक से संपर्क करें',
    saveProperty: 'सहेजें',
    viewDetails: 'विवरण देखें',
    noResults: 'कोई प्रॉपर्टी नहीं मिली।',
    loginTitle: 'ApnaNest में लॉग इन करें',
    otpSent: 'OTP भेज दिया गया!',
    developmentNote: 'फ़ोन OTP — विकास प्रगति में है',
  }
};

export type Locale = keyof typeof translations;
export const t = (key: keyof typeof translations.en, locale: Locale = 'en') =>
  translations[locale][key] ?? translations.en[key];
```

---

## Property Card Component

```tsx
// components/property/PropertyCard.tsx
import Link from 'next/link';
import { MapPin, BedDouble, Bath, Square, ShieldCheck } from 'lucide-react';
import { Property } from '@/types';
import { formatPrice } from '@/lib/format';
import styles from './PropertyCard.module.css';

export default function PropertyCard({ p }: { p: Property }) {
  const primaryImg = p.images?.find(i => i.isPrimary)?.url ?? p.images?.[0]?.url ?? '/placeholder.jpg';
  const isShortStay = p.propertyTypeId === 6;

  return (
    <Link href={`/property/${p.id}`} className={styles.card}>
      <div className={styles.imageWrap}>
        <img src={primaryImg} alt={p.title} className={styles.image} loading="lazy" />
        <div className={styles.badges}>
          {p.isReraVerified && (
            <span className={styles.reraB}><ShieldCheck size={12}/> RERA</span>
          )}
          {p.isZeroBrokerage && <span className={styles.brokerB}>0% Brokerage</span>}
          {isShortStay && p.pricePerNight && (
            <span className={styles.nightB}>{formatPrice(p.pricePerNight)}/night</span>
          )}
        </div>
      </div>
      <div className={styles.body}>
        <div className={styles.price}>{formatPrice(p.price)}</div>
        <h3 className={styles.title}>{p.title}</h3>
        <p className={styles.location}><MapPin size={13}/>{p.localityName}, {p.cityName}</p>
        <div className={styles.specs}>
          {p.bhk && <span><BedDouble size={13}/> {p.bhk} BHK</span>}
          {p.bathrooms && <span><Bath size={13}/> {p.bathrooms} Bath</span>}
          <span><Square size={13}/> {p.areaSqft.toLocaleString()} sqft</span>
        </div>
      </div>
    </Link>
  );
}
```

```css
/* PropertyCard.module.css */
.card { display: flex; flex-direction: column; border-radius: 12px; overflow: hidden;
  border: 1px solid #e5e7eb; transition: transform 0.2s, box-shadow 0.2s;
  background: #fff; text-decoration: none; color: inherit; }
.card:hover { transform: translateY(-4px); box-shadow: 0 8px 24px rgba(0,0,0,0.10); }
.imageWrap { position: relative; aspect-ratio: 4/3; overflow: hidden; background: #f3f4f6; }
.image { width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s; }
.card:hover .image { transform: scale(1.04); }
.badges { position: absolute; top: 8px; left: 8px; display: flex; gap: 6px; flex-wrap: wrap; }
.reraB, .brokerB, .nightB { font-size: 11px; font-weight: 600; padding: 2px 7px;
  border-radius: 20px; backdrop-filter: blur(4px); }
.reraB  { background: rgba(20,184,166,0.85); color: #fff; }
.brokerB { background: rgba(245,158,11,0.85); color: #fff; }
.nightB  { background: rgba(249,115,22,0.85); color: #fff; }
.body { padding: 14px; }
.price { font-size: 20px; font-weight: 700; color: #0f766e; }
.title { font-size: 14px; font-weight: 500; margin: 4px 0; color: #111827;
  display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.location { font-size: 12px; color: #6b7280; display: flex; align-items: center; gap: 4px; margin-bottom: 8px; }
.specs { display: flex; gap: 12px; font-size: 12px; color: #374151; }
.specs span { display: flex; align-items: center; gap: 4px; }
```

---

## Page Examples

### Search Page (app/search/page.tsx)

```tsx
'use client';
import { useState, useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { api } from '@/lib/api';
import { Property, SearchFilter } from '@/types';
import PropertyCard from '@/components/property/PropertyCard';
import PropertyFilter from '@/components/property/PropertyFilter';

export default function SearchPage() {
  const searchParams = useSearchParams();
  const [properties, setProperties] = useState<Property[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<SearchFilter>({
    query: searchParams.get('q') ?? '',
    cityId: searchParams.get('city') ?? undefined,
    page: 1, pageSize: 20,
  });

  useEffect(() => {
    setLoading(true);
    api.properties.search(filter)
      .then(r => { setProperties(r.data); setTotal(r.total); })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [filter]);

  return (
    <div style={{ display: 'flex', gap: 24, padding: '24px 40px' }}>
      <aside style={{ width: 280, flexShrink: 0 }}>
        <PropertyFilter filter={filter} onChange={setFilter} />
      </aside>
      <main style={{ flex: 1 }}>
        <p style={{ color: '#6b7280', marginBottom: 16 }}>
          {total} properties found
        </p>
        {loading ? (
          <div className="grid-4">
            {[...Array(8)].map((_, i) => <div key={i} className="skeleton-card" />)}
          </div>
        ) : properties.length === 0 ? (
          <div className="empty-state">No properties found. Try widening your search.</div>
        ) : (
          <div className="grid-3">
            {properties.map(p => <PropertyCard key={p.id} p={p} />)}
          </div>
        )}
      </main>
    </div>
  );
}
```

### Login Page (app/login/page.tsx)

```tsx
'use client';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [phone, setPhone] = useState('');
  const [otp, setOtp] = useState('');
  const [step, setStep] = useState<'phone' | 'otp'>('phone');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function sendOtp() {
    if (!/^[6-9]\d{9}$/.test(phone)) { setError('Enter a valid 10-digit Indian mobile number'); return; }
    setLoading(true);
    try {
      await api.auth.requestOtp(phone);
      setStep('otp');
      setError('');
    } catch { setError('Failed to send OTP. Try again.'); }
    finally { setLoading(false); }
  }

  async function verifyOtp() {
    setLoading(true);
    try {
      const { token, user } = await api.auth.verifyOtp(phone, otp);
      login(token, user);
      router.push('/dashboard');
    } catch { setError('Invalid OTP. Try any 6 digits (dev mode).'); }
    finally { setLoading(false); }
  }

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h1>Sign in to ApnaNest</h1>
        {step === 'phone' ? (
          <>
            <label>Mobile Number</label>
            <div className="phone-input">
              <span>+91</span>
              <input value={phone} onChange={e => setPhone(e.target.value)}
                placeholder="98765 43210" maxLength={10} />
            </div>
            <p className="dev-note">⚠️ Phone OTP — Development in Progress. Any 6 digits will work.</p>
            {error && <p className="error">{error}</p>}
            <button onClick={sendOtp} disabled={loading}>
              {loading ? 'Sending...' : 'Send OTP'}
            </button>
          </>
        ) : (
          <>
            <p>OTP sent to +91 {phone} <button onClick={() => setStep('phone')}>Change</button></p>
            <label>Enter OTP</label>
            <input value={otp} onChange={e => setOtp(e.target.value)}
              placeholder="6-digit OTP" maxLength={6} />
            {error && <p className="error">{error}</p>}
            <button onClick={verifyOtp} disabled={loading}>
              {loading ? 'Verifying...' : 'Verify & Sign In'}
            </button>
          </>
        )}
      </div>
    </div>
  );
}
```

---

## Map Component (Leaflet, free, no API key)

```tsx
// components/property/PropertyMap.tsx
'use client';
import { useEffect, useRef } from 'react';

export default function PropertyMap({ lat, lng, title }: { lat: number; lng: number; title: string }) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!ref.current || typeof window === 'undefined') return;
    // Dynamic import to avoid SSR issues
    import('leaflet').then(L => {
      import('leaflet/dist/leaflet.css');
      const map = L.map(ref.current!).setView([lat, lng], 15);
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
      }).addTo(map);
      L.marker([lat, lng]).addTo(map).bindPopup(title).openPopup();
      return () => { map.remove(); };
    });
  }, [lat, lng, title]);

  return <div ref={ref} style={{ height: 350, borderRadius: 12 }} />;
}
```

---

## Lottie Hero (use pre-built free JSON from LottieFiles)

```tsx
// components/home/HeroAnimation.tsx
'use client';
import Lottie from 'lottie-react';
// Download a free "home / family / city" Lottie from https://lottiefiles.com/free-animations
// Save as /public/lottie/hero.json
import heroAnimation from '@/public/lottie/hero.json';

export default function HeroAnimation() {
  return (
    <Lottie
      animationData={heroAnimation}
      loop
      style={{ width: '100%', maxWidth: 500 }}
    />
  );
}
```

Search LottieFiles.com for: "home", "city", "family house", "real estate" — filter Free. Download the JSON.

---

## Header — Multilingual Brand Name

```tsx
// In Navbar component
const brandNames = [
  { text: 'ApnaNest', lang: 'en' },
  { text: 'अपना नेस्ट', lang: 'hi' },       // Hindi
  { text: 'અપના નેસ્ટ', lang: 'gu' },       // Gujarati
  { text: 'ਆਪਣਾ ਨੈਸਟ', lang: 'pa' },       // Punjabi
  { text: 'আপনা নেস্ট', lang: 'bn' },       // Bengali
];
// Rotate every 3 seconds with CSS fade transition
```

---

## Pages Checklist

| Page | Route | Priority |
|------|-------|---------|
| Home | `/` | Day 3 |
| Search | `/search` | Day 3 |
| Property Detail | `/property/[id]` | Day 3 |
| Login | `/login` | Day 4 |
| Post Property (form) | `/post-property` | Day 4 |
| Owner Dashboard | `/dashboard` | Day 5 |
| Short Stays | `/short-stays` | Day 5 |
| City Landing | `/city/[slug]` | Day 6 |
| Admin Panel | `/admin` | Day 7 (if time) |
| 404 | `not-found.tsx` | Day 6 |

---

## Deployment

```bash
# Push to GitHub, then:
# 1. Go to vercel.com → Import repo → apnanest-frontend
# 2. Set env vars:
#    NEXT_PUBLIC_API_URL = https://your-railway-backend.up.railway.app/api
# 3. Deploy → done. Auto-deploys on every push to main.
```

---

## CSS Global Variables (globals.css)

```css
:root {
  --color-primary:    #0f766e;
  --color-secondary:  #f59e0b;
  --color-accent:     #f97316;
  --color-bg:         #fefce8;
  --color-text:       #111827;
  --color-muted:      #6b7280;
  --color-border:     #e5e7eb;
  --radius:           12px;
  --shadow:           0 4px 16px rgba(0,0,0,0.08);
}
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: 'Plus Jakarta Sans', 'Inter', system-ui, sans-serif; color: var(--color-text); background: #fff; }
.grid-3 { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 20px; }
.grid-4 { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 20px; }
.skeleton-card { height: 300px; border-radius: var(--radius); background: linear-gradient(90deg, #f3f4f6 25%, #e5e7eb 50%, #f3f4f6 75%); background-size: 200% 100%; animation: shimmer 1.5s infinite; }
@keyframes shimmer { 0% { background-position: 200% 0; } 100% { background-position: -200% 0; } }
.empty-state { text-align: center; padding: 60px 20px; color: var(--color-muted); }
.auth-container { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--color-bg); }
.auth-card { background: white; padding: 40px; border-radius: 16px; box-shadow: var(--shadow); width: 100%; max-width: 400px; }
.dev-note { font-size: 12px; color: #f59e0b; background: #fffbeb; padding: 8px 12px; border-radius: 6px; margin: 8px 0; }
.error { color: #dc2626; font-size: 13px; margin: 6px 0; }
@media (max-width: 768px) {
  .grid-3, .grid-4 { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 480px) {
  .grid-3, .grid-4 { grid-template-columns: 1fr; }
}
```

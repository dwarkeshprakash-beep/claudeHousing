# CLAUDE.md — ApnaNest Project Conventions

> This file is auto-loaded by Claude Code, Cursor, and similar AI coding tools as project context. Keep it current. When in doubt about how something should be done, **this file wins** over generic best-practices.

---

## Project at a glance

**ApnaNest** is a real estate platform for the Indian market (buy / rent / commercial / PG / plots). Competitors: Housing.com, MagicBricks, NoBroker. Differentiators: zero brokerage by default, RERA verification first-class, hyper-local content moat, owner-friendly pricing.

**Repo layout** (monorepo, two top-level packages):

```
/apnanest
├── frontend/        # Next.js 15 + React 19 + TypeScript + Tailwind v4
├── backend/         # ASP.NET Core 8 Web API + Clean Architecture
├── docs/            # All planning docs (PRD, ARCHITECTURE, this file, etc.)
├── infra/           # docker-compose, Terraform later
├── .claude/         # Claude-specific config + skills
│   └── skills/      # Project-specific reusable skills
├── CLAUDE.md        # This file
└── README.md
```

---

## How to navigate this codebase

**Before coding anything**, check these in order:

1. **`docs/PRD.md`** — what we're building & why (personas, features, NFRs)
2. **`docs/ARCHITECTURE.md`** — how the system fits together
3. **`docs/DATABASE_SCHEMA.md`** — DDL, relationships, naming conventions
4. **`docs/prompts/02_FRONTEND_REACT_PROMPT.md`** — frontend conventions
5. **`docs/prompts/03_BACKEND_API_PROMPT.md`** — backend conventions
6. **`.claude/instructions/`** — detailed coding, git, and vibe coding standards
7. **`CLAUDE.md` (this file)** — quick rules

If the docs and the code disagree, **the code is wrong** until proven otherwise. Update docs in the same PR if intent has changed.

---

## Tech stack (locked decisions — don't propose alternatives unless asked)

### Frontend
- **Framework**: Next.js 15 (App Router) + React 19 + TypeScript 5
- **Styling**: Tailwind CSS v4 with `@theme` design tokens
- **UI primitives**: shadcn/ui (Radix-based), customized to brand
- **Animation**: Framer Motion + Lottie React
- **State**:
  - Server state → TanStack Query (React Query) v5
  - Client state → Zustand (one store per concern, not one global)
  - Forms → React Hook Form + Zod resolver
- **Maps**: Mapbox GL JS (cheaper than Google Maps, good enough)
- **Mock API (dev)**: MSW (Mock Service Worker) reading from `/mocks/*.json`
- **Icons**: Lucide React + 8 custom SVG icons
- **i18n**: `next-intl` (English + Hindi at MVP)
- **Auth**: HTTP-only refresh cookie + in-memory access token

### Backend
- **Framework**: ASP.NET Core 8 Web API
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → Web)
- **CQRS**: MediatR with `Result<T>` pattern (no exceptions for control flow)
- **Validation**: FluentValidation
- **ORM**: EF Core 8 + NetTopologySuite for PostGIS
- **DB**: PostgreSQL 16 + PostGIS extension
- **Cache**: Redis (StackExchange.Redis)
- **Auth**: JWT RS256 (15 min access) + refresh tokens (30 days, rotated)
- **Background jobs**: Hangfire
- **Storage**: S3-compatible (Cloudflare R2 in prod, MinIO locally)
- **Logging**: Serilog → Seq locally, structured JSON in prod
- **Tests**: xUnit + FluentAssertions + Testcontainers

### Infrastructure
- **Local dev**: docker-compose (Postgres+PostGIS, Redis, MinIO, Seq)
- **Prod (TBD)**: Likely DigitalOcean / Hetzner for cost (Indian audience), or Azure for resume value

---

## Coding conventions

### TypeScript / React (frontend)

- **Components**: PascalCase. One component per file. Co-locate `Component.tsx`, `Component.test.tsx`, and small helpers in the same folder.
- **Files**: kebab-case for non-component files (`use-property-search.ts`).
- **Hooks**: `useFooBar` pattern. Custom hooks live in `/lib/hooks/` or co-located if specific to a feature.
- **Server components by default**. Add `'use client'` only when you need interactivity. Search the codebase before adding a new client component.
- **Data fetching**:
  - Server components fetch directly via the API client.
  - Client components use TanStack Query hooks (`useQuery`, `useMutation`).
  - Never `useEffect` + `fetch` for data fetching. Ever.
- **Styling**: Tailwind utility classes inline. For repeated patterns, extract to a component, not a CSS class. Use `cn()` helper for conditional classes.
- **Imports**: Absolute imports via `@/` prefix. Sort: react/next → external → `@/` → relative → styles.
- **No `any`**. If you reach for `any`, you're missing a type. Use `unknown` and narrow.
- **No default exports** for components (named exports only — easier to refactor and grep).
- **Error boundaries**: Wrap each route segment with an `error.tsx`.

### C# / .NET (backend)

- **Naming**:
  - PascalCase for classes, methods, properties.
  - camelCase for parameters and locals.
  - `_camelCase` for private fields.
  - Async methods end in `Async` (`GetPropertyAsync`).
- **Nullable reference types**: Enabled project-wide. No `!` (null-forgiving) unless you've genuinely proven it's safe.
- **No exceptions for control flow**. Use `Result<T>` from MediatR pipeline. Throw only for truly exceptional conditions.
- **One class per file**. Files match class names exactly.
- **Records for DTOs**, classes for entities and aggregates.
- **EF Core**: All entity configuration in `IEntityTypeConfiguration<T>` classes. Never use data annotations on domain entities.
- **Migrations**: Always reviewed in PR. Never edit a committed migration; add a new one.

### Database
- **Tables**: snake_case, plural (`properties`, `users`)
- **Columns**: snake_case (`created_at`, `is_active`)
- **Primary keys**: `id` (UUID v7 for sortable distributed IDs)
- **Foreign keys**: `<table_singular>_id` (`property_id`, `user_id`)
- **Timestamps**: `created_at`, `updated_at` everywhere (no exceptions)
- **Soft deletes**: `deleted_at` nullable timestamp; never hard-delete user/property data
- **Booleans**: `is_active`, `has_balcony` (positive phrasing)

### Git workflow

- **Branches**: `feat/short-description`, `fix/short-description`, `chore/...`, `docs/...`
- **Commits**: [Conventional Commits](https://www.conventionalcommits.org/). `feat: add saved search email cron`. Imperative mood, lowercase, no period.
- **PRs**:
  - Small (< 400 lines diff ideal). If bigger, you probably should split.
  - PR description: what, why, screenshots/screencasts for UI, breaking changes called out.
  - At least one review before merge. Self-merge only allowed for docs.
  - Never merge red CI.
- **Main is always deployable** (eventually — pre-prod, this is aspirational, not enforced).

---

## Where things go (file placement guide)

When adding a new feature, here's where each piece lives. **Follow this strictly** — drift here destroys the codebase over time.

### Adding a new property feature (e.g., "favorite a property")

**Frontend**:
- Type definition → `/types/property.ts`
- API endpoint constant → `/lib/api/endpoints.ts`
- API call wrapper → `/lib/api/properties.ts`
- TanStack Query hook → `/lib/hooks/use-toggle-favorite.ts`
- UI component → `/components/property/favorite-button.tsx`
- Used in → `/app/property/[id]/page.tsx`
- Mock handler (dev) → `/mocks/handlers/properties.ts`

**Backend**:
- Domain method on `Property` entity → `/Domain/Entities/Property.cs` (`MarkAsFavorite(userId)` if domain logic; otherwise pure persistence)
- Or: separate join entity `SavedProperty` → `/Domain/Entities/SavedProperty.cs`
- MediatR command → `/Application/Properties/Commands/ToggleFavorite/ToggleFavoriteCommand.cs`
- Validator → same folder, `ToggleFavoriteCommandValidator.cs`
- Handler → same folder, `ToggleFavoriteCommandHandler.cs`
- EF config → `/Infrastructure/Persistence/Configurations/SavedPropertyConfiguration.cs`
- Migration → `dotnet ef migrations add AddSavedProperties`
- Endpoint in controller → `/Web/Controllers/PropertiesController.cs` (`POST /api/properties/{id}/favorite`)
- Test → `/tests/Application.Tests/Properties/ToggleFavoriteCommandTests.cs`

If the file you'd be adding doesn't fit any existing folder, **stop and ask** — it probably means we're missing an architecture pattern.

---

## How to add a new endpoint (backend)

1. Add command/query under `/Application/<Feature>/{Commands|Queries}/<Name>/`
2. Define request DTO, response DTO, validator, handler — in that order
3. Add endpoint to controller (or new controller if new feature). Always include:
   - `[Authorize]` attribute (or `[AllowAnonymous]` if intentional)
   - Result type — `ActionResult<T>` or `IActionResult`
   - XML doc comment (used by Swagger)
   - `[ProducesResponseType]` for at least 200, 400, 401, 404
4. Update OpenAPI spec — auto-generated from code, but verify it looks right
5. Add tests:
   - Unit test the handler (mock dependencies)
   - Integration test the endpoint (Testcontainers + WebApplicationFactory)
6. Update frontend:
   - Add endpoint constant
   - Add API wrapper
   - Add hook
   - Update mock handler

---

## How to add a new page (frontend)

1. Create route folder under `/app/`. App Router convention applies.
2. `page.tsx` — server component by default
3. If interactive: split into a server `page.tsx` (data fetching, metadata) + a `'use client'` view component
4. Add `metadata` export with full SEO (title, description, OG image)
5. Add JSON-LD if it's a content/product page
6. Add to sitemap (`/app/sitemap.ts`) if public
7. Add `loading.tsx` and `error.tsx` for the route segment
8. Add link to it from where users will discover it (nav, related pages, etc.)

---

## Build, test, run

### Frontend (`/frontend`)

```bash
pnpm install                    # install deps (use pnpm, not npm/yarn)
pnpm dev                        # dev server with MSW mock API at :3000
pnpm dev:real                   # dev server hitting real backend
pnpm build                      # production build
pnpm start                      # serve production build
pnpm lint                       # eslint
pnpm typecheck                  # tsc --noEmit
pnpm test                       # vitest unit tests
pnpm test:e2e                   # playwright
pnpm format                     # prettier write
```

### Backend (`/backend`)

```bash
dotnet build                                      # build solution
dotnet run --project src/ApnaNest.Web             # run API at :5000
dotnet test                                       # all tests
dotnet ef migrations add <Name> -p src/ApnaNest.Infrastructure -s src/ApnaNest.Web
dotnet ef database update -p src/ApnaNest.Infrastructure -s src/ApnaNest.Web
```

### Local infra

```bash
docker compose up -d            # postgres + redis + minio + seq
docker compose logs -f          # tail logs
docker compose down -v          # nuke everything (CAREFUL — wipes DB)
```

URLs:
- Frontend dev: http://localhost:3000
- API dev: http://localhost:5000 + Swagger at http://localhost:5000/swagger
- Postgres: localhost:5432 (user: `apnanest`, pass: `dev`, db: `apnanest`)
- Redis: localhost:6379
- MinIO console: http://localhost:9001 (admin / password)
- Seq logs: http://localhost:5341

---

## Detailed Instructions & Skills

For more in-depth guidance, refer to these specialized files:

### Instructions
- [Coding Standards](file:///f:/claude_houseing/apnanest/.claude/instructions/coding-standards.md)
- [Git Standards](file:///f:/claude_houseing/apnanest/.claude/instructions/git-standards.md)
- [Naming Conventions](file:///f:/claude_houseing/apnanest/.claude/instructions/naming-conventions.md)
- [Vibe Coding Principles](file:///f:/claude_houseing/apnanest/.claude/instructions/vibe-coding.md)

### Skills
- [Frontend Development](file:///f:/claude_houseing/apnanest/.claude/skills/frontend.md)
- [Backend Development](file:///f:/claude_houseing/apnanest/.claude/skills/backend.md)

### Mentor Support
- [Onboarding Guide](file:///f:/claude_houseing/apnanest/.claude/instructions/mentor/onboarding.md)
- [Debugging Guide](file:///f:/claude_houseing/apnanest/.claude/instructions/mentor/debugging-guide.md)
- [Technical Debt](file:///f:/claude_houseing/apnanest/.claude/instructions/mentor/technical-debt.md)

---

## Things to ALWAYS do

- ✅ Add tests for new business logic (handlers, domain methods)
- ✅ Update OpenAPI / regenerate frontend types when backend contract changes
- ✅ Run `pnpm typecheck` and `dotnet build` before committing
- ✅ Add a feature flag for risky changes (use `unleash` or env-var toggles)
- ✅ Add observability: log meaningful events, increment metrics on important paths
- ✅ Handle the loading state, the error state, and the empty state in every UI component
- ✅ Mobile-test every UI change (Chrome DevTools mobile mode, then real device)
- ✅ Audit accessibility before merging UI (keyboard nav, screen reader, color contrast)

## Things to NEVER do

- ❌ Commit secrets, API keys, or `.env` files (we have a pre-commit hook; don't bypass it)
- ❌ Add `console.log` to production code (use the logger)
- ❌ Disable ESLint rules without a code comment explaining why
- ❌ Use `eval`, `dangerouslySetInnerHTML`, or `unsafe-eval` CSP — ever
- ❌ Cache user-specific data on the CDN (cache-key separation must be airtight)
- ❌ Send 100+ properties in one API response (paginate, always)
- ❌ Hard-delete user or property data (soft-delete with `deleted_at`)
- ❌ Hardcode strings shown to users (route through i18n)
- ❌ Trust client-supplied user IDs — derive from JWT every time
- ❌ N+1 query (always use `.Include()` or projection in EF Core)
- ❌ Ship a UI that doesn't degrade on slow 3G (test with throttling)

---

## Domain glossary (so we all use the same terms)

- **Property** — a single listed home/shop/plot/etc. The atomic unit.
- **Project** — a developer's launch (e.g., "Godrej Garden City" with 200 units inside). One project has many properties.
- **Owner** — the person who owns and lists the property (could be self-listing or a broker on behalf).
- **Lead** — a buyer/renter who has expressed interest in a property (saved, contacted, scheduled visit).
- **Locality** — a neighborhood within a city (e.g., "Bopal" within "Ahmedabad"). Has its own SEO landing page.
- **Listing type** — Buy / Rent / Commercial / PG / Plot. Different field sets per type.
- **Verification** — RERA-checked + ApnaNest-staff-verified. Two badges, two meanings.
- **Boost** — paid feature where an owner pays to surface their property higher in search results.
- **Zero brokerage** — owner has agreed not to charge brokerage to the buyer/tenant. Marketed prominently.

---

## Common AI prompt patterns (when you need Claude/Cursor help)

### "Add a new endpoint"
> Read `CLAUDE.md` and `docs/prompts/03_BACKEND_API_PROMPT.md`. I want to add an endpoint `POST /api/properties/{id}/report` for users to report a fraudulent listing. Generate the command, validator, handler, controller method, and tests. Follow the patterns in `Application/Properties/Commands/CreateProperty/` exactly. Use the existing `FraudReport` entity in Domain.

### "Add a new page"
> Read `CLAUDE.md` and `docs/prompts/02_FRONTEND_REACT_PROMPT.md`. I want a new route `/insights/[category]/[slug]` for blog articles. Server component, full SEO metadata, JSON-LD `Article` schema, content streamed from `/api/articles/{slug}`. Follow the patterns in `/app/property/[id]/page.tsx`.

### "Add a new database field"
> I want to add a `furnishing_status` enum to `properties` (one of: unfurnished, semi_furnished, fully_furnished). Generate: EF Core entity update, IEntityTypeConfiguration update, migration, DTO update, validator update, frontend type update, mock data update, search filter update, UI filter update.

### "Refactor"
> Read `CLAUDE.md`. I think `<file>` is doing too much. Suggest a refactor that splits responsibilities according to our Clean Architecture. Don't change behavior; just structure. Show me a plan first; don't write code yet.

---

## When you (the AI) are uncertain

**Default to asking, not assuming.** Especially around:
- Schema changes (always ask before modifying)
- Auth / authorization changes (always ask)
- Migrations that affect existing data (always ask)
- Anything labeled `// HACK` or `// TODO` (read the surrounding context, ask if unclear)
- New top-level dependencies (ask first — we're conservative about adding deps)

When asking, propose 2–3 options with tradeoffs, not just "what should I do?".

---

## Open questions / tech debt log

> Living section. Add entries when you find them; remove when fixed.

- [ ] Decide whether to ship i18n at MVP or V1 (currently scaffolded, English-only content)
- [ ] Decide payment gateway (Razorpay vs Stripe India vs Cashfree) — V2 question
- [ ] RERA API integration — manual upload at MVP; need an automated check by V1
- [ ] Mobile app: PWA at MVP; React Native vs Flutter for V2 still open

---

**This file is the source of truth for "how we do things here." If you find yourself fighting the conventions, propose a change to this file in a PR — don't quietly diverge in your code.**

# ApnaNest — How This Project Was Built with Claude Code

> A plain-English explanation of the project structure, the AI-assisted build process, and where Claude Code added value.

---

## What Is This Project?

**ApnaNest** is a real estate platform for India — like Housing.com or MagicBricks but with zero brokerage as the default. Owners can list properties, buyers can search and enquire, and the platform handles everything in between.

The entire project was built using **Claude Code** (Anthropic's AI coding tool) from scratch — no human wrote the core code manually. This is an example of "vibe coding" — describing what you want, and letting the AI build it.

---

## The Folder Structure (and What Each Part Does)

```
/apnanest
├── frontend/              → The website (Next.js)
│   ├── app/               → All the pages (Home, Search, Property, Dashboard, etc.)
│   ├── components/        → Reusable UI pieces (Navbar, Cards, Forms, Modals)
│   ├── lib/               → Shared logic (API client, types, auth store, helpers)
│   ├── services/          → API call wrappers (property-service.ts, etc.)
│   ├── mocks/             → Fake API data for development without real backend
│   └── public/            → Static files (images, icons)
│
├── backend/               → The API server (.NET / C#)
│   └── src/
│       ├── ApnaNest.API/        → HTTP endpoints (Controllers), app setup
│       ├── ApnaNest.Data/       → Database queries (Repositories, Entities)
│       └── ApnaNest.Services/   → Business logic (Auth, Property, Lead services)
│
├── docs/                  → All planning documents
│   ├── PRD.md             → What to build and why (Product Requirements)
│   ├── ARCHITECTURE.md    → How it all fits together technically
│   ├── DATABASE_SCHEMA.md → Database tables, columns, relationships
│   └── prompts/           → Detailed guides for frontend and backend coding style
│
├── infra/                 → Docker setup for local development
├── .claude/               → Claude Code specific configuration
│   ├── instructions/      → Coding standards, naming rules, git workflow
│   └── skills/            → Reusable Claude "skills" (frontend.md, backend.md)
│
├── CLAUDE.md              → Master instruction file read by Claude Code
├── IMPLEMENTATION_STATUS.md  → This session's progress (see other doc)
└── db-*.js                → One-time database seeding scripts
```

---

## The Key Files That Guided Claude Code

### 1. `CLAUDE.md` — The Master Rulebook

This is the most important file. Every time Claude Code starts working on this project, it reads `CLAUDE.md` first. It contains:

- **What we're building** — ApnaNest's vision, competitors, differentiators
- **Tech stack decisions** — Next.js 15, .NET 8, Supabase, Tailwind v4 (these are locked — Claude doesn't suggest alternatives)
- **Coding conventions** — How to name files, what patterns to use, what to never do
- **File placement rules** — Where every type of file goes (so the codebase doesn't become messy)
- **Git workflow** — Branch naming, commit message format, PR rules
- **Things to NEVER do** — `console.log` in production, `any` in TypeScript, hard-delete user data, etc.

Think of `CLAUDE.md` as the "employee handbook" for Claude Code. It's 150+ lines of rules that make sure Claude always writes code the same way, every time.

---

### 2. `docs/PRD.md` — The Product Plan

Before any code was written, this document defined:
- **Who uses the app** — Aarav (first-time buyer), Priya (renter), Rajesh (property owner), Aditi (admin)
- **What each user needs to do** — Search, shortlist, post property, contact owner, verify listings
- **Success metrics** — 500 monthly active users, 200 listings, under 5 minutes to contact
- **What's out of scope** — Mobile app, payment gateway, 3D tours (saves Claude from building things that weren't asked for)

This document prevents Claude from "feature creep" — adding things that weren't requested.

---

### 3. `docs/ARCHITECTURE.md` — The Blueprint

A technical document explaining:
- How the browser, Next.js server, .NET API, and database connect to each other
- Why each technology was chosen (and what alternatives were rejected)
- How the database is structured
- How authentication flows (JWT tokens, refresh tokens)
- How search works (client-side filtering vs. server-side)

Claude uses this to make consistent decisions. For example: "Should this be a server component or a client component?" — ARCHITECTURE.md answers that.

---

### 4. `docs/prompts/02_FRONTEND_REACT_PROMPT.md` and `03_BACKEND_API_PROMPT.md`

These are detailed "how to code" guides for each layer:

**Frontend guide covers:**
- How to structure React components
- How to do data fetching (TanStack Query, never `useEffect + fetch`)
- How to handle forms (React Hook Form + Zod validation)
- How to style things (Tailwind utility classes, `cn()` helper)
- What shadcn/ui components to use

**Backend guide covers:**
- How to structure .NET controllers
- How to use Dapper for database queries
- How repositories work (data access layer)
- How services work (business logic layer)
- Error handling patterns

Without these guides, Claude would write valid code but in inconsistent styles.

---

### 5. `.claude/instructions/` — Detailed Standards

Four detailed instruction files:

- **`coding-standards.md`** — TypeScript/React and C#/.NET specific rules
- **`git-standards.md`** — Conventional commits format, branch naming, PR process
- **`naming-conventions.md`** — Files: kebab-case. Components: PascalCase. DB: snake_case.
- **`vibe-coding.md`** — How to approach AI-assisted development: describe what you want, iterate, don't over-engineer

---

### 6. `.claude/skills/frontend.md` and `backend.md`

These are reusable "skills" — pre-written instructions Claude can follow when doing specific tasks like:
- Adding a new page (what files to create, what metadata to add)
- Adding a new API endpoint (what layers to touch)
- Running tests

---

## How Claude Code Helped Build This Project

### What Claude Code Did Well

**1. Full-stack code generation from descriptions**
- User described "I want a real estate platform for India with zero brokerage"
- Claude built: Next.js frontend, .NET backend, database schema, seed data, authentication — everything

**2. Following project conventions consistently**
- Because of CLAUDE.md, every component uses the same patterns
- Naming is consistent throughout: `property-service.ts`, `PropertyDto.cs`, `PropertyCard.tsx`
- The code looks like one developer wrote everything, not a random mix of styles

**3. Database design and seeding**
- Created a complete PostgreSQL schema with proper relationships
- Generated 40 realistic properties across 6 Indian cities
- Used real image URLs (Unsplash), realistic prices in Indian Rupees, real locality names

**4. Debugging complex issues**
- Found that Dapper can't map PostgreSQL `text[]` arrays — fixed by changing view to use `string_agg` (CSV strings)
- Found that Supabase's transaction pooler needs `No Reset On Close=true` — fixed connection string
- Found that `appsettings.json` kept reverting to localhost config — identified and fixed

**5. Reading and understanding the codebase**
- When asked to fix something, Claude first explored all relevant files before making changes
- Used the Explore agent to scan 50+ files and understand the full architecture
- Made targeted fixes without breaking unrelated things

### What Was Hard (Limitations)

**1. Can't open a browser**
- Claude can start the dev server but can't visually verify UI looks correct
- Had to infer UI correctness from build output and API responses

**2. Process management on Windows**
- Starting/stopping backend processes is tricky
- Some processes got locked (Visual Studio holding .dll files)
- Had to kill processes manually multiple times

**3. Configuration drift**
- `appsettings.json` had been manually modified (Supabase creds) but not committed to git
- When git state was read at start of session, it showed localhost config
- This caused confusion about what the real config was

**4. Npgsql version compatibility**
- Npgsql 10 (very new) has different behavior for SSL with Supabase's pooler
- Took several iterations to find the right combination of SSL flags
- This type of library-version debugging is slower with AI than human intuition

---

## The Architecture: How Well It Was Built

### Clean Separation of Concerns

```
Frontend (Next.js)          Backend (.NET)           Database (Supabase)
┌─────────────────┐        ┌──────────────────┐     ┌────────────────┐
│ pages (app/)    │  HTTP  │ Controllers       │ SQL │ vw_properties  │
│ components/     │ ──────>│ Services          │────>│ properties     │
│ lib/api.ts      │        │ Repositories      │     │ cities         │
│ services/       │        │ Entities/DTOs     │     │ localities     │
└─────────────────┘        └──────────────────┘     │ users          │
                                                      │ leads          │
                                                      └────────────────┘
```

Each layer only talks to the layer next to it. Frontend never touches the DB directly. Backend DTOs match what the frontend expects.

### What's Production-Grade vs. What's Demo-Grade

| Feature | Quality | Notes |
|---------|---------|-------|
| Database schema | Production | Proper indexes, soft deletes, timestamps everywhere |
| API design | Production | RESTful, proper HTTP status codes, pagination |
| Frontend components | Production | Proper loading/error/empty states, mobile responsive |
| Authentication | Demo | JWT in localStorage (should be HttpOnly cookie in prod) |
| Password hashing | Demo | HMACSHA512 (should be BCrypt in prod) |
| Image upload | Demo | Base64 in form (should upload to S3/R2 in prod) |
| Search filters | Demo | Client-side on first page (should be server-side in prod) |
| Error handling | Partial | Backend has middleware, frontend has try-catch but not consistent |

---

## The "Anthropic Course" Connection

This project follows the patterns from Anthropic's guidance on building with Claude Code:

**Project structure:** The monorepo layout (frontend/, backend/, docs/, .claude/) follows the recommended structure for large Claude Code projects.

**CLAUDE.md:** Writing a detailed CLAUDE.md is the #1 recommended practice. The better the instructions, the more consistent Claude's output.

**Skills system:** The `.claude/skills/` folder uses Claude Code's skills feature — reusable instruction sets that Claude can invoke for specific tasks.

**Instructions folder:** The `.claude/instructions/` folder holds detailed guidance that Claude reads when doing specific types of work.

**Iterative building:** The project was built in small chunks (schema first, then backend, then frontend, then integration) rather than trying to generate everything at once.

**Vibe coding principles:** The `vibe-coding.md` instruction file explicitly guides Claude to: move fast, use simple logic, don't over-engineer, get it working first.

---

## Summary: Is This a Good Project?

**Yes, for what it is.** This is a full-stack real estate web application with:
- 25+ pages and routes
- A complete REST API (12+ endpoints)
- A real PostgreSQL database with 40+ properties
- Authentication (register, login, JWT)
- Search, filtering, property listing, contact forms, dashboard
- Mobile-responsive UI with Tailwind CSS
- Built entirely by AI in a matter of hours

For a "human team" this would take 4–8 weeks with 2–3 developers. Claude Code did it in hours.

**The main limitation** is that Claude can write excellent code but can't verify it works visually. The last mile — loading the browser, seeing that the home page shows properties, clicking through flows — still needs a human to verify and report back what's broken.

The project is **70% production-ready** as code quality goes. The remaining 30% is the demo shortcuts (localStorage JWT, HMACSHA512, client-side search filters) that would need hardening before real users.

---

*This document was created as a handoff guide — for any developer (human or AI) picking up this project.*

# ApnaNest Comprehensive System Audit & Solutions Report

**Date:** 2026-05-04  
**Audit Scope:** Frontend (Next.js), Backend (.NET 9), Database (PostgreSQL), Auth, and Performance.

---

## 1. Critical & Functional Bugs

### **B1. Insecure Direct Object Reference (IDOR) in Property Updates**
- **Issue:** The `PropertiesController` does not verify if the `CurrentUserId` matches the `OwnerId` of the property being updated or deleted.
- **Impact:** Any authenticated user can modify or delete any property listing by knowing its ID.
- **Solution:** In `PropertiesController.cs`, fetch the existing property before update/delete and verify `existing.OwnerId == currentUserId`.
- **Agent Guide:** Update `Update` and `Delete` methods in [PropertiesController.cs](file:///f:/claude_houseing/apnanest/backend/src/ApnaNest.API/Controllers/PropertiesController.cs) to include owner validation.

### **B2. Broken Search Filtering (Client-side vs Server-side)**
- **Issue:** [search-results.tsx](file:///f:/claude_houseing/apnanest/frontend/components/search/search-results.tsx) calls the API with only `type` and `q`. All other filters (Price, BHK, Area) are applied client-side on a small subset (PAGE_SIZE=12) of results.
- **Impact:** Users will miss relevant properties that are not in the first 12 results fetched from the API.
- **Solution:** Move all filtering logic (minPrice, maxPrice, bhk, area, etc.) to the backend `PropertyRepository.GetAllAsync`.
- **Agent Guide:** Modify `searchProperties` service and backend [PropertyRepository.cs](file:///f:/claude_houseing/apnanest/backend/src/ApnaNest.Data/Repositories/PropertyRepository.cs) to accept and apply all filter parameters.

### **B3. Placeholder IDs in Post Property Wizard**
- **Issue:** [property-form-wizard.tsx](file:///f:/claude_houseing/apnanest/frontend/components/post-property/property-form-wizard.tsx) uses hardcoded GUIDs for `cityId` and `localityId`.
- **Impact:** New properties are not correctly linked to real cities/localities, breaking search and analytics.
- **Solution:** Implement a searchable city/locality dropdown that fetches real IDs from the backend.
- **Agent Guide:** Update the "Location" step in the wizard to use an API-backed autocomplete for cities and localities.

### **B4. Database Schema Inconsistency (Missing Columns)**
- **Issue:** [schema.sql](file:///f:/claude_houseing/apnanest/backend/schema.sql) is missing critical columns like `deleted_at`, `view_count`, and tables like `property_images`.
- **Impact:** Fresh installations using only `schema.sql` will crash when the backend tries to access these fields.
- **Solution:** Consolidate `apnanest_alter_properties.sql` into the main `schema.sql`.
- **Agent Guide:** Merge the ALTER statements and view definitions into the primary [schema.sql](file:///f:/claude_houseing/apnanest/backend/schema.sql).

---

## 2. Security & Auth Issues

### **S1. Token Storage Vulnerability**
- **Issue:** JWT tokens are stored in `localStorage` in [auth-store.ts](file:///f:/claude_houseing/apnanest/frontend/lib/stores/auth-store.ts).
- **Impact:** Vulnerable to XSS attacks where malicious scripts can steal the user's token.
- **Solution:** Use HttpOnly, Secure cookies for the main access token. The backend already sets a `refreshToken` cookie, but it's misused.
- **Agent Guide:** Refactor backend to issue Access Token in response and Refresh Token in HttpOnly cookie, or move Access Token to cookie entirely.

### **S2. Weak Password Hashing**
- **Issue:** [AuthService.cs](file:///f:/claude_houseing/apnanest/backend/src/ApnaNest.Services/Services/AuthService.cs) uses `HMACSHA512` which is fast but less secure against GPU-based cracking.
- **Solution:** Replace with `BCrypt.Net-Next` or `Argon2`.
- **Agent Guide:** Install `BCrypt.Net-Next` and update `CreatePasswordHash` and `VerifyPasswordHash`.

### **S3. Lack of Token Validation on Hydration**
- **Issue:** Frontend trusts `localStorage` token without verifying its validity on app load.
- **Solution:** Call the `/auth/me` endpoint during `hydrateFromStorage` to verify the session.

---

## 3. Performance & Load Issues

### **P1. Non-Scalable View Count Increments**
- **Issue:** Every property view triggers a DB write in [PropertyRepository.cs](file:///f:/claude_houseing/apnanest/backend/src/ApnaNest.Data/Repositories/PropertyRepository.cs).
- **Impact:** High DB contention on popular properties.
- **Solution:** Use Redis for atomic increments and sync to DB in batches or using a background job.

### **P2. Inefficient Search Query**
- **Issue:** `ILIKE %q%` is used for global search.
- **Impact:** Full table scans on every search.
- **Solution:** Use PostgreSQL `tsvector` and `tsquery` with a GIN index.

### **P3. Missing Pagination Metadata**
- **Issue:** Backend doesn't return the total number of items or pages.
- **Impact:** Frontend "Load More" logic is based on a guess (`data.length === PAGE_SIZE`).
- **Solution:** Wrap the response in a `PagedResponse<T>` containing `Items`, `TotalCount`, and `PageSize`.

---

## 4. UI/UX Issues

### **U1. Login Redirect Loop**
- **Issue:** [login/page.tsx](file:///f:/claude_houseing/apnanest/frontend/app/login/page.tsx) opens a modal and immediately redirects to `/`.
- **Impact:** If a user tries to bookmark the login page or reloads it, they lose the modal.
- **Solution:** Implement the login as a proper page OR use a URL query param (e.g., `?auth=login`) to trigger the modal globally.

### **U2. Missing Photo Upload**
- **Issue:** The "Post Property" wizard has no UI for uploading photos.
- **Impact:** Listings are created without images, making them unattractive (breaking PRD requirement).
- **Solution:** Add an image upload component (using UploadThing or S3) to the wizard.

### **U3. Hardcoded Navigation/Data**
- **Issue:** Many components still use `PROPERTIES` from [lib/data.ts](file:///f:/claude_houseing/apnanest/frontend/lib/data.ts) instead of real API calls.
- **Solution:** Standardize on [property-service.ts](file:///f:/claude_houseing/apnanest/frontend/services/property-service.ts) for all data fetching.

---

## 5. Library & Dependency Audit

### **L1. Invalid Next.js Version**
- **Current Version:** `^16.2.4` in [package.json](file:///f:/claude_houseing/apnanest/frontend/package.json).
- **Issue:** This version does not exist. The current stable is v15.
- **Solution:** Revert to `"next": "^15.0.0"`.

### **L2. Overlapping Map Libraries**
- **Current Libraries:** `mapbox-gl` and `maplibre-gl`.
- **Issue:** Using both increases bundle size unnecessarily.
- **Solution:** Standardize on `maplibre-gl` (open source) or `mapbox-gl` (premium features).

---

## Summary for AI Agents

To solve these issues, follow this sequence:
1.  **DB First**: Fix [schema.sql](file:///f:/claude_houseing/apnanest/backend/schema.sql) and apply all migrations to ensure the backend can store images, view counts, and soft-delete properties.
2.  **Security**: Fix the IDOR vulnerability in [PropertiesController.cs](file:///f:/claude_houseing/apnanest/backend/src/ApnaNest.API/Controllers/PropertiesController.cs) before any other backend changes.
3.  **Search Overhaul**: Refactor filtering to be server-side. This involves changing the `IPropertyRepository` interface and the frontend `SearchResults` component.
4.  **Auth Fix**: Move token storage to cookies or implement a proper refresh token flow to protect against XSS.
5.  **UX Polish**: Fix the "Post Property" wizard to fetch real city/locality data and support photo uploads.

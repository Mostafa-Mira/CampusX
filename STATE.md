# CampusX project state

## 1. Project Overview

CampusX is a student marketplace for study-related items. The working milestone is `Milestone-3/`, the ASP.NET Core MVC application; milestone acceptance and overall completion are not established.

Stack: .NET 8, C#, Razor HTML/CSS/JavaScript, EF Core 8.0.4, and SQL Server (`Milestone-3/CampusX.csproj`, `Milestone-3/Program.cs`). Scope comes from `Milestone-0/CampusX_SRS.docx` and supporting `Milestone-0/CampusX_BRD.docx`; source priority and approved design direction are recorded in `AGENTS.md`.

## 2. Current Implementation Status

Snapshot from source inspection at `d56781d`. Reinspect before each task. Application paths below are relative to `Milestone-3/` unless explicitly prefixed.

Statuses: **Completed** = implemented and verified; **Partial** = incomplete implementation; **Pending** = not implemented; **Blocked** = cannot proceed due to a dependency; **Unverified** = insufficient verification evidence. No complete feature is marked Completed in this review.

| Area | Status | Evidence and limits |
| --- | --- | --- |
| Home Page & Product Listing | Unverified | `Controllers/HomeController.cs`, `Views/Home/Index.cshtml`, `Views/Shared/_ProductCard.cshtml`, `wwwroot/css/site.css`: database listing/count, cards, empty/error markup, responsive rules. Historical Home smoke test passed; current revision/browser behavior not retested. |
| Product Details | Unverified | `Controllers/HomeController.cs`, `Views/Home/Details.cshtml`: lookup/404, product/seller/status display, image fallback. No current runtime verification. |
| Search, Filter & Sort | Partial | `Models/ProductBL.cs`, `Controllers/HomeController.cs`, `Views/Home/Index.cshtml`, `Views/Shared/_Layout.cshtml`: name/category/price queries and newest/price sorting wired. Client script mismatch noted below; no behavioral test evidence. University/condition remain placeholders. |
| Add Product | Partial | `Controllers/ProductsController.cs`, `ViewModel/AddProductViewModel.cs`, `Views/Products/Add.cshtml`: validation, upload and database-save paths; uses first/default seller instead of authenticated student. Persistence/upload flows untested here. |
| Edit Product | Partial | `Controllers/ProductsController.cs`, `Views/Products/Edit.cshtml`: load/save and optional image replacement; no owner authorization. Category limitation below; round trip unverified. |
| Delete Product | Partial | `Controllers/ProductsDeleteController.cs`, `Models/ProductBL.cs`, `Views/Home/Details.cshtml`: Development-only POST, antiforgery, confirmation and soft deletion by ID. No ownership check or production student deletion workflow. |
| Authentication | Partial | `Models/ApplicationUser.cs`, `Models/ApplicationUserBL.cs`: password hashing, registration/authentication helpers. No login/register/logout controllers/views or authenticated session setup in `Program.cs`. |
| Student Profile | Partial | `Models/ApplicationUser.cs`, `Models/ApplicationUserBL.cs`: profile/update helpers; no student profile controller/view or authenticated workflow. |
| Communication | Partial | `Views/Home/Details.cshtml`, `Views/Shared/_ProductCard.cshtml`: email/phone links; `Models/ContactInquiryBL.cs`: stored inquiry helper without a controller/view. Contact-channel approval and end-to-end behavior unresolved. |
| Reports | Partial | `Models/Report.cs`, `Models/ReportBL.cs`, `Data/Configurations/ReportConfiguration.cs`: submission/handling helpers and target constraint; no report controllers/views or tested workflow. |
| Admin Features | Partial | `Models/ApplicationUserBL.cs`, `Models/ProductBL.cs`, `Models/ReportBL.cs`: account blocking, product removal and report helpers. No admin controllers/views or authenticated role workflow; monitoring not implemented as a user workflow. |

## 3. Completed Work

- Historical verification on **2026-10-30** established a successful build and Development launch-profile startup, with a Home HTTPS response of 200 displaying database products. This is prior conversational evidence, not a test of `d56781d`.
- Current source review confirms MVC/ViewModel/partial-view wiring and EF configuration/migration files. These artifacts do not establish functional completion or database readiness.

## 4. Known Issues & Blockers

- **Authorization dependency:** add assigns the first/default account; edit mutates by supplied product ID without authentication/ownership checks (`Controllers/ProductsController.cs`). Development deletion accepts any existing product ID (`Controllers/ProductsDeleteController.cs`). These paths do not meet owner-only management requirements.
- **Incomplete seller workflow:** My Products is a placeholder; `ProductBL.MarkSold` has no controller/view entry point. Authentication is needed before completing owner/profile/admin workflows.
- **Validation conflict:** `ViewModel/AddProductViewModel.cs` allows prices 1–1,000,000, while `Models/ProductBL.cs` and `Data/Configurations/ProductConfiguration.cs` allow zero. The approved rule remains unresolved.
- **Source-confirmed UI mismatches, browser impact untested:** Home category-click JavaScript reads absent `data-category` attributes and hides cards before link navigation. Edit builds category options from hardcoded college lists, so an existing category outside those lists cannot be preselected (`Views/Home/Index.cshtml`, `Views/Products/Edit.cshtml`).
- **Verification/dependency gaps:** no automated test project or checked-in test-result files found. Current build, database availability/migration state, responsive behavior and mutation flows are unverified. Development startup migrates/seeds and can terminate if initialization fails (`Program.cs`); inspect the configured environment before running it.
- Approved class specifications are not available inside the repository. Missing rules listed below block specification-dependent decisions, not all development work.

## 5. Pending Work & Next Steps

1. Confirm the minimum account/identity and validation decisions needed for authentication; obtain repository-accessible approved class documentation for class-level decisions.
2. Implement and test registration/login/logout and authenticated identity, using existing hashing helpers where appropriate.
3. Enforce authenticated ownership/admin authorization on mutations; replace default seller assignment. Complete My Products, owner deletion and mark-sold workflows.
4. Resolve price/category/image rules, repair the identified filter/edit mismatches, and verify add/edit persistence and upload failures.
5. Complete profile, approved seller contact, reporting and admin workflows after their dependent decisions are confirmed.
6. Build and test the current revision, including permission failures, validation, empty/error states, search/filter/sort combinations, and mobile/tablet layouts. Record dated results before upgrading statuses.

No task ownership is documented in the reviewed sources.

## 6. Decisions Requiring Approval

See `AGENTS.md`, **Implementation and unresolved details**: account identifier/fields and password rules; profile schema; contact channel and public seller data; category administration/taxonomy; price and image requirements; report fields/origin/handling and permitted account-management actions. Existing fields, statuses and limits are not evidence of approval.

The green Home design direction is approved; full final implementation approval is not established. Sorting and other existing implementation choices do not independently establish new approved requirements.

## 7. Verification Status

| Evidence | Result / limitation |
| --- | --- |
| Current repository review, 2026-11-06 | Source paths and workflow gaps inspected; initial Git working tree clean; no existing `STATE.md` found. No current feature acceptance test performed. |
| Historical build, 2026-10-30 | `dotnet build --no-restore` passed with 0 errors and one NU1900 vulnerability-service connectivity warning. Not rerun against the current revision. |
| Historical runtime, 2026-10-30 | Development launch profile started; Home HTTPS returned 200 with eight product cards. Does not verify current details, mutations, filtering, authorization, or responsive layouts. |
| Automated/end-to-end tests | No test suite/results found; the Development test-data seeder is not a test suite. |

This documentation-only review did not build, start the app, apply migrations, seed data, or inspect live database contents. Only `STATE.md` was created; source files and `AGENTS.md` were preserved.

## 8. Last Updated

**2026-11-06 (Africa/Cairo)** — inspected `AGENTS.md`, SRS/BRD scope, repository structure, controllers, views, ViewModels, BL/model code, mappings/migrations, startup, Git status and available historical build/runtime evidence. Reverify against the working tree before relying on this snapshot.

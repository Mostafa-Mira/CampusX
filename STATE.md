# CampusX project state

## 1. Project Overview

CampusX is a student marketplace for study-related items. The working milestone is `Milestone-3/`, an ASP.NET Core MVC application. Milestone acceptance and overall completion are not established.

Stack: .NET 8, C#, Razor HTML/CSS/JavaScript, EF Core 8.0.4, and SQL Server. Scope comes from `Milestone-0/CampusX_SRS.docx` and supporting `Milestone-0/CampusX_BRD.docx`; source priority and engineering rules are recorded in `AGENTS.md`.

## 2. Current Implementation Status

Snapshot from the current working tree on `main`. Reinspect before each task. Paths below are relative to `Milestone-3/` unless explicitly prefixed.

Statuses: **Completed** = implemented and verified; **Partial** = incomplete implementation or limited verification; **Pending** = not implemented; **Blocked** = cannot proceed due to a dependency; **Unverified** = insufficient verification evidence.

| Area | Status | Evidence and limits |
| --- | --- | --- |
| Home Page & Product Listing | Partial | `HomeController`, `Home/Index`, `_ProductCard`, and `site.css` provide listing/count, cards, empty/error markup, featured ordering, and responsive rules. Full browser regression is still needed. |
| Product Details | Partial | `HomeController` and `Home/Details` provide product lookup/404, seller/status display, and a neutral no-image state. Full browser regression is still needed. |
| Search, Filter & Sort | Partial | `ProductBL`, `HomeController`, `Home/Index`, and `_Layout` provide name/category/price queries and newest/price sorting. University/condition remain placeholders. |
| Add Product | Partial | `ProductsController`, `AddProductViewModel`, and `Products/Add` provide authenticated student ownership, antiforgery, validation, upload, and database save. A valid 2 MB PNG was verified through file save, SQL insert, and redirect; the separately reported failing upload has not been reproduced or captured. |
| Edit / Delete Product | Partial | Edit supports loading, saving, and optional image replacement; Development deletion is a POST confirmation. Edit and deletion still need authenticated owner checks. |
| Authentication | Partial | `AccountController`, login/signup ViewModels/views, and `Program.cs` implement cookie login/logout, password-hash verification, safe local return URLs, and simple approved-domain registration. University ownership remains intentionally unverified in development. |
| Revenue | Partial | `RevenueController`, `RevenueBL`, payment-provider abstraction, models/configuration, migration, and Revenue view implement simulated purchases, owner-only promotion, configurable settings, commission calculation/disclosure, and featured ordering. Real payments and verified-sale commission collection remain deferred. |
| Student Profile, Reports & Admin | Partial | Models/BL helpers exist, but authenticated end-to-end profile, report, moderation, and administration workflows remain incomplete. |

## 3. Verified Local Work

- **2026-11-10:** `dotnet build CampusX.csproj --no-restore` passed with 0 warnings and 0 errors.
- **2026-11-10:** the Revenue migration was applied to local SQLEXPRESS after a verified local database backup. An authenticated product/add-credit/promote/featured-order flow was exercised and temporary test data was removed.
- **2026-11-10:** a valid 2 MB PNG completed the real multipart Add Product flow through filesystem write, SQL insert, and redirect. Its temporary account, product, and file were removed.
- **2026-11-10:** light/dark preference initialization and persistence were syntax/behavior checked locally; full visual browser regression remains pending.

## 4. Known Issues & Blockers

- **Authorization gap:** Add uses the authenticated student identity and Revenue validates ownership. Edit and Development deletion still mutate by supplied product ID without owner checks.
- **Incomplete seller workflow:** My Products is a placeholder; mark-sold has no controller/view entry point.
- **Validation conflict:** Add/Edit ViewModel prices require 1–1,000,000, while ProductBL/database checks permit zero. The approved rule remains unresolved.
- **UI/data limitations:** Home category-click JavaScript expects absent `data-category` attributes. Edit builds category options from hardcoded college lists, so arbitrary existing categories may not preselect.
- **Upload report needs evidence:** the reported browser upload crash was not reproduced with a valid PNG, and no server exception was recorded. Preserve the failing image/request and Development stack trace if it recurs.
- No automated test project or checked-in test results were found. Responsive behavior and remaining permission/mutation flows need browser regression testing.

## 5. Pending Work & Next Steps

1. Enforce authenticated ownership/admin authorization on edit, deletion, mark-sold, and remaining product-management workflows; complete My Products.
2. Resolve price/category/image rules, repair filter/edit mismatches, and capture the reported upload failure if it recurs.
3. Complete profile, approved seller contact, reporting, and admin workflows after dependent decisions are confirmed.
4. Run browser regression for permission failures, validation, empty/error states, search/filter/sort, and mobile/tablet layouts.
5. Add a verified purchase/payment flow before collecting sales commission; real gateway integration remains deferred.

## 6. Decisions Requiring Approval

See `AGENTS.md`, **Implementation and unresolved details**: account/profile fields, password rules, public seller contact data, category taxonomy, price/image requirements, report fields/origin/handling, and permitted account-management actions.

The green Home direction is approved; final UI implementation approval is not established.

## 7. Verification Status

| Evidence | Result / limitation |
| --- | --- |
| Current build, 2026-11-10 | `dotnet build CampusX.csproj --no-restore` passed with 0 warnings and 0 errors. |
| Local Revenue workflow, 2026-11-10 | Backup completed, migration applied to local SQLEXPRESS, and authenticated purchase/promote/featured-order flow was exercised. |
| Local image upload, 2026-11-10 | Valid PNG multipart submission saved the file and product, then redirected successfully. It does not explain the separately reported failing upload. |
| Automated/end-to-end tests | No test suite/results found. Local evidence is not full acceptance testing or proof that every environment is configured identically. |

## 8. Last Updated

**2026-11-11 (Africa/Cairo)** — Updated against the current implementation: authentication, Revenue actions/UI, local migration/runtime checks, neutral no-image UI, and current verification limits.
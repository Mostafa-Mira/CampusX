# CampusX project state

**Last updated 2026-11-26 (Africa/Cairo).** Reviewed `main` at `b237ce0` with the local startup refactor. CampusX is a student marketplace in `Milestone-3/`, using ASP.NET Core MVC, .NET 8, Razor, EF Core 8.0.4, and SQL Server. Full acceptance and live deployment remain unverified.

Use [AGENTS.md](AGENTS.md) for approved scope and durable rules and the [MVC README](Milestone-3/README.md) for setup. Source paths below are relative to `Milestone-3/`.

## Implementation status

| Area | Current implementation | Verification and limits |
| --- | --- | --- |
| Registration, login, and logout | Identity password registration and sign-in, cookie sessions, antiforgery POSTs, logout, and local return URLs. Signup keeps login and university emails separate and starts university onboarding. `Controllers/AccountController.cs`, `Models/ApplicationUserBL.cs`, `ViewModel/SignUpViewModel.cs` | Locally exercised, including failed passwords, unverified onboarding, and logout. |
| University email verification | Gmail SMTP sends an eight-digit, single-use code. SQL persists cooldowns, send and attempt limits, expiry, and the confirmed-address binding. Changing the address revokes eligibility. Cairo undergraduate addresses use 14 ASCII digits at `stud.cu.edu.eg`. Ain Shams, Alexandria, and Helwan are disabled and marked Coming Soon. `Models/UniversityEmailVerificationBL.cs`, `Models/StudentUniversities.cs`, `Views/Account/PendingUniversityVerification.cshtml` | The user confirmed real inbox delivery and successful marketplace entry in an earlier local session. Shared/hosted SMTP delivery remains unverified. |
| Google sign-in and linking | Optional OAuth configuration, provider-key login, passwordless signup with university onboarding, collision handling, and explicit linking from an authenticated account with password confirmation. `Controllers/AccountController.Google.cs`, `Program.cs` | The user confirmed real local Google signup, denial before university verification, and sign-in again after logout. Existing-account linking is implemented. A hosted OAuth callback and live account-linking acceptance remain unverified. |
| Forgot/reset password | Generic queued requests, throttling, and an Identity reset link with a 30-minute protected ticket. An account needs a confirmed login email or verified university email. Where only a different university address is verified, recovery requires proof from both inboxes. Blocked accounts are excluded. `Controllers/AccountController.Recovery.cs`, `Models/PasswordRecoveryBL.cs`, `Services/PasswordRecoveryQueue.cs` | Accounts with neither recovery factor cannot self-recover. Live recovery inbox delivery and hosted HTTPS origin remain unverified. |
| Role and ownership authorization | `MarketplaceAccessEvaluator` reads current eligibility and role from SQL. `HomeController.Details` applies the ProductOwner requirement to the loaded listing before preparing view actions. The handler and `ProductBL` share `Product.IsOwnedBy`. Edit, Delete, and Mark Sold enforce persisted ownership in BL. Admin removal has a separate policy. Promotions retain their transaction-bound ownership check in `RevenueBL`. | Local checks covered stale role claims, owner/non-owner and Student/Admin access, blocked/unverified accounts, antiforgery, and concurrency. Seller hard delete and Admin soft removal are Development-only. |
| Home, details, and discovery | Home, Details, and My Products read through `ProductBL` and `IProductRepository`. `CategoryBL` reads through `ICategoryRepository`. Scoped DI binds both interfaces to the existing EF implementations. Home shares one category list between the chips and sidebar. Name search, category/price filters, and featured-first sorting run in SQL. | Product cards and Details contain images. University and condition filters are placeholders. Full visual acceptance and hosted media checks remain pending. |
| Product management | `ProductsController`, `ProductsDeleteController`, and `MyProductsController` delegate to `ProductBL`. Add/Edit save new categories and products together through the scoped repositories. My Products lists the student's own available and sold items. | Mark Sold removes a listing from Home. Seller hard delete and Admin soft removal remain Development-only. |
| Student profile, communication, and reports | Seller phone/email links are available where data exists. Profile, stored inquiry, and reporting models/BL are present. `Views/Home/Details.cshtml`, `Models/ApplicationUserBL.cs`, `Models/ContactInquiryBL.cs`, `Models/ReportBL.cs` | Profile editing, stored inquiries, and report submission/review lack complete MVC workflows. Confirm the approved contact channels and data visibility. |
| Admin Dashboard | Admin-only overview with counts and supporting account/report BL methods. `Controllers/AdminController.cs`, `Models/AdminDashboardBL.cs`, `Views/Admin/Index.cshtml`. | The account-management UI and moderation queue remain pending. Named ownership is not recorded in the repository. |
| Revenue and Payment Integration | Simulated promotion credit purchases, owner-only promotions, featured ordering, and commission calculation/disclosure. `Controllers/RevenueController.cs`, `Models/RevenueBL.cs`, `Services/SimulatedPaymentProvider.cs`, `Views/Revenue/` | Earlier local purchase/promotion rehearsal and latest local Revenue access were verified. Real Payment Integration is team-owned follow-on work and requires separate scope approval. No gateway or verified-sale commission collection. Named ownership is not recorded in the repository. |
| MonsterASP.NET shared SQL | `Program.cs` delegates Development database initialization to `Data/Development/CampusXDevelopmentStartupExtensions.cs`. The explicit shared startup flag suppresses automatic migrations and both seeders. | Startup branching verified with a test probe. Hosted application deployment, current live schema, and live integration have not been verified. |
| Development Team Tester Access | Separate Student accounts enrolled through an Identity claim and console provisioning. The Team Test profile requires private opt-in and allowlist, approved SQL target, validated network SQL TLS, and loopback HTTPS with a per-machine client certificate. `Services/DevelopmentTesterAccess.cs`, `Data/Development/DevelopmentTesterProvisioner.cs`, `Properties/launchSettings.json` | Six temporary accounts provisioned and cleaned up in isolated local SQL. Tested certificate, loopback, allowlist, role, ownership, and Production denial. Shared enrollment was not verified. Confirm existing enrollment before approved provisioning. See [Team Test setup](Milestone-3/README.md#shared-team-tester-access). |

## Open issues and deployment work

1. **Shared SQL rollout.** The last reported shared baseline was five migrations ending at `20261110065800_AddRevenueSystem`. Verify the current history before deciding whether `20261119142222_IdentityFoundation` and `20261119182114_AddUniversityEmailVerification` are still pending. The isolated SQL copy used in the tester rehearsal had all seven. The checked-in `campusx_migrations_review.sql` ends at Revenue and cannot serve as the current upgrade script. Back up and rehearse the upgrade against a suitable copy before approved deployment. Check legacy email normalization/collisions, known roles, preservation of hashes and relationships, and SQL application-lock support.
2. **Hosting prerequisites.** Verify production HTTPS and Google callback registration, Gmail STARTTLS delivery, `PasswordRecovery:PublicOrigin`, and protected, persistent Data Protection keys. Password recovery throttling is per process. Confirm that uploads are persisted and served through authorized requests. External or separately hosted images are outside MVC authorization. Shared-test writes affect shared records, and direct SQL access is governed by SQL permissions. The legacy local details seeder has a fixed fixture password. Use the separate Identity provisioner for approved testers.
3. **Product rules and bugs.** Add/Edit accepts prices from 1 to 1,000,000 EGP, while the BL and database allow zero. Edit uses a fixed category list, so existing categories outside that list can fail to preselect. Add/Edit can create category records from submitted names. Agree on price, taxonomy, and upload requirements. An earlier reported upload failure was not reproduced with a valid 2 MB PNG. Ordinary concurrent edits can overwrite the same field. Ownership and deletion state are concurrency-checked. Category names lack a unique constraint, so concurrent first use of one name can create duplicate categories.
4. **Incomplete features and decisions.** Complete production listing control, profile editing, approved communication/reporting, and Admin workflows. Identity still uses a legacy-compatible one-character minimum for normal signup. Agree on the password policy, recovery for legacy accounts with neither approved factor, seller data visibility, report handling, and named team ownership. The green UI direction is approved. Full final UI approval and marketplace acceptance remain open.

## Verification record

- **2026-11-26, startup refactor.** Build passed with 0 warnings and 0 errors. Before-and-after Edge tests on isolated local SQL matched for Student/Admin policies, owner-only CRUD, public CSS, protected product images, and Production HSTS. Post-refactor tests passed for mocked Google and email, recovery throttling, and certificate-protected Team Tester access. Startup probes replaced migration and seeding operations with markers. Local Development selected all three in order. Shared mode and Production skipped them.
- **2026-11-23, IAM and resource authorization:** The project built with 0 warnings and 0 errors. Baseline and final Edge checks on isolated local SQL matched for owner and non-owner Details actions, Edit, Delete, Mark Sold, Admin removal, and promotions. A concurrent seller change or moderation caused rollback, while Edit preserved a concurrent Sold status. Production policy, login/logout, mocked Google and university email flows, certificate-protected Team Tester access, responsive layouts, both themes, and complete product images passed. No migrations or shared database connections were used.
- **2026-11-23, repository interfaces:** Builds before and after alignment passed with 0 warnings and 0 errors. Isolated local SQL and Edge tests matched the baseline for marketplace reads, sorting and promotions, category creation, CRUD, ownership, concurrency, and rollback. Home still used one category query and one catalog query. Identity tests covered passwords and blocked accounts with mocked Google OAuth and email delivery. Team Tester startup and certificate-protected sign-in passed against the local catalog. Browser checks covered 375, 768, and 1360 px, light and dark themes, and full product images.
- **2026-11-22, repository verification:** `dotnet build Milestone-3/CampusX.csproj --no-restore` passed with 0 warnings and 0 errors. Edge tests on isolated local SQL passed catalog reads, filters, sorting, promotions, ownership, Add/Edit/Delete/Mark Sold, concurrent edits, and category rollback on an edit conflict. Home ran one category query and one catalog query. Marketplace layouts were checked at 375, 768, and 1360 px in light and dark modes. Add/Edit label associations were checked in the browser. Tests exercised Identity password sign-in, university verification with a mock mailbox, and sign-in through a mock Google OAuth provider. No migrations or shared database connections were used.
- **2026-11-20, documentation sync:** `dotnet build Milestone-3/CampusX.csproj --no-restore` passed with 0 warnings and 0 errors. Reviewed tracked sources, migration history in the repository, and documentation. This check used source inspection and a build.
- **2026-11-20, earlier local sessions:** The user confirmed real Gmail-to-university inbox delivery and real Google OAuth signup/sign-in. Local Edge and isolated SQL testing exercised Team Test provisioning and marketplace protections, including the app running under Production settings. A redacted configuration check confirmed the Team Test startup guard after updating remote SQL TLS settings, without contacting MonsterASP.NET.
- **Earlier local checks:** Simulated credits and promotion ordering, registration, and a valid 2 MB image upload were exercised. The repository contains no checked-in automated test suite. Local test evidence does not establish deployment or acceptance.
### My Products (added 2026-11-20, updated 2026-11-21)

**Status: Completed locally.** Built and manually verified in the browser against the shared database by the author.

**What it does**

- Adds a page at `/products/mine` listing the signed-in student's own products, both Available and Sold.
- Shows a header with counts ("N available · N sold"), then product cards using the existing `_ProductCard` partial with a status badge above each card.
- Cards have no separate Edit link. Editing, deleting, and marking as sold are done from the product's Details page ("View details").
- Empty state: a message and an "Add Product" button when the student has no listings.
- The sidebar "My Products" item in `_Layout` changed from a disabled "Soon" placeholder to a real link, highlighted when active.

**Files added**

| File | Purpose |
| --- | --- |
| `Controllers/MyProductsController.cs` | `GET /products/mine`. `[Authorize(Policy = MarketplaceAccessPolicies.Student)]`. Reads the student id from `ClaimTypes.NameIdentifier` only, never from the request. Calls `ProductBL.GetBySeller` and maps to `ProductCardViewModel`. |
| `ViewModel/MyProductsViewModel.cs` | `MyProductsViewModel` (item list, `AvailableCount`, `SoldCount`) and `MyProductItemViewModel` (card + `ProductStatus`). |
| `Views/MyProducts/Index.cshtml` | Page markup: heading and counts, `TempData` success messages, product grid, empty state. |

**Existing files changed (minimal)**

| File | Change |
| --- | --- |
| `Views/Shared/_Layout.cshtml` | Added `isMyProducts` flag; replaced the disabled My Products list item with a link to `MyProducts/Index`. |
| `Models/ProductBL.cs` | `GetBySeller` now includes `Category` so the category name appears on cards. |
| `wwwroot/css/campus-ui.css` | Appended `.my-product-item` rules at the end of the file. No existing rules changed. |

**Design decisions**

- Separate controller, so `ProductsController` (Add/Edit) was not modified and merge conflicts are avoided.

  ### Mark Product as Sold (added 2026-11-21)

**Status: Completed locally.** Built and manually verified in the browser against the shared database by the author.

**What it does**

- On a product's Details page, the seller sees a **Mark as Sold** button next to **Edit Product**. The button is shown only when the signed-in user is the product's seller, passes the Student policy, and the product status is `Available`.
- Clicking it shows a browser confirmation. On confirm, the product status changes to `Sold` and the user returns to the same Details page with the message "Product marked as sold."
- A sold product disappears from the public Home listing (Home already filters to `Available`) and remains visible to its seller in My Products with a "Sold" badge and updated counts.
- There is no UI to revert a product to Available.

**Files changed**

| File | Change |
| --- | --- |
| `Controllers/MyProductsController.cs` | Added `POST /products/mine/{id}/sold` (`MarkSold`). Student policy, `[ValidateAntiForgeryToken]`, seller id from the auth claim. Calls the existing `ProductBL.MarkSold`, which enforces ownership. `UnauthorizedAccessException` returns Forbid; `InvalidOperationException` and `DbUpdateConcurrencyException` return NotFound. Redirects to `Home/Details`. Added `using Microsoft.EntityFrameworkCore;`. |
| `Views/Home/Details.cshtml` | Added `@using CampusX.Enums` and a small form with a **Mark as Sold** button inside `product-details-actions`, after Edit Product and before Delete. Reuses existing button classes; no new CSS. |

**Design decisions**

- The action lives in `MyProductsController` (the author's own file) so `ProductsController` and `ProductsDeleteController` were not modified. The only shared file touched is `Details.cshtml`, with one additive block.
- No changes to `ProductBL`, models, migrations, or the database schema. `ProductBL.MarkSold` and `Product.ChangeStatus` already existed and are reused.
- Server-side ownership check: another student cannot mark someone else's product as sold even by posting directly.
- Unlike the delete flow (Development-only), Mark as Sold is available in all environments.

**Known notes**

- Visual Studio may show TS2683 ("'this' implicitly has type 'any'") on the image `onerror` handler in `Details.cshtml`. This is an IntelliSense warning from the native TypeScript compiler on existing teammate code, not a build error, and it was not changed.
- Marking as sold is permanent from the UI. A "Mark as Available" action would need a new `ProductBL` method and endpoint.
- Edit is still allowed on sold products (existing behavior, not changed).

**Verification done**

1. Mark as Sold button appears on the seller's own available product Details page.
2. After confirming, status changes to Sold and the success message appears.
3. Product disappears from Home and remains in My Products with updated counts.

**Still worth checking**

- Another student's product Details page does not show the button.
- A direct POST to `/products/mine/{id}/sold` for another student's product is rejected (Forbid).
- Signed-out or non-student access is denied.
- Uses the existing `ProductBL` instead of direct `DbContext` access in the controller, following `AGENTS.md`.
- `_ProductCard`, Home, Revenue, Account, models, migrations, `DbContext`, and `Program.cs` were not changed. No schema or migration changes.
- Authorization is enforced server-side; hiding UI is a convenience only.

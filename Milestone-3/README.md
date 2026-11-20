# CampusX MVC database milestone

This project is an ASP.NET Core MVC application targeting .NET 8, with EF Core and SQL Server. Its structure follows the instructor's MVC example while retaining CampusX-specific entities.

- `Models/`: the five database entities and matching `*BL` classes for business operations, like the instructor example.
- `Data/DbContexts/`: the EF Core context.
- `Data/Configurations/`: one mapping class per entity.
- `Data/Migrations/`: the original migration and a follow-up schema correction.
- `Controllers/` and `Views/`: only a minimal Home scaffold to establish the MVC folders and route; CampusX feature pages are intentionally not built.

## Naming

Use singular PascalCase for entity classes (`Product`), plural DbSet/table names (`Products`), `EntityNameConfiguration` for mapping classes, and `EntityNameId` for foreign keys. Namespace and folder names match. The product field is `Name`, matching the SRS.

## Database setup

### Identity foundation

`ApplicationUser` now inherits from `IdentityUser<Guid>` and remains mapped to `ApplicationUsers`. `CampusXDbContext` uses Identity's user-only store: the existing `UserRole` enum remains the single source of Student/Admin roles, supplied through the CampusX claims factory. No parallel role membership tables or new roles are introduced. Existing MVC account pages/routes are retained; their backend uses `UserManager` and `SignInManager`. Existing cookies require a fresh login after the authentication-scheme cutover.

`Email`/`EmailConfirmed` describe the personal login email only. `UniversityEmail` and `UniversityEmailConfirmed` are separate; old accounts receive no university address and remain unverified. Google login is not implemented. Password acceptance is kept compatible with the prior implementation pending an approved password policy.

The new `20261119142222_IdentityFoundation` migration adds Identity metadata and claim/login/token tables, initializes usernames from the existing GUIDs, backfills normalized login metadata and stamps, and permits a nullable local password hash for future external-login accounts. It preserves existing GUIDs, emails, hashes, enum roles and business foreign keys. Automatic normalization stops for blank/non-ASCII emails, normalized-email collisions, or unknown enum roles; such data needs review instead of being silently changed. Rollback stops if new Identity/university data or passwordless users would be lost.

Before running this version against an existing database, review and apply its schema migration through a coordinated deployment. Do not run the new model against the old schema. Local-copy rehearsal passed; neither the original local database nor the shared MonsterASP.NET database was migrated by this task. Keep shared startup mode enabled and arrange a backup, data preflight and reviewed migration application separately before deploying this version to shared environments. Do not mark old users university-verified to bypass onboarding.

### Marketplace access

The default and fallback authorization policies require an authenticated, existing, unblocked account with a Cairo undergraduate email (exactly 14 ASCII digits at `stud.cu.edu.eg`), `UniversityEmailConfirmed=true`, and a matching `VerifiedUniversityEmail` binding written only after code verification. Eligibility is read from the database on each protected request; personal email confirmation, Google claims and the Admin role do not bypass it. Guests are challenged to Login, unverified accounts go to `/Account/PendingUniversityVerification`, and blocked or missing accounts receive HTTP 403. Do not manually mark accounts verified.

### University email verification

Cairo undergraduate verification is enabled. Ain Shams, Alexandria and Helwan remain Coming Soon. Registration still uses Identity and keeps login and university emails separate. The pending page supports sending/resending an eight-digit code, verification and changing the university email. Verified users can manage the address through the University Email navigation entry. Changing it clears verification immediately; one university email can be verified on only one account at a time.

Codes expire after 10 minutes. SQL-backed reservations enforce a 60-second cooldown and five sends/hour per account and recipient, including failed deliveries. Failed checks are limited to five per 15 minutes across resends, email changes and application restarts. Codes use Identity's salted password hasher plus Data Protection and are consumed once. A database-owned application lock and unique indexes serialize races; SMTP runs outside the lock. Keep the hosting Data Protection key ring persistent and protected across deployments. Do not log codes, message bodies or SMTP secrets.

Configure `EmailSmtp:Host` (`smtp.gmail.com`), `EmailSmtp:Port` (`587`), `EmailSmtp:UserName`, `EmailSmtp:AppPassword`, and `EmailSmtp:FromEmail` in User Secrets for local Development or private environment configuration on the host. Environment names use double underscores, such as `EmailSmtp__AppPassword`. Use a Gmail app password with 2-Step Verification, and an authorized sender address. STARTTLS is required; there is no plaintext fallback. Missing configuration or delivery failure leaves the account unverified. Gmail acceptance and actual inbox delivery require separate live verification; no live delivery is claimed by the local tests.

The additive `20261119182114_AddUniversityEmailVerification` migration adds the nullable confirmed-address binding, its filtered unique index, and verification/failed-attempt tables. Existing user IDs, password hashes, roles and business relationships are retained; old accounts receive no verified binding. Review and coordinate migration deployment separately before using this version against the shared database. SQL application-lock support must also be available on the deployment database. Shared startup mode must remain enabled; this work does not migrate the shared database.

Login, signup, logout, verification onboarding and safe error pages have explicit exceptions to marketplace eligibility. Their separate layout does not query categories or expose marketplace navigation/data. CSS and JavaScript remain public; other local static files, including product uploads, pass authorization and receive private/no-store caching. Hosting must route product media through this application; external image URLs, independently served uploads and previously downloaded copies are outside this protection. Existing product owner/admin permission gaps still require separate remediation; marketplace eligibility does not establish product ownership.

The connection string is read from `ConnectionStrings:CampusXDb`. For local Development, put your SQL Server login in `appsettings.Development.json` (kept out of source control) or set the `ConnectionStrings__CampusXDb` environment variable. The local SQL Server should be reachable at `localhost:1433`.

In the `Development` launch profile, when `DatabaseStartup:UseSharedDatabase` is absent or `false`, the app applies pending EF Core migrations at startup and then runs the existing development seeders. You can also apply migrations manually with `dotnet ef database update`. The correction migration deliberately stops if existing accounts are present: the old schema did not store password hashes, so those accounts cannot be migrated into usable logins automatically. It also stops for invalid legacy product statuses/prices, ambiguous reports, or contact records whose seller disagrees with the product. Back up and resolve such data before applying the correction; do not drop the database to bypass the checks.

### Shared team database startup

Before launching against the shared MonsterASP.NET database, set `DatabaseStartup:UseSharedDatabase` to `true` in your local configuration or user secrets:

```json
{
  "DatabaseStartup": {
    "UseSharedDatabase": true
  }
}
```

The equivalent environment variable is `DatabaseStartup__UseSharedDatabase=true`. Configure `ConnectionStrings:CampusXDb` separately through your existing private configuration; do not commit credentials. This flag is explicit, not inferred from the server address. With shared mode enabled, startup skips automatic migrations, `SampleProductSeeder`, and `DevelopmentTestDataSeeder`, including in Development. It does not disable normal application database access or manual EF commands. Coordinate shared schema updates separately. Non-Development startup remains unchanged; use an absent/false flag only for the existing local Development initialization behavior.

When the app runs with the `CampusX` launch profile (`Development`) and shared mode is disabled, it inserts five sample products once. Every product card shows a **Delete product** button, including products created through the add form. The development-only `POST /dev/products/{id}/delete` endpoint soft-deletes by ID so the product is hidden from the catalog while its database record is retained.

The BL classes establish the instructor-style business-logic layer, but CampusX feature controllers and views are intentionally not built yet. The Home view is only a starter placeholder, not the approved visual design. The controllers call BL classes, and the BL classes use the injected DbContext; unlike the instructor's demo, the context is not instantiated with `new` in each BL class. The SRS does not specify profile fields or the exact contact mechanism; the current profile fields and stored contact inquiry are retained from the pulled code pending team confirmation.

The SRS does not define who creates categories or a fixed category list. The database currently requires an existing category before a product can be added, so the team must decide how categories are supplied before completing the add-product screen.

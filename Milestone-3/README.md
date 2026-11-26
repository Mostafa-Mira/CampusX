# CampusX MVC

This project is an ASP.NET Core MVC application targeting .NET 8, with EF Core and SQL Server. Its structure follows the instructor's MVC example while retaining CampusX-specific entities.

- `Models/`: application entities and `*BL` classes for business operations.
- `Data/Repositories/`: scoped EF Core data access for products and categories.
- `Data/DbContexts/`: the scoped EF Core context.
- `Data/Configurations/`: one mapping class per entity.
- `Data/Migrations/`: the baseline and subsequent product, revenue, Identity, and verification migrations.
- `Controllers/` and `Views/`: the MVC account, marketplace, listing, and simulated revenue workflows. See [STATE.md](../STATE.md) for verification, incomplete features, and deployment status.

## Naming

Use singular PascalCase for entity classes (`Product`), plural DbSet/table names (`Products`), `EntityNameConfiguration` for mapping classes, and `EntityNameId` for foreign keys. Namespace and folder names match. The product field is `Name`, matching the SRS.

## Database setup

### Application startup

`Program.cs` shows MVC and DbContext registration, the tester provisioning exit, the request pipeline, and the default route. `CampusXServiceCollectionExtensions.cs` groups repositories, business services, email, payment registration, and recovery rate limits. `CampusXDevelopmentStartupExtensions.cs` handles Team Tester hosting and Development database initialization. The Identity and authorization registration extensions remain the entry points for their own configuration.

### Identity foundation

`ApplicationUser` now inherits from `IdentityUser<Guid>` and remains mapped to `ApplicationUsers`. `CampusXDbContext` uses Identity's user-only store: the existing `UserRole` enum remains the single source of Student/Admin roles, supplied through the CampusX claims factory. No parallel role membership tables or new roles are introduced. Existing MVC account pages/routes are retained; their backend uses `UserManager` and `SignInManager`. Existing cookies require a fresh login after the authentication-scheme cutover.

`Email` and `EmailConfirmed` describe the login email. University eligibility uses the separate `UniversityEmail`, `UniversityEmailConfirmed`, and `VerifiedUniversityEmail` fields. The migration leaves existing accounts university-unverified. Password acceptance remains compatible with the prior implementation pending an approved password policy.

The new `20261119142222_IdentityFoundation` migration adds Identity metadata and claim/login/token tables, initializes usernames from the existing GUIDs, backfills normalized login metadata and stamps, and permits a nullable local password hash for future external-login accounts. It preserves existing GUIDs, emails, hashes, enum roles and business foreign keys. Automatic normalization stops for blank/non-ASCII emails, normalized-email collisions, or unknown enum roles; such data needs review instead of being silently changed. Rollback stops if new Identity/university data or passwordless users would be lost.

Before running this version against an existing database, confirm its migration history. Keep shared startup mode enabled and coordinate a backup, data preflight, and reviewed migration application separately. Keep old accounts university-unverified until they complete onboarding. The checked-in `campusx_migrations_review.sql` ends at `AddRevenueSystem` and does not cover the Identity and verification upgrades.

### Google sign-in and account recovery

Set `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` in private configuration to enable Google OAuth. The callback route is `/signin-google`. The local HTTPS callback is `https://localhost:7148/signin-google`. Google signup still requires university verification. Linking to an existing account requires an authenticated session, explicit confirmation, and the existing password. Matching email alone never links accounts.

Forgot Password uses Gmail SMTP and requires `PasswordRecovery:PublicOrigin` to be an approved HTTPS origin without a path, query, or fragment. Recovery requires a confirmed login email or an already verified university email. If only a different university address is verified, the user needs access to both inboxes. Legacy accounts missing both recovery factors need an approved ownership-verification path before recovery can proceed. Keep Data Protection keys persistent and protected on the host.

### Marketplace access

The default and fallback authorization policies require an authenticated, existing, unblocked account with a Cairo undergraduate email (exactly 14 ASCII digits at `stud.cu.edu.eg`), `UniversityEmailConfirmed=true`, and a matching `VerifiedUniversityEmail` binding written only after code verification. The server checks eligibility and the current role. Personal email confirmation, Google claims, and the Admin role do not confer university eligibility. Guests are challenged to Login, unverified accounts go to `/Account/PendingUniversityVerification`, and blocked or missing accounts receive HTTP 403. The [Development tester setup](#shared-team-tester-access) provides the approved local exception for enrolled Students.

### Identity and authorization lifecycle

1. **Identity and authentication.** `Program.cs` calls `AddCampusXIdentity` to register the Identity EF store, managers, cookies, and optional Google OAuth. `AccountController.SignUp` calls `ApplicationUserBL.RegisterAsync`, which creates the account through `UserManager`. `Login` calls `ApplicationUserBL.AuthenticateAsync` for the password check. `SignInManager` issues and clears the session cookie.
2. **Roles and claims.** `CampusXUserClaimsPrincipalFactory` issues the user GUID and the Student or Admin role claim. The stored `ApplicationUser.Role` is the current role used in authorization decisions.
3. **Policies.** `AddCampusXAuthorization` registers Marketplace, Student, Admin, and AuthenticatedOnly access. `MarketplaceAccessHandler` uses `MarketplaceAccessEvaluator` to check current role, account status, university verification, and the Development tester exception. `MarketplaceAuthorizationResultHandler` handles redirect and denial outcomes.
4. **Product resources.** `HomeController.Details` loads the listing through `ProductBL.GetById` and calls `IAuthorizationService` with that product. `ProductOwnershipHandler` uses `Product.IsOwnedBy` for the ProductOwner requirement. The Student and Admin policies determine which actions the view displays. Edit, Delete, and Mark Sold also enforce ownership inside `ProductBL` before saving. Admin removal has its own permission. EF checks seller and deletion state when saving. `RevenueBL.ActivatePromotion` checks ownership within its existing transaction.

### University email verification

Cairo undergraduate verification is enabled. Ain Shams, Alexandria and Helwan remain Coming Soon. Registration still uses Identity and keeps login and university emails separate. The pending page supports sending/resending an eight-digit code, verification and changing the university email. Verified users can manage the address through the University Email navigation entry. Changing it clears verification immediately; one university email can be verified on only one account at a time.

Codes expire after 10 minutes. SQL-backed reservations enforce a 60-second cooldown and five sends/hour per account and recipient, including failed deliveries. Failed checks are limited to five per 15 minutes across resends, email changes and application restarts. Codes use Identity's salted password hasher plus Data Protection and are consumed once. A database-owned application lock and unique indexes serialize races; SMTP runs outside the lock. Keep the hosting Data Protection key ring persistent and protected across deployments. Do not log codes, message bodies or SMTP secrets.

Configure `EmailSmtp:Host` (`smtp.gmail.com`), `EmailSmtp:Port` (`587`), `EmailSmtp:UserName`, `EmailSmtp:AppPassword`, and `EmailSmtp:FromEmail` in User Secrets for local Development or private environment configuration on the host. Environment names use double underscores, such as `EmailSmtp__AppPassword`. Use a Gmail app password with 2-Step Verification and an authorized sender address. STARTTLS is required. Missing configuration or delivery failure leaves the account unverified. The user confirmed mailbox delivery in earlier local university verification tests. Verify delivery separately for the hosted environment.

The additive `20261119182114_AddUniversityEmailVerification` migration adds the nullable confirmed-address binding, its filtered unique index, and verification/failed-attempt tables. Existing user IDs, password hashes, roles and business relationships are retained; old accounts receive no verified binding. Review and coordinate migration deployment separately before using this version against the shared database. SQL application-lock support must also be available on the deployment database. Shared startup mode must remain enabled; this work does not migrate the shared database.

Authentication, recovery, verification onboarding, and safe error pages have explicit access rules. Their separate layout avoids marketplace data queries. CSS and JavaScript remain public. Other local static files, including product uploads, pass authorization and receive private/no-store caching. Hosting must route product media through the application to apply that protection. External image URLs, independently served uploads, and previously downloaded copies remain outside it. Add uses the authenticated Student as the seller. Edit, seller delete, and promotions check persisted ownership. Admin removal has a separate role policy.

Configure `ConnectionStrings:CampusXDb` through User Secrets for local Development or private environment settings. The equivalent environment key is `ConnectionStrings__CampusXDb`. Environment settings override User Secrets, and the launch profile supplies environment settings for keys it defines. Match the effective SQL endpoint to your own local instance or the approved shared target.

The `CampusX` profile uses Development. When `DatabaseStartup:UseSharedDatabase` is absent or `false`, startup applies pending migrations and runs the sample product seeder. The separate details fixture seeder checks for the expected local database and an up-to-date schema. Use automatic initialization only with local SQL. The early correction migration deliberately stops for legacy accounts without stored password hashes, invalid product statuses/prices, ambiguous reports, or contact records with inconsistent sellers. Review and resolve any such data before applying it.

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

### Shared team tester access

Use the `CampusX Team Test` profile in Visual Studio for approved shared testers. This profile enables shared startup mode, listens on loopback HTTPS, and requires a client certificate. Keep this profile off hosted and Production deployments. It grants marketplace access only to enrolled, allowlisted Student accounts. It does not confirm university email or grant Admin privileges.

1. Open **Manage User Secrets** for the MVC project. Configure the approved `ConnectionStrings:CampusXDb`, set `DevelopmentTesterAccess:AllowSharedDatabase` to `true`, and set `DevelopmentTesterAccess:SharedServer` and `DevelopmentTesterAccess:SharedDatabase` to the SQL Server and catalog values in the approved connection. Network SQL requires encryption and server certificate validation.
2. Put your tester login email in `DevelopmentTesterAccess:AllowedEmails:0`. Each machine needs its own matching allowlist entry. Create a non-exportable Client Authentication certificate in the Windows Current User personal store. Store its SHA-256 fingerprint in `DevelopmentTesterAccess:ClientCertificateSha256`. Trust the ASP.NET Core HTTPS development certificate locally with `dotnet dev-certs https --trust` if needed.
3. Once shared provisioning is approved, the maintainer adds the approved emails to the private allowlist, enables `DevelopmentTesterAccess__AllowSharedProvisioning` only for the provisioning session, and runs the command below once per tester from the repository root. The command confirms the target database, checks the schema, prompts for a password without echoing it, and refuses existing emails. Remove the provisioning flag when finished.

   ```powershell
   dotnet run --project Milestone-3/CampusX.csproj --launch-profile "CampusX Team Test" -- --provision-shared-tester
   ```

4. Select `CampusX Team Test`, open `https://localhost:7148/Account/Login`, and select the client certificate if prompted. Use the normal Login form. All writes from this session go to the configured shared database.

For isolated local SQL testing, the separate `--provision-tester` command and `DevelopmentTesterAccess:Enabled` flag support the existing local tester setup. That mode also requires loopback, an explicitly local SQL transport, a private allowlist, and Identity enrollment.

## Listings and admin operations

### Product CRUD lifecycle

`Razor view → MVC controller → BL → repository interface → repository implementation → EF Core DbContext → SQL Server`

1. **Read.** Home, Details, and My Products call `ProductBL`. It defines search, filters, visibility, and featured ordering. BL uses `IProductRepository` for product data and `ICategoryRepository` for category data. `CategoryBL` also uses `ICategoryRepository`. DI binds each interface to its existing scoped EF implementation. Home passes its loaded categories to both the chips and the sidebar.
2. **Add and edit.** `ProductsController` handles form validation, uploads, and redirects. `ProductBL` uses the signed-in student ID, checks ownership on edit, and resolves the submitted category. Both repositories share the request's DbContext, so any new category and the product are saved together.
3. **Delete and mark sold.** `ProductsDeleteController` and `MyProductsController` pass the signed-in actor to `ProductBL`. BL checks eligibility and ownership or Admin permissions. `ProductRepository` saves the change. Sold listings leave Home and remain in the seller's My Products.

Development listing controls appear on Product Details. `POST /dev/products/{id}/delete` hard-deletes an eligible Student's own listing when its relationships permit it. `POST /dev/products/{id}/remove` soft-deletes for eligible Admins. Listing mutations require antiforgery checks. Add/Edit keep their fixed category choices. See [STATE.md](../STATE.md) for known limitations and remaining work.

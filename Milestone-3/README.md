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

The connection string is read from `ConnectionStrings:CampusXDb` in `appsettings.json`. The current local configuration expects SQL Server at `localhost:1433` with SQL authentication. Start the local SQL Server container from this directory with `docker compose up -d sqlserver`. It stores data in the `campusx_sql_data` volume, so restarting the container does not remove the database. Set `CAMPUSX_DB_PASSWORD` before starting the container if you change the password; keep it in sync with the connection string or override `ConnectionStrings__CampusXDb`.

After SQL Server is ready, apply the EF Core migrations from this directory with `dotnet ef database update`. In Visual Studio Package Manager Console, select the CampusX project and run `Update-Database` instead. The correction migration deliberately stops if existing accounts are present: the old schema did not store password hashes, so those accounts cannot be migrated into usable logins automatically. It also stops for invalid legacy product statuses/prices, ambiguous reports, or contact records whose seller disagrees with the product. Back up and resolve such data before applying the correction; do not drop the database to bypass the checks.

When the app runs with the `CampusX` launch profile (`Development`) and all migrations are applied, it inserts five sample products once. Each sample card shows its database ID and a **Delete sample** button. The development-only `POST /dev/sample-products/{id}/delete` endpoint hard-deletes only those seeded IDs; products referenced by reports or contact inquiries are protected. Deleting a sample is remembered, so restarting the app does not add it again.

The BL classes establish the instructor-style business-logic layer, but CampusX feature controllers and views are intentionally not built yet. The Home view is only a starter placeholder, not the approved visual design. The controllers call BL classes, and the BL classes use the injected DbContext; unlike the instructor's demo, the context is not instantiated with `new` in each BL class. The SRS does not specify profile fields or the exact contact mechanism; the current profile fields and stored contact inquiry are retained from the pulled code pending team confirmation.

The SRS does not define who creates categories or a fixed category list. The database currently requires an existing category before a product can be added, so the team must decide how categories are supplied before completing the add-product screen.

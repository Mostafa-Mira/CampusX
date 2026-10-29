using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Data.Development;

public static class DevelopmentTestDataSeeder
{
    private const string TestCategoryName = "Development Test Items";
    private const string TestSellerEmail = "campusx.details.test@local.invalid";
    private const string TestProductName = "CampusX Details Page Test Book";

    public static void TrySeed(CampusXDbContext context, IConfiguration configuration,
        IHostEnvironment environment, ILogger logger)
    {
        if (!environment.IsDevelopment())
            return;

        var connectionString = configuration.GetConnectionString("CampusXDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Development test data was not seeded because CampusXDb is not configured.");
            return;
        }

        var connectionBuilder = new SqlConnectionStringBuilder(connectionString);
        var server = connectionBuilder.DataSource.Trim();
        var isLocalServer = server.Equals(".", StringComparison.OrdinalIgnoreCase)
            || server.Equals("(local)", StringComparison.OrdinalIgnoreCase)
            || server.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || server.Equals("localhost,1433", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1,1433", StringComparison.OrdinalIgnoreCase);

        if (!isLocalServer ||
            !string.Equals(connectionBuilder.InitialCatalog, "CampusXDb", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Development test data was not seeded because CampusXDb is not the configured local CampusXDb database.");
            return;
        }

        try
        {
            if (!context.Database.CanConnect())
            {
                logger.LogWarning("Development test data was not seeded because the local CampusXDb database is unreachable.");
                return;
            }

            if (context.Database.GetPendingMigrations().Any())
            {
                logger.LogWarning("Development test data was not seeded because CampusXDb has pending migrations.");
                return;
            }

            var category = context.Categories.SingleOrDefault(category => category.Name == TestCategoryName);
            if (category is null)
            {
                category = new Category
                {
                    Name = TestCategoryName,
                    Description = "Local development data for the Product Details page."
                };
                context.Categories.Add(category);
            }

            var seller = context.ApplicationUsers.SingleOrDefault(user => user.Email == TestSellerEmail);
            if (seller is null)
            {
                seller = new ApplicationUser
                {
                    FullName = "CampusX Test Seller",
                    Email = TestSellerEmail,
                    PhoneNumber = "01000000000",
                    CollegeName = "CampusX Development",
                    Role = UserRole.Student,
                    IsBlocked = false
                };
                seller.SetPassword("DevelopmentOnly123!");
                context.ApplicationUsers.Add(seller);
            }
            else if (seller.Role != UserRole.Student || seller.IsBlocked)
            {
                logger.LogWarning("Development test data was not seeded because the test seller account is not an active student.");
                return;
            }

            var product = context.Products.SingleOrDefault(product =>
                product.Name == TestProductName && product.SellerId == seller.Id);
            if (product is null)
            {
                context.Products.Add(new Product
                {
                    Name = TestProductName,
                    Description = "Temporary local data for verifying the CampusX Product Details page.",
                    Price = 125.00m,
                    ImageUrl = string.Empty,
                    Category = category,
                    Seller = seller,
                    Status = ProductStatus.Available,
                    IsDeleted = false
                });
            }
            else
            {
                product.Category = category;
                product.Status = ProductStatus.Available;
                product.IsDeleted = false;
            }

            context.SaveChanges();
            logger.LogInformation("Local development product-details test data is available.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Development test data was not seeded.");
        }
    }
}

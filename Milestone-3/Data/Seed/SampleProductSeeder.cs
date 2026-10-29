using System.Security.Cryptography;
using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.Models;

namespace CampusX.Data.Seed;

public static class SampleProductSeeder
{
    private static readonly Guid SampleSellerId = Guid.Parse("00000000-0000-4000-8000-00000000c001");
    private const string SampleSellerEmail = "sample-seller@campusx.local";

    private sealed record ProductSeed(Guid Id, string Name, string Description, decimal Price, string Category);

    private static readonly ProductSeed[] Products =
    [
        new(Guid.Parse("b630560a-d579-4b72-9553-16f9e7bc4001"), "Scientific Calculator", "Lightly used scientific calculator, ready for exams.", 850m, "Electronics"),
        new(Guid.Parse("b630560a-d579-4b72-9553-16f9e7bc4002"), "Data Structures Textbook", "A clean copy with a few highlighted examples.", 420m, "Books"),
        new(Guid.Parse("b630560a-d579-4b72-9553-16f9e7bc4003"), "Desk Lamp", "Adjustable LED desk lamp for late-night study sessions.", 600m, "Study Supplies"),
        new(Guid.Parse("b630560a-d579-4b72-9553-16f9e7bc4004"), "Mechanical Keyboard", "Compact mechanical keyboard in good condition.", 1250m, "Electronics"),
        new(Guid.Parse("b630560a-d579-4b72-9553-16f9e7bc4005"), "Campus Backpack", "Roomy backpack with a padded laptop compartment.", 950m, "Accessories")
    ];

    public static IReadOnlyCollection<Guid> ProductIds => Products.Select(product => product.Id).ToArray();

    public static bool IsSampleProduct(Guid id) => Products.Any(product => product.Id == id);

    public static void Seed(CampusXDbContext context)
    {
        // The demo seller is also a one-time seed marker. Deleted sample products stay deleted.
        if (context.ApplicationUsers.Any(user => user.Id == SampleSellerId))
            return;

        var seller = new ApplicationUser
        {
            Id = SampleSellerId,
            FullName = "CampusX Demo Seller",
            Email = SampleSellerEmail,
            CollegeName = "CampusX",
            Role = UserRole.Student
        };
        seller.SetPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

        var categories = context.Categories.ToList()
            .ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var name in Products.Select(product => product.Category).Distinct())
        {
            if (!categories.ContainsKey(name))
            {
                var category = new Category { Name = name, Description = "CampusX sample listing category." };
                categories.Add(name, category);
                context.Categories.Add(category);
            }
        }

        context.ApplicationUsers.Add(seller);
        for (var index = 0; index < Products.Length; index++)
        {
            var seed = Products[index];
            context.Products.Add(new Product
            {
                Id = seed.Id,
                Name = seed.Name,
                Description = seed.Description,
                Price = seed.Price,
                ImageUrl = string.Empty,
                SellerId = seller.Id,
                CategoryId = categories[seed.Category].Id,
                Status = ProductStatus.Available,
                CreatedAt = DateTime.UtcNow.AddDays(-index)
            });
        }

        context.SaveChanges();
    }
}

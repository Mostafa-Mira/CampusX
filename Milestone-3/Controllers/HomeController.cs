
using CampusX.Models;
using CampusX.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

public class HomeController : Controller
{
    private readonly ProductBL _productBL;
    private readonly IWebHostEnvironment _environment;
    private readonly CategoryBL _categoryBL;

    public HomeController(ProductBL productBL, IWebHostEnvironment environment, CategoryBL categoryBL)
    {
        _productBL = productBL;
        _environment = environment;
        _categoryBL = categoryBL;
    }

    public IActionResult Index(string? searchTerm, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice, string? sortBy)
    {
        List<Product> products = new List<Product>();
        HomeIndexViewModel homeVM = new()
        {
            Heading = "Browse Products",
            Description = "Buy and sell products with students."
        };

        homeVM.SearchTerm = searchTerm;
        homeVM.CategoryId = categoryId;
        homeVM.MinPrice = minPrice;
        homeVM.MaxPrice = maxPrice;
        homeVM.SortBy = string.IsNullOrEmpty(sortBy) ? "newest" : sortBy;
        homeVM.Categories = _categoryBL.GetAll();

        try
        {
            products = _productBL.GetAvailable(searchTerm, categoryId, minPrice, maxPrice, homeVM.SortBy);
        }
        catch (ArgumentException)
        {
            homeVM.ErrorMessage = "Invalid price range. Min price must be less than max price.";
        }

        foreach (Product product in products)
        {
            ProductCardViewModel cardVM = new()
            {
                Id = product.Id,
                IsSample = CampusX.Data.Seed.SampleProductSeeder.IsSampleProduct(product.Id),
                Name = product.Name,
                Price = product.Price,
                CategoryName = product.Category?.Name ?? "Uncategorized",
                College = product.College ?? "General",
                PhoneNumber = product.PhoneNumber ?? string.Empty,
                ImageUrl = product.ImageUrl,
                Description = product.Description,
                SellerName = product.Seller?.FullName ?? "Campus Student",
                CreatedAt = product.CreatedAt
            };
            cardVM.CanDeleteSample = cardVM.IsSample && _environment.IsDevelopment();
            homeVM.Products.Add(cardVM);
        }

        return View("Index", homeVM);
    }

    public IActionResult Details(Guid id)
    {
        Product? product = _productBL.GetById(id);

        if (product is null)
            return NotFound();

        ProductDetailsViewModel detailsVM = new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            CategoryName = product.Category?.Name ?? "Uncategorized",
            College = product.College,
            PhoneNumber = product.PhoneNumber,
            Status = product.Status,
            SellerName = product.Seller?.FullName ?? "Seller information unavailable",
            SellerCollegeName = product.Seller?.CollegeName,
            SellerEmail = product.Seller?.Email,
            SellerPhoneNumber = product.Seller?.PhoneNumber
        };

        return View(detailsVM);
    }

    public IActionResult Error() => View();
}


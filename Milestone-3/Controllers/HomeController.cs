using CampusX.Models;
using CampusX.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

public class HomeController : Controller
{
    private readonly ProductBL _productBL;
    private readonly IWebHostEnvironment _environment;

    public HomeController(ProductBL productBL, IWebHostEnvironment environment)
    {
        _productBL = productBL;
        _environment = environment;
    }

    public IActionResult Index()
    {
        List<Product> products = _productBL.GetAvailable();
        HomeIndexViewModel homeVM = new()
        {
            Heading = "Browse Products",
            Description = "Buy and sell products with students."
        };

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

using Microsoft.AspNetCore.Mvc;
using CampusX.Models;
using CampusX.ViewModels;

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

        HomeIndexViewModel homeVM = new HomeIndexViewModel();

        homeVM.Heading = "Browse Products";
        homeVM.Description = "Buy and sell products with students.";

        foreach (Product product in products)
        {
            ProductCardViewModel cardVM = new ProductCardViewModel();

            cardVM.Id = product.Id;
            cardVM.IsSample = CampusX.Data.Seed.SampleProductSeeder.IsSampleProduct(product.Id);
            cardVM.CanDeleteSample = cardVM.IsSample && _environment.IsDevelopment();
            cardVM.Name = product.Name;
            cardVM.Price = product.Price;
            cardVM.CategoryName = product.Category?.Name ?? "Uncategorized";
            cardVM.ImageUrl = product.ImageUrl;
            cardVM.Description = product.Description;
            cardVM.SellerName = product.Seller.FullName;
            cardVM.CreatedAt = product.CreatedAt;

            homeVM.Products.Add(cardVM);
        }

        return View("Index", homeVM);
    }
    
    public IActionResult Error() => View();
}

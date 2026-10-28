using CampusX.Models;
using CampusX.ViewModels;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductBL _productBL;

        public HomeController(ProductBL productBL)
        {
            _productBL = productBL;
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
                cardVM.Name = product.Name;
                cardVM.Price = product.Price;
                cardVM.CategoryName = product.Category?.Name ?? "Uncategorized";
                cardVM.College = product.College ?? "General";        
                cardVM.PhoneNumber = product.PhoneNumber ?? string.Empty; 
                cardVM.ImageUrl = product.ImageUrl;
                cardVM.Description = product.Description;
                cardVM.SellerName = product.Seller?.FullName ?? "Campus Student";
                cardVM.CreatedAt = product.CreatedAt;

                homeVM.Products.Add(cardVM);
            }

            return View("Index", homeVM);
        }

        public IActionResult Error() => View();
    }
}
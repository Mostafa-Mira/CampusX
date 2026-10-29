using Microsoft.AspNetCore.Mvc;
using CampusX.Models;
using CampusX.ViewModels;
using System.Collections.Generic;
using System.Linq;

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
                cardVM.ImageUrl = product.ImageUrl;
                cardVM.Description = product.Description;
                cardVM.SellerName = product.Seller?.FullName ?? "Campus Student";
                cardVM.CreatedAt = product.CreatedAt;

                homeVM.Products.Add(cardVM);
            }

            return View("Index", homeVM);
        }

        public IActionResult Details(Guid id)
        {
            Product? product = _productBL.GetById(id);

            if (product is null)
                return NotFound();

            ProductDetailsViewModel detailsVM = new ProductDetailsViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryName = product.Category?.Name ?? "Uncategorized",
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
}

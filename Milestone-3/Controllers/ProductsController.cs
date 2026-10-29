using CampusX.Data.DbContexts;
using CampusX.Models;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CampusX.Controllers
{
    public class ProductsController : Controller
    {
        private readonly CampusXDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(CampusXDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Add() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddProductViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string uniqueFileName = string.Empty;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                uniqueFileName = Guid.NewGuid() + "_" + model.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using var fileStream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(fileStream);
            }

            var defaultUser = _context.ApplicationUsers.FirstOrDefault();
            if (defaultUser == null)
            {
                defaultUser = new ApplicationUser
                {
                    FullName = "Test Student",
                    Email = "student@campusx.com",
                    CollegeName = "General"
                };
                defaultUser.SetPassword("Password123!");
                _context.ApplicationUsers.Add(defaultUser);
            }

            string categoryName = model.Category.Trim();
            var category = _context.Categories.FirstOrDefault(category => category.Name == categoryName);
            if (category == null)
            {
                category = new Category { Name = categoryName };
                _context.Categories.Add(category);
            }

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                CategoryId = category.Id,
                College = string.IsNullOrWhiteSpace(model.College) ? "General" : model.College,
                PhoneNumber = model.PhoneNumber,
                ImageUrl = !string.IsNullOrEmpty(uniqueFileName) ? "/images/products/" + uniqueFileName : string.Empty,
                SellerId = defaultUser.Id
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        #region Mostafa - Product Edit Functionality (GET & POST)

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var viewModel = new AddProductViewModel
            {
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = _context.Categories.Where(c => c.Id == product.CategoryId).Select(c => c.Name).FirstOrDefault() ?? string.Empty,
                College = product.College,
                PhoneNumber = product.PhoneNumber
            };

            ViewBag.ProductId = product.Id;

            // هنا بنوجهه صراحة لملف الـ Edit.cshtml المستقل
            return View("Edit", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ProductId = id;
                // في حالة وجود خطأ في الـ Validation نرجع لصفحة الـ Edit برضه ومعاها الـ ID
                return View("Edit", model);
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            string uniqueFileName = product.ImageUrl;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                uniqueFileName = Guid.NewGuid() + "_" + model.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using var fileStream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(fileStream);

                uniqueFileName = "/images/products/" + uniqueFileName;
            }

            string categoryName = model.Category.Trim();
            var category = _context.Categories.FirstOrDefault(c => c.Name == categoryName);
            if (category == null)
            {
                category = new Category { Name = categoryName };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }

            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.CategoryId = category.Id;
            product.College = string.IsNullOrWhiteSpace(model.College) ? "General" : model.College;
            product.PhoneNumber = model.PhoneNumber;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                product.ImageUrl = uniqueFileName;
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Home", new { id = product.Id });
        }
        #endregion
    }
}
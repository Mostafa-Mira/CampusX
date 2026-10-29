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

        // GET: /Products/Add
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        // POST: /Products/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = string.Empty;

                // معالجة رفع الصورة
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                }

                // التأكد من وجود مستخدم في القاعدة، وإذا لم يوجد نقوم بإنشاء مستخدم تجريبي تلقائياً
                var defaultUser = _context.ApplicationUsers.FirstOrDefault();
                if (defaultUser == null)
                {
                    defaultUser = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        FullName = "Test Student",
                        Email = "student@campusx.com",
                        CollegeName = "General"
                    };
                    // تعيين كلمة مرور افتراضية باستخدام الـ Method الموجودة في الكلاس
                    defaultUser.SetPassword("Password123!");

                    _context.ApplicationUsers.Add(defaultUser);
                    await _context.SaveChangesAsync();
                }

                // Use the category entered by the client so it can be shown consistently on product details.
                string categoryName = model.Category.Trim();
                var category = _context.Categories.FirstOrDefault(category => category.Name == categoryName);
                if (category == null)
                {
                    category = new Category
                    {
                        Name = categoryName
                    };
                    _context.Categories.Add(category);
                }

                // ربط الـ ViewModel مع كلاس Product لحفظه
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    CategoryId = category.Id,
                    ImageUrl = !string.IsNullOrEmpty(uniqueFileName) ? "/images/products/" + uniqueFileName : string.Empty,
                    SellerId = defaultUser.Id
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }
    }
}

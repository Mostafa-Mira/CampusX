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

                // التأكد من وجود قسم (Category) افتراضي أو جلبه
                var defaultCategory = _context.Categories.FirstOrDefault();
                Guid categoryId;
                if (defaultCategory == null)
                {
                    var newCategory = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "General"
                    };
                    _context.Categories.Add(newCategory);
                    await _context.SaveChangesAsync();
                    categoryId = newCategory.Id;
                }
                else
                {
                    categoryId = defaultCategory.Id;
                }

                // ربط الـ ViewModel مع كلاس Product لحفظه
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    CategoryId = categoryId,
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
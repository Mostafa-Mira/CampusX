using CampusX.Data.DbContexts;
using CampusX.Models;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using CampusX.Options;
using CampusX.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using AutoMapper; // أضفنا مكتبة الـ AutoMapper هنا
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CampusX.Controllers
{
    public class ProductsController : Controller
    {
        private const long MaxProductImageSizeBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedProductImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly CampusXDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly RevenueOptions _revenueOptions;
        private readonly IMapper _mapper; // متغير الـ AutoMapper

        public ProductsController(
            CampusXDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IOptions<RevenueOptions> revenueOptions,
            IMapper mapper) // حقن الـ IMapper في الكونستركتور
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _revenueOptions = revenueOptions.Value;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.CommissionRatePercent = _revenueOptions.CommissionRatePercent;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddProductViewModel model)
        {
            if (!ModelState.IsValid || !TryValidateProductImage(model.ImageFile))
                return View(model);

            string imageUrl = string.Empty;
            if (model.ImageFile is { Length: > 0 })
            {
                try
                {
                    imageUrl = await SaveProductImageAsync(model.ImageFile);
                }
                catch (IOException)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "The image could not be uploaded. Please try another file.");
                    return View(model);
                }
                catch (UnauthorizedAccessException)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "The image could not be uploaded. Please contact an administrator.");
                    return View(model);
                }
            }

            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
                return Forbid();

            var student = await _context.ApplicationUsers.FindAsync(studentId);
            if (student?.Role != UserRole.Student || student.IsBlocked)
                return Forbid();

            string categoryName = model.Category.Trim();
            var category = _context.Categories.FirstOrDefault(category => category.Name == categoryName);
            if (category == null)
            {
                category = new Category { Name = categoryName };
                _context.Categories.Add(category);
            }

            // استخدام AutoMapper لإنشاء كائن الـ Product وتحويل البيانات الأساسية تلقائياً
            var product = _mapper.Map<Product>(model);

            // استكمال الخصائص التي تتطلب منطق خاص (Logic)
            product.CategoryId = category.Id;
            product.College = string.IsNullOrWhiteSpace(model.College) ? "General" : model.College;
            product.ImageUrl = imageUrl;
            product.SellerId = studentId;

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

            // استخدام AutoMapper للتحويل من Product إلى AddProductViewModel
            var viewModel = _mapper.Map<AddProductViewModel>(product);

            // ضبط اسم الكلية والفئة يدوياً للتأكد من مطابقتها للعرض
            viewModel.Category = _context.Categories.Where(c => c.Id == product.CategoryId).Select(c => c.Name).FirstOrDefault() ?? string.Empty;

            ViewBag.ProductId = product.Id;

            return View("Edit", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddProductViewModel model)
        {
            if (!ModelState.IsValid || !TryValidateProductImage(model.ImageFile))
            {
                ViewBag.ProductId = id;
                return View("Edit", model);
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            string imageUrl = product.ImageUrl;
            if (model.ImageFile is { Length: > 0 })
            {
                try
                {
                    imageUrl = await SaveProductImageAsync(model.ImageFile);
                }
                catch (IOException)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "The image could not be uploaded. Please try another file.");
                    ViewBag.ProductId = id;
                    return View("Edit", model);
                }
                catch (UnauthorizedAccessException)
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "The image could not be uploaded. Please contact an administrator.");
                    ViewBag.ProductId = id;
                    return View("Edit", model);
                }
            }

            string categoryName = model.Category.Trim();
            var category = _context.Categories.FirstOrDefault(c => c.Name == categoryName);
            if (category == null)
            {
                category = new Category { Name = categoryName };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
            }

            // استخدام AutoMapper لتحديث بيانات الـ Product من الـ Model
            _mapper.Map(model, product);

            // الحفاظ على الخصائص الخاصة بالـ System والـ Database
            product.CategoryId = category.Id;
            product.College = string.IsNullOrWhiteSpace(model.College) ? "General" : model.College;
            product.ImageUrl = imageUrl;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Home", new { id = product.Id });
        }
        #endregion

        private bool TryValidateProductImage(IFormFile? imageFile)
        {
            if (imageFile is null || imageFile.Length == 0)
                return true;

            if (imageFile.Length > MaxProductImageSizeBytes)
            {
                ModelState.AddModelError(nameof(AddProductViewModel.ImageFile), "Image files must be 5 MB or smaller.");
                return false;
            }

            string extension = Path.GetExtension(Path.GetFileName(imageFile.FileName));
            if (!AllowedProductImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(AddProductViewModel.ImageFile), "Upload a JPG, PNG, WEBP, or GIF image.");
                return false;
            }

            return true;
        }

        private async Task<string> SaveProductImageAsync(IFormFile imageFile)
        {
            string? filePath = null;

            try
            {
                if (string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
                    throw new IOException("The application's web root directory is unavailable.");

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);

                string extension = Path.GetExtension(Path.GetFileName(imageFile.FileName)).ToLowerInvariant();
                string fileName = $"{Guid.NewGuid():N}{extension}";
                filePath = Path.Combine(uploadsFolder, fileName);

                await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await imageFile.CopyToAsync(fileStream);

                return $"/images/products/{fileName}";
            }
            catch
            {
                TryDeleteIncompleteImage(filePath);
                throw;
            }
        }

        private static void TryDeleteIncompleteImage(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
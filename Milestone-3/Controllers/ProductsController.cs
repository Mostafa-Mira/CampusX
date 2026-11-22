using CampusX.Models;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using CampusX.Options;
using CampusX.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AutoMapper; // أضفنا مكتبة الـ AutoMapper هنا
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CampusX.Controllers
{
    [Authorize(Policy = MarketplaceAccessPolicies.Student)]
    public class ProductsController : Controller
    {
        private const long MaxProductImageSizeBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedProductImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly ProductBL _productBL;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly RevenueOptions _revenueOptions;
        private readonly IMapper _mapper; // متغير الـ AutoMapper

        public ProductsController(
            ProductBL productBL,
            IWebHostEnvironment webHostEnvironment,
            IOptions<RevenueOptions> revenueOptions,
            IMapper mapper) // حقن الـ IMapper في الكونستركتور
        {
            _productBL = productBL;
            _webHostEnvironment = webHostEnvironment;
            _revenueOptions = revenueOptions.Value;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.CommissionRatePercent = _revenueOptions.CommissionRatePercent;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddProductViewModel model)
        {
            if (!ModelState.IsValid || !TryValidateProductImage(model.ImageFile))
                return View(model);

            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
                return Forbid();

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

            var product = _mapper.Map<Product>(model);
            product.ImageUrl = imageUrl;
            try
            {
                await _productBL.AddAsync(studentId, product, model.Category);
            }
            catch (UnauthorizedAccessException)
            {
                TryDeleteUploadedImage(imageUrl);
                return Forbid();
            }

            return RedirectToAction("Index", "Home");
        }

        #region Mostafa - Product Edit Functionality (GET & POST)

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
                return Forbid();

            try
            {
                var product = await _productBL.GetForEditAsync(studentId, id);
                var viewModel = _mapper.Map<AddProductViewModel>(product);
                viewModel.Category = product.Category?.Name ?? string.Empty;
                ViewBag.ProductId = product.Id;
                return View("Edit", viewModel);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromRoute] Guid id, AddProductViewModel model)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
                return Forbid();

            Product product;
            try
            {
                product = await _productBL.GetForEditAsync(studentId, id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            if (!ModelState.IsValid || !TryValidateProductImage(model.ImageFile))
            {
                ViewBag.ProductId = id;
                return View("Edit", model);
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

            var changes = _mapper.Map<Product>(model);
            changes.ImageUrl = imageUrl;
            try
            {
                await _productBL.EditAsync(studentId, id, changes, model.Category);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (model.ImageFile is { Length: > 0 })
                    TryDeleteUploadedImage(imageUrl);
                return NotFound();
            }

            TempData["ProductSuccess"] = "Product changes saved.";
            return RedirectToAction("Details", "Home", new { id });
        }
        #endregion

        private void TryDeleteUploadedImage(string imageUrl)
        {
            if (imageUrl.Length != 0)
                TryDeleteIncompleteImage(Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", Path.GetFileName(imageUrl)));
        }

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

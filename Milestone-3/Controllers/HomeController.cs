
using CampusX.Models;
using CampusX.Services;
using CampusX.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CampusX.Controllers;

public class HomeController : Controller
{
    private readonly ProductBL _productBL;
    private readonly IWebHostEnvironment _environment;
    private readonly CategoryBL _categoryBL;
    private readonly IAuthorizationService _authorization;

    public HomeController(ProductBL productBL, IWebHostEnvironment environment, CategoryBL categoryBL,
        IAuthorizationService authorization)
    {
        _productBL = productBL;
        _environment = environment;
        _categoryBL = categoryBL;
        _authorization = authorization;
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
                CanDelete = _environment.IsDevelopment(),
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
            homeVM.Products.Add(cardVM);
        }

        return View("Index", homeVM);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        Product? product = _productBL.GetById(id);

        if (product is null)
            return NotFound();

        var isDevelopment = _environment.IsDevelopment();
        var canEdit = (await _authorization.AuthorizeAsync(User, product, MarketplaceAccessPolicies.ProductOwner)).Succeeded &&
            (await _authorization.AuthorizeAsync(User, MarketplaceAccessPolicies.Student)).Succeeded;
        var canModerate = isDevelopment &&
            (await _authorization.AuthorizeAsync(User, MarketplaceAccessPolicies.Admin)).Succeeded;

        ProductDetailsViewModel detailsVM = new()
        {
            Id = product.Id,
            SellerId = product.SellerId,
            CanEdit = canEdit,
            CanModerate = canModerate,
            CanRemove = canModerate || (isDevelopment && canEdit),
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

    [AllowAnonymous]
    public IActionResult Error() => View();
}


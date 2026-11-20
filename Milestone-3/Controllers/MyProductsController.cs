using System.Security.Claims;
using CampusX.Models;
using CampusX.Services;
using CampusX.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

// My Products: lists only the signed-in student's own (non-deleted) listings.
// Kept in its own controller so ProductsController (Add/Edit) is not touched.
[Authorize(Policy = MarketplaceAccessPolicies.Student)]
[Route("products/mine")]
public class MyProductsController : Controller
{
    private readonly ProductBL _productBL;

    public MyProductsController(ProductBL productBL)
    {
        _productBL = productBL;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        // The seller id always comes from the authenticated identity, never from the request.
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
            return Forbid();

        MyProductsViewModel vm = new();

        foreach (Product product in _productBL.GetBySeller(studentId))
        {
            vm.Items.Add(new MyProductItemViewModel
            {
                Status = product.Status,
                Card = new ProductCardViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    CategoryName = product.Category?.Name ?? "Uncategorized",
                    College = product.College ?? "General",
                    PhoneNumber = product.PhoneNumber ?? string.Empty,
                    ImageUrl = product.ImageUrl,
                    Description = product.Description,
                    SellerName = "You",
                    CreatedAt = product.CreatedAt
                }
            });
        }

        return View(vm);
    }
}

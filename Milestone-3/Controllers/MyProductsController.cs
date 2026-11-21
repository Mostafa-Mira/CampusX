using System.Security.Claims;
using CampusX.Models;
using CampusX.Services;
using CampusX.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Controllers;


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
    
    [HttpPost("{id:guid}/sold")]
    [ValidateAntiForgeryToken]
    public IActionResult MarkSold([FromRoute] Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var studentId))
            return Forbid();

        try
        {
            _productBL.MarkSold(studentId, id);
            TempData["ProductSuccess"] = "Product marked as sold.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }

        return RedirectToAction("Details", "Home", new { id });
    }
}

using System.Security.Claims;
using CampusX.Models;
using CampusX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Controllers;

[Route("dev/products")]
public sealed class ProductsDeleteController : Controller
{
    private readonly ProductBL _productBL;
    private readonly IWebHostEnvironment _environment;

    public ProductsDeleteController(ProductBL productBL, IWebHostEnvironment environment)
    {
        _productBL = productBL;
        _environment = environment;
    }

    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = MarketplaceAccessPolicies.Student)]
    [ValidateAntiForgeryToken]
    public IActionResult Delete([FromRoute] Guid id) =>
        RemoveListing(id, _productBL.Delete, "Product deleted successfully.");

    [HttpPost("{id:guid}/remove")]
    [Authorize(Policy = MarketplaceAccessPolicies.Admin)]
    [ValidateAntiForgeryToken]
    public IActionResult Remove([FromRoute] Guid id) =>
        RemoveListing(id, _productBL.RemoveByAdmin, "Product removed from the marketplace.");

    private IActionResult RemoveListing(Guid id, Action<Guid, Guid> remove, string successMessage)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Forbid();

        try
        {
            remove(userId, id);
            TempData["SampleProductMessage"] = successMessage;
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
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 547 })
        {
            TempData["ProductError"] = "This listing has related marketplace activity and could not be deleted.";
            return RedirectToAction("Details", "Home", new { id });
        }

        return RedirectToAction("Index", "Home");
    }
}

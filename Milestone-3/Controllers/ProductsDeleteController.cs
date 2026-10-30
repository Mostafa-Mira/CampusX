using CampusX.Models;
using Microsoft.AspNetCore.Mvc;

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
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        try
        {
            _productBL.DeleteById(id);
            TempData["SampleProductMessage"] = "Product deleted successfully.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["SampleProductMessage"] = exception.Message;
        }

        return RedirectToAction("Index", "Home");
    }
}

using CampusX.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

[Route("dev/sample-products")]
public sealed class SampleProductsController : Controller
{
    private readonly ProductBL _productBL;
    private readonly IWebHostEnvironment _environment;

    public SampleProductsController(ProductBL productBL, IWebHostEnvironment environment)
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
            TempData["SampleProductMessage"] = $"Deleted sample product {id}.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["SampleProductMessage"] = exception.Message;
        }

        return RedirectToAction("Index", "Home");
    }
}

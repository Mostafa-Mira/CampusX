using System.Security.Claims;
using CampusX.Enums;
using CampusX.Models;
using CampusX.Options;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CampusX.Controllers;

[Authorize]
public class RevenueController : Controller
{
    private readonly RevenueBL _revenueBL;
    private readonly RevenueOptions _options;

    public RevenueController(RevenueBL revenueBL, IOptions<RevenueOptions> options)
    {
        _revenueBL = revenueBL;
        _options = options.Value;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!TryGetCurrentStudentId(out var studentId))
            return Forbid();

        try
        {
            var viewModel = new RevenueDashboardViewModel
            {
                AvailableCredits = _revenueBL.GetAvailableCredits(studentId),
                CommissionRatePercent = _options.CommissionRatePercent,
                Packages =
                [
                    new() { Package = PromotionPackage.Basic, Credits = (int)PromotionPackage.Basic, Price = _options.BasicPackagePrice },
                    new() { Package = PromotionPackage.Standard, Credits = (int)PromotionPackage.Standard, Price = _options.StandardPackagePrice },
                    new() { Package = PromotionPackage.Premium, Credits = (int)PromotionPackage.Premium, Price = _options.PremiumPackagePrice }
                ],
                Products = _revenueBL.GetOwnedAvailableProducts(studentId)
                    .Select(product => new PromotableProductViewModel
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        PromotionExpiresAt = product.PromotionExpiresAt
                    }).ToList()
            };
            return View(viewModel);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Purchase(PromotionPackage package)
    {
        if (!TryGetCurrentStudentId(out var studentId))
            return Forbid();

        try
        {
            var purchase = _revenueBL.PurchaseCredits(studentId, package);
            TempData["RevenueSuccess"] = $"Simulated payment complete. {purchase.CreditsPurchased} credits were added.";
        }
        catch (ArgumentOutOfRangeException)
        {
            TempData["RevenueError"] = "Select a valid credit package.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            TempData["RevenueError"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Promote(Guid productId)
    {
        if (!TryGetCurrentStudentId(out var studentId))
            return Forbid();

        try
        {
            var promotion = _revenueBL.ActivatePromotion(studentId, productId);
            TempData["RevenueSuccess"] = $"Promotion activated until {promotion.ExpiresAt.ToLocalTime():dd MMM yyyy}.";
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException exception)
        {
            TempData["RevenueError"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private bool TryGetCurrentStudentId(out Guid studentId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out studentId);
}

using CampusX.Models;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusX.Services;

namespace CampusX.Controllers;

public partial class AccountController : Controller
{
    private readonly ApplicationUserBL _applicationUserBL;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UniversityEmailVerificationBL _verification;

    public AccountController(ApplicationUserBL applicationUserBL, SignInManager<ApplicationUser> signInManager,
        UniversityEmailVerificationBL verification)
    {
        _applicationUserBL = applicationUserBL;
        _signInManager = signInManager;
        _verification = verification;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool googleError = false)
    {
        if (googleError)
            ModelState.AddModelError(string.Empty, "Google sign-in was cancelled or could not finish. Please try again.");
        return View(new LoginViewModel { ReturnUrl = LocalReturnUrl(returnUrl) });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, [FromServices] MarketplaceAccessHandler access)
    {
        model.ReturnUrl = LocalReturnUrl(model.ReturnUrl);
        ModelState.Remove(nameof(LoginViewModel.ReturnUrl));
        if (!ModelState.IsValid)
            return View(model);

        var user = await _applicationUserBL.AuthenticateAsync(model.Email, model.Password);
        var status = user is null ? MarketplaceAccessStatus.MissingAccount : await access.GetStatusAsync(user.Id);
        if (user is null || status is MarketplaceAccessStatus.Blocked or MarketplaceAccessStatus.MissingAccount)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            model.Password = string.Empty;
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return AfterSignIn(status, model.ReturnUrl);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult SignUp() => View(new SignUpViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(SignUpViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        ApplicationUser user;
        try
        {
            user = await _applicationUserBL.RegisterAsync(new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                UniversityEmail = model.UniversityEmail.Trim(),
                CollegeName = string.Empty
            }, model.Password, model.University);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["AccountMessage"] = "Account created. University email verification is required before you can enter the marketplace.";
        return RedirectToAction(nameof(PendingUniversityVerification));
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["AccountMessage"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpGet]
    public Task<IActionResult> PendingUniversityVerification() => VerificationPageAsync();

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpGet]
    public Task<IActionResult> UniversityEmail() => VerificationPageAsync(allowConfirmed: true);

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendUniversityVerificationCode()
    {
        return await RunVerificationAsync(() => _verification.SendAsync(CurrentUserId));
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyUniversityEmail(UniversityVerificationViewModel model)
    {
        // Invalid submissions count toward the persisted budget; never echo the submitted code.
        var code = model.Code;
        ModelState.Clear();
        return await RunVerificationAsync(() => _verification.VerifyAsync(CurrentUserId, code), enterMarketplace: true);
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUniversityEmail(ChangeUniversityEmailViewModel model)
    {
        if (!ModelState.IsValid)
            return await VerificationPageAsync(allowConfirmed: true);
        return await RunVerificationAsync(() => _verification.ChangeEmailAsync(CurrentUserId, model.UniversityEmail));
    }

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private string? LocalReturnUrl(string? url) => Url.IsLocalUrl(url) ? url : null;

    private IActionResult AfterSignIn(MarketplaceAccessStatus status, string? returnUrl)
    {
        if (status == MarketplaceAccessStatus.PendingVerification)
            return RedirectToAction(nameof(PendingUniversityVerification));
        return LocalReturnUrl(returnUrl) is { } local ? LocalRedirect(local) : RedirectToAction("Index", "Home");
    }

    private async Task<IActionResult> RunVerificationAsync(Func<Task<(bool Success, string Message)>> operation,
        bool enterMarketplace = false)
    {
        try
        {
            var result = await operation();
            if (result.Success)
            {
                TempData["AccountMessage"] = result.Message;
                return enterMarketplace ? RedirectToAction("Index", "Home") : RedirectToAction(nameof(UniversityEmail));
            }
            ModelState.AddModelError(string.Empty, result.Message);
        }
        catch (UnauthorizedAccessException) { return DeniedVerification(); }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number == 51020)
        {
            ModelState.AddModelError(string.Empty, "Verification is busy. Please try again.");
        }
        return await VerificationPageAsync(allowConfirmed: true);
    }

    private async Task<IActionResult> VerificationPageAsync(bool allowConfirmed = false)
    {
        try
        {
            var model = await _verification.GetPageAsync(CurrentUserId);
            if (model.IsConfirmed && !allowConfirmed)
                return RedirectToAction("Index", "Home");
            return View("PendingUniversityVerification", model);
        }
        catch (UnauthorizedAccessException) { return DeniedVerification(); }
    }

    private IActionResult DeniedVerification()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("AccessDenied");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}

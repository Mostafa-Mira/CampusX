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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UniversityEmailVerificationBL _verification;

    public AccountController(ApplicationUserBL applicationUserBL, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, UniversityEmailVerificationBL verification)
    {
        _applicationUserBL = applicationUserBL;
        _userManager = userManager;
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
    public async Task<IActionResult> Login(LoginViewModel model, [FromServices] MarketplaceAccessEvaluator access)
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

    private Guid CurrentUserId => Guid.TryParse(
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    private string? LocalReturnUrl(string? url) => Url.IsLocalUrl(url) ? url : null;

    private IActionResult AfterSignIn(MarketplaceAccessStatus status, string? returnUrl)
    {
        if (status == MarketplaceAccessStatus.PendingVerification)
            return RedirectToAction(nameof(PendingUniversityVerification));
        return LocalReturnUrl(returnUrl) is { } local ? LocalRedirect(local) : RedirectToAction("Index", "Home");
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

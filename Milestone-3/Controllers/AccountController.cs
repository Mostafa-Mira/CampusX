using System.Security.Claims;
using CampusX.Models;
using CampusX.Options;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CampusX.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationUserBL _applicationUserBL;
    private readonly UniversityEmailOptions _universityEmailOptions;

    public AccountController(ApplicationUserBL applicationUserBL, IOptions<UniversityEmailOptions> universityEmailOptions)
    {
        _applicationUserBL = applicationUserBL;
        _universityEmailOptions = universityEmailOptions.Value;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
    });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.ReturnUrl = Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : null;
        if (!ModelState.IsValid)
            return View(model);

        var user = _applicationUserBL.Authenticate(model.Email, model.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            model.Password = string.Empty;
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });

        return model.ReturnUrl is not null ? LocalRedirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }


    [AllowAnonymous]
    [HttpGet]
    public IActionResult SignUp() => View(new SignUpViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignUp(SignUpViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!IsApprovedUniversityEmail(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Registration is unavailable until an approved university email domain is configured.");
            return View(model);
        }

        try
        {
            _applicationUserBL.Register(new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                CollegeName = string.Empty
            }, model.Password);
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
            return View(model);
        }

        TempData["AccountMessage"] = "Account created. Your university email ownership is not verified in this development version.";
        return RedirectToAction(nameof(Login));
    }

    private bool IsApprovedUniversityEmail(string email)
    {
        var atIndex = email.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
            return false;

        var domain = email[(atIndex + 1)..].Trim().ToLowerInvariant();
        return _universityEmailOptions.ApprovedDomains.Any(approved =>
            string.Equals(approved.Trim().TrimStart('@'), domain, StringComparison.OrdinalIgnoreCase));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}

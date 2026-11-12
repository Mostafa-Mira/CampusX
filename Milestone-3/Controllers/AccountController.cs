using System.Security.Claims;
using CampusX.Models;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusX.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationUserBL _applicationUserBL;

    public AccountController(ApplicationUserBL applicationUserBL)
    {
        _applicationUserBL = applicationUserBL;
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

        TempData["AccountMessage"] = "Account created. You can now sign in.";
        return RedirectToAction(nameof(Login));
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
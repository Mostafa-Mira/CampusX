using CampusX.Models;
using CampusX.Services;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CampusX.Controllers;

public partial class AccountController
{
    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting(PasswordRecoveryQueue.RequestPolicy)]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model, [FromServices] PasswordRecoveryQueue queue)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Queue every valid request so the HTTP response never waits for account lookup or SMTP.
        var resetPath = Url.Action(nameof(ResetPassword));
        if (resetPath is not null)
            queue.Enqueue(model.Email.Trim(), resetPath);
        return RedirectToAction(nameof(ForgotPasswordSubmitted));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPasswordSubmitted() => View();

    [AllowAnonymous, HttpGet]
    public IActionResult ResetPassword()
    {
        SetResetPageHeaders();
        return View(new ResetPasswordViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting(PasswordRecoveryQueue.ResetPolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model,
        [FromServices] PasswordRecoveryBL recovery, CancellationToken cancellationToken)
    {
        SetResetPageHeaders();
        if (!ModelState.IsValid)
            return View(model);

        var result = await recovery.ResetAsync(model.Ticket, model.Password, model.UniversityCode, cancellationToken);
        if (result?.Succeeded == true)
        {
            await _signInManager.SignOutAsync();
            TempData["AccountMessage"] = "Your password was reset. Sign in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        var passwordErrors = result?.Errors.Where(error => error.Code.StartsWith("Password", StringComparison.Ordinal)).ToList();
        if (passwordErrors is { Count: > 0 })
        {
            foreach (var error in passwordErrors)
                ModelState.AddModelError(nameof(model.Password), error.Description);
        }
        else
        {
            ModelState.AddModelError(string.Empty, "This reset link or confirmation code is invalid, expired, or already used.");
        }
        return View(model);
    }

    private void SetResetPageHeaders()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }
}

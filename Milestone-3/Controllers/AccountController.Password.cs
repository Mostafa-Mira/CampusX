using CampusX.Models;
using CampusX.Services;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CampusX.Controllers;

public partial class AccountController
{
    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpGet]
    public async Task<IActionResult> Settings([FromServices] IAuthenticationSchemeProvider schemes)
    {
        var manager = _signInManager.UserManager;
        var user = await manager.GetUserAsync(User);
        if (user is not { IsBlocked: false })
            return DeniedVerification();

        return View(new AccountSettingsViewModel(await schemes.GetSchemeAsync(Google) is not null,
            (await manager.GetLoginsAsync(user)).Any(login => login.LoginProvider == Google),
            await manager.HasPasswordAsync(user),
            StudentUniversities.HasVerifiedEmail(user.UniversityEmail, user.VerifiedUniversityEmail, user.UniversityEmailConfirmed)));
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpGet]
    public Task<IActionResult> SetPassword() => PasswordSetupPageAsync(new SetPasswordViewModel());

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordSetupCode()
    {
        try
        {
            var (sent, message) = await _verification.SendPasswordSetupCodeAsync(CurrentUserId);
            if (sent)
            {
                TempData["PasswordSetupMessage"] = message;
                return RedirectToAction(nameof(SetPassword));
            }
            ModelState.AddModelError(string.Empty, message);
        }
        catch (UnauthorizedAccessException) { return DeniedVerification(); }
        catch (SqlException exception) when (exception.Number == 51020)
        {
            ModelState.AddModelError(string.Empty, "Verification is busy. Please try again.");
        }
        return await PasswordSetupPageAsync(new SetPasswordViewModel());
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var (user, message) = await _verification.SetPasswordAsync(CurrentUserId, model.Code, model.NewPassword);
                if (user is not null)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["AccountMessage"] = message;
                    return RedirectToAction(nameof(Settings));
                }
                ModelState.AddModelError(string.Empty, message);
            }
            catch (UnauthorizedAccessException) { return DeniedVerification(); }
            catch (SqlException exception) when (exception.Number == 51020)
            {
                ModelState.AddModelError(string.Empty, "Verification is busy. Please try again.");
            }
        }

        // Do not return the submitted code or passwords in the form.
        ModelState.Remove(nameof(model.Code));
        model.Code = null;
        model.NewPassword = model.ConfirmPassword = string.Empty;
        return await PasswordSetupPageAsync(model);
    }

    private async Task<IActionResult> PasswordSetupPageAsync(SetPasswordViewModel model)
    {
        try
        {
            var verification = await _verification.GetPasswordSetupPageAsync(CurrentUserId);
            if (verification is null)
                return RedirectToAction(nameof(Settings));
            model.Verification = verification;
            return View(nameof(SetPassword), model);
        }
        catch (UnauthorizedAccessException) { return DeniedVerification(); }
    }
}

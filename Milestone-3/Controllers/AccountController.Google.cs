using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CampusX.Models;
using CampusX.Services;
using CampusX.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Controllers;

public partial class AccountController
{
    private const string Google = GoogleDefaults.AuthenticationScheme;
    private const string Purpose = "campusx:google-purpose";
    private const string ChallengeId = "campusx:google-challenge";
    private const string ReturnUrl = "campusx:google-return-url";
    private const string SignInPurpose = "sign-in";
    private const string LinkPurpose = "link";

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ContinueWithGoogle(string? returnUrl, [FromServices] IAuthenticationSchemeProvider schemes)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(UniversityEmail));

        return await StartGoogleAsync(schemes, SignInPurpose, nameof(GoogleCallback), LocalReturnUrl(returnUrl));
    }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GoogleCallback([FromServices] MarketplaceAccessEvaluator access)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(UniversityEmail));

        var info = await ReadGoogleAsync(SignInPurpose);
        if (info is null)
            return await GoogleFailureAsync("Google sign-in expired or could not finish. Please try again.");

        var linked = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (linked is not null)
        {
            var status = await access.GetStatusAsync(linked.Id);
            if (status is MarketplaceAccessStatus.Blocked or MarketplaceAccessStatus.MissingAccount)
            {
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                return DeniedVerification();
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
                isPersistent: false, bypassTwoFactor: false);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            if (!result.Succeeded)
                return await GoogleFailureAsync("This account could not sign in with Google.");
            return AfterSignIn(status, info.AuthenticationProperties!.Items[ReturnUrl]);
        }

        var email = VerifiedGoogleEmail(info);
        if (email is null)
            return await GoogleFailureAsync("Google did not provide a verified email for registration.");
        if (await _userManager.FindByEmailAsync(email) is not null)
            return await ExistingGoogleEmailAsync();

        return View("GoogleRegistration", new GoogleRegistrationViewModel
        {
            FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            GoogleEmail = email,
            Challenge = info.AuthenticationProperties!.Items[ChallengeId]!
        });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleRegistration(GoogleRegistrationViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(UniversityEmail));

        var info = await ReadGoogleAsync(SignInPurpose, model.Challenge);
        if (info is null)
            return await GoogleFailureAsync("Google registration expired or changed. Start again from login.");

        var email = VerifiedGoogleEmail(info);
        if (email is null)
            return await GoogleFailureAsync("Google did not provide a verified email for registration.");
        if (await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey) is not null)
            return await GoogleFailureAsync("This Google account is already registered. Sign in with Google.");
        if (await _userManager.FindByEmailAsync(email) is not null)
            return await ExistingGoogleEmailAsync();

        model.GoogleEmail = email;
        if (!StudentUniversities.IsCairoStudentEmail(model.UniversityEmail))
            ModelState.AddModelError(nameof(model.UniversityEmail), "Enter 14 digits followed by @stud.cu.edu.eg.");
        if (!ModelState.IsValid)
            return View(model);

        ApplicationUser user;
        try
        {
            user = await _applicationUserBL.RegisterWithGoogleAsync(new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                Email = email,
                UniversityEmail = StudentUniversities.NormalizeEmail(model.UniversityEmail)
            }, info);
        }
        catch (InvalidOperationException)
        {
            return await GoogleFailureAsync("Could not create the account. If you already registered, log in and link Google.");
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        await _signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: Google);
        TempData["AccountMessage"] = "Account created. Verify your university email to access the marketplace.";
        return RedirectToAction(nameof(PendingUniversityVerification));
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpGet]
    public async Task<IActionResult> GoogleLink([FromServices] IAuthenticationSchemeProvider schemes, bool googleError = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is not { IsBlocked: false })
            return DeniedVerification();
        if (await schemes.GetSchemeAsync(Google) is null)
            return NotFound();

        if (googleError)
            ViewData["GoogleError"] = "Google linking was cancelled or could not finish. Please try again.";

        var logins = await _userManager.GetLoginsAsync(user);
        return View(new GoogleLinkViewModel { IsLinked = logins.Any(login => login.LoginProvider == Google) });
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartGoogleLink([FromServices] IAuthenticationSchemeProvider schemes)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is not { IsBlocked: false })
            return DeniedVerification();
        if (!await _userManager.HasPasswordAsync(user) ||
            (await _userManager.GetLoginsAsync(user)).Any(login => login.LoginProvider == Google))
            return RedirectToAction(nameof(GoogleLink));

        return await StartGoogleAsync(schemes, LinkPurpose, nameof(GoogleLinkCallback), userId: user.Id.ToString());
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpGet]
    public async Task<IActionResult> GoogleLinkCallback()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is not { IsBlocked: false })
            return DeniedVerification();

        var info = await ReadGoogleAsync(LinkPurpose, userId: user.Id.ToString());
        var email = info is null ? null : VerifiedGoogleEmail(info);
        if (email is null)
            return await GoogleFailureAsync("Google linking expired or could not finish. Please try again.", linking: true);

        return View("GoogleLink", new GoogleLinkViewModel
        {
            GoogleEmail = email,
            Challenge = info!.AuthenticationProperties!.Items[ChallengeId]!
        });
    }

    [Authorize(Policy = MarketplaceAccessPolicies.AuthenticatedOnly), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmGoogleLink(GoogleLinkViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is not { IsBlocked: false })
            return DeniedVerification();

        var info = await ReadGoogleAsync(LinkPurpose, model.Challenge, user.Id.ToString());
        var email = info is null ? null : VerifiedGoogleEmail(info);
        if (info is null || email is null)
            return await GoogleFailureAsync("Google linking expired or changed. Please start again.", linking: true);

        model.GoogleEmail = email;
        model.IsLinked = false;
        if (!ModelState.IsValid || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Enter your current CampusX password to link Google.");
            model.Password = string.Empty;
            return View("GoogleLink", model);
        }

        var owner = await _userManager.FindByEmailAsync(email);
        var linked = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if ((owner is not null && owner.Id != user.Id) || (linked is not null && linked.Id != user.Id))
            return await GoogleFailureAsync("This Google account belongs to a different CampusX account.", linking: true);

        if (linked is null)
        {
            if ((await _userManager.GetLoginsAsync(user)).Any(login => login.LoginProvider == Google))
                return await GoogleFailureAsync("A Google account is already linked to this CampusX account.", linking: true);

            try
            {
                if (!(await _userManager.AddLoginAsync(user, info)).Succeeded)
                    return await GoogleFailureAsync("Google could not be linked. Please start again.", linking: true);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
            {
                return await GoogleFailureAsync("This Google account is already linked.", linking: true);
            }
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        TempData["AccountMessage"] = "Google was linked to your CampusX account.";
        return RedirectToAction(nameof(GoogleLink));
    }

    private async Task<IActionResult> StartGoogleAsync(IAuthenticationSchemeProvider schemes,
        string purpose, string callbackAction, string? returnUrl = null, string? userId = null)
    {
        if (await schemes.GetSchemeAsync(Google) is null)
            return NotFound();

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(Google,
            Url.Action(callbackAction, "Account")!, userId);
        properties.Items[Purpose] = purpose;
        properties.Items[ChallengeId] = Guid.NewGuid().ToString("N");
        properties.Items[ReturnUrl] = returnUrl;
        return Challenge(properties, Google);
    }

    private async Task<ExternalLoginInfo?> ReadGoogleAsync(string purpose, string? challenge = null, string? userId = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync(userId);
        var items = info?.AuthenticationProperties?.Items;
        if (info?.LoginProvider != Google || string.IsNullOrWhiteSpace(info.ProviderKey) || info.ProviderKey.Length > 128 ||
            items is null || !items.TryGetValue(Purpose, out var actualPurpose) || actualPurpose != purpose ||
            !items.TryGetValue(ChallengeId, out var actualChallenge) || string.IsNullOrEmpty(actualChallenge) ||
            (HttpMethods.IsPost(Request.Method) && challenge != actualChallenge))
            return null;

        return info;
    }

    private static string? VerifiedGoogleEmail(ExternalLoginInfo info)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email)?.Trim();
        var verified = info.Principal.FindFirstValue("google:email_verified");
        return bool.TryParse(verified, out var confirmed) && confirmed &&
            email is { Length: > 0 and <= 256 } && new EmailAddressAttribute().IsValid(email) ? email : null;
    }

    private Task<IActionResult> ExistingGoogleEmailAsync() => GoogleFailureAsync(
        "We couldn’t sign you in with this Google account. Use your original sign-in method or choose Forgot password.");

    private async Task<IActionResult> GoogleFailureAsync(string message, bool linking = false)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        TempData["GoogleError"] = message;
        return RedirectToAction(linking ? nameof(GoogleLink) : nameof(Login));
    }
}


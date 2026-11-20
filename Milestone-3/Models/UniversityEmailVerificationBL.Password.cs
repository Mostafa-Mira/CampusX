using CampusX.ViewModel;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Models;

public partial class UniversityEmailVerificationBL
{
    private const string PasswordSetupUnavailable = "Set Password requires a linked Google account, no existing password, and a verified Cairo University email.";

    public async Task<UniversityVerificationViewModel?> GetPasswordSetupPageAsync(Guid id)
    {
        var user = await GetAccountAsync(id);
        return await CanSetPasswordAsync(user) ? await BuildPageAsync(user, eligible: true, passwordSetup: true) : null;
    }

    public Task<(bool Success, string Message)> SendPasswordSetupCodeAsync(Guid id) =>
        SendForOperationAsync(id, passwordSetup: true);

    public async Task<(ApplicationUser? User, string Message)> SetPasswordAsync(Guid id, string? code, string password)
    {
        await using var transaction = await BeginMutationAsync();
        // The verification lock serializes changes to the address and code. Hold the user row as well
        // because Identity updates it, and account blocking can run independently of verification.
        var user = await _context.ApplicationUsers
            .FromSqlInterpolated($"SELECT * FROM [dbo].[ApplicationUsers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync();
        if (user is null)
            throw new UnauthorizedAccessException();
        await _context.Entry(user).ReloadAsync();
        if (user.IsBlocked)
            throw new UnauthorizedAccessException();
        if (!await CanSetPasswordAsync(user))
            return (null, PasswordSetupUnavailable);

        var (challenge, error) = await CheckCodeAsync(id, user.VerifiedUniversityEmail!, code, user.SecurityStamp);
        if (challenge is null)
        {
            await transaction.CommitAsync();
            return (null, error!);
        }

        var result = await _users.AddPasswordAsync(user, password);
        if (!result.Succeeded)
        {
            var passwordErrors = result.Errors.Where(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
                .Select(error => error.Description).ToList();
            return (null, passwordErrors.Count > 0 ? string.Join(" ", passwordErrors) : "The password could not be set. Please try again.");
        }

        Consume(challenge);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (user, "Your password was set. You can sign in with Google or your CampusX password.");
    }

    private async Task<bool> CanSetPasswordAsync(ApplicationUser user) =>
        !string.IsNullOrEmpty(user.SecurityStamp) &&
        StudentUniversities.HasVerifiedEmail(user.UniversityEmail, user.VerifiedUniversityEmail, user.UniversityEmailConfirmed) &&
        !await _users.HasPasswordAsync(user) &&
        await _context.UserLogins.AnyAsync(login => login.UserId == user.Id && login.LoginProvider == GoogleDefaults.AuthenticationScheme);
}

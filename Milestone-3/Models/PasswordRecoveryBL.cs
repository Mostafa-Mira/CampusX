using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CampusX.Data.DbContexts;
using CampusX.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Models;

public sealed class PasswordRecoveryBL
{
    private const string TicketPurpose = "CampusX.PasswordRecovery.v1";
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly CampusXDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPasswordRecoveryEmailSender _sender;
    private readonly IConfiguration _configuration;
    private readonly ITimeLimitedDataProtector _protector;

    public PasswordRecoveryBL(CampusXDbContext db, UserManager<ApplicationUser> users,
        IPasswordRecoveryEmailSender sender, IConfiguration configuration, IDataProtectionProvider protection)
    {
        _db = db;
        _users = users;
        _sender = sender;
        _configuration = configuration;
        _protector = protection.CreateProtector(TicketPurpose).ToTimeLimitedDataProtector();
    }

    public async Task SendLinkAsync(string email, string resetPath)
    {
        if (!Uri.TryCreate(_configuration["PasswordRecovery:PublicOrigin"], UriKind.Absolute, out var origin) ||
            origin.Scheme != Uri.UriSchemeHttps || origin.UserInfo.Length != 0 ||
            origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Password recovery requires a configured HTTPS origin.");

        var user = await _users.FindByEmailAsync(email);
        var factors = GetRecoveryAddresses(user);
        if (user is null || factors is null)
            return;

        var (login, university) = factors.Value;
        var code = university is null ? null : Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        var identityToken = await _users.GeneratePasswordResetTokenAsync(user);
        var ticket = new ResetTicket(user.Id, login, university, code is null ? null : Convert.ToHexString(HashCode(code)), identityToken);
        var protectedTicket = _protector.Protect(JsonSerializer.Serialize(ticket), Lifetime);
        var link = new UriBuilder(origin)
        {
            Path = resetPath,
            Fragment = "ticket=" + Uri.EscapeDataString(protectedTicket) + (code is null ? "" : "&proof=1")
        }.Uri;

        if (university is not null && code is not null)
            await _sender.SendUniversityProofAsync(university, code);
        await _sender.SendResetLinkAsync(login, link);
    }

    public async Task<IdentityResult?> ResetAsync(string protectedTicket, string password, string? universityCode,
        CancellationToken cancellationToken)
    {
        ResetTicket? ticket;
        try
        {
            ticket = JsonSerializer.Deserialize<ResetTicket>(_protector.Unprotect(protectedTicket));
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or JsonException)
        {
            return null;
        }
        if (ticket is null || !HasRequiredProof(ticket, universityCode))
            return null;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        // Keep recovery checks and the Identity update atomic with blocking and university-email changes.
        var user = await _db.ApplicationUsers
            .FromSqlInterpolated($"SELECT * FROM [dbo].[ApplicationUsers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {ticket.UserId}")
            .SingleOrDefaultAsync(cancellationToken);
        var addresses = GetRecoveryAddresses(user);
        if (user is null || addresses is null || addresses.Value != (ticket.LoginEmail, ticket.UniversityEmail))
            return null;

        var result = await _users.ResetPasswordAsync(user, ticket.IdentityToken, password);
        if (result.Succeeded)
            await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static (string Login, string? University)? GetRecoveryAddresses(ApplicationUser? user)
    {
        if (user is not { IsBlocked: false } || string.IsNullOrWhiteSpace(user.Email))
            return null;

        var login = user.Email.Trim();
        if (user.EmailConfirmed)
            return (login, null);
        if (!StudentUniversities.HasVerifiedEmail(user.UniversityEmail, user.VerifiedUniversityEmail, user.UniversityEmailConfirmed))
            return null;

        return (login, string.Equals(login, user.VerifiedUniversityEmail, StringComparison.OrdinalIgnoreCase)
            ? null : user.VerifiedUniversityEmail);
    }

    private static bool HasRequiredProof(ResetTicket ticket, string? code) =>
        ticket.UniversityProofHash is null ||
        !string.IsNullOrWhiteSpace(code) && CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(ticket.UniversityProofHash), HashCode(code.Trim().ToUpperInvariant()));

    private static byte[] HashCode(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(code));

    private sealed record ResetTicket(Guid UserId, string LoginEmail, string? UniversityEmail,
        string? UniversityProofHash, string IdentityToken);
}

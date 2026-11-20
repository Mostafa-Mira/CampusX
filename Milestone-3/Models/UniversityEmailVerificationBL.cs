using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using CampusX.Data.DbContexts;
using CampusX.Services;
using CampusX.ViewModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CampusX.Models;

public partial class UniversityEmailVerificationBL
{
    private const string PasswordSetupPrefix = "set-password:";
    private readonly CampusXDbContext _context;
    private readonly IUniversityEmailSender _sender;
    private readonly TimeProvider _clock;
    private readonly IDataProtector _protector;
    private readonly IDataProtector _passwordSetupProtector;
    private readonly UserManager<ApplicationUser> _users;
    private readonly PasswordHasher<UniversityEmailVerification> _hasher = new();

    public UniversityEmailVerificationBL(CampusXDbContext context, IUniversityEmailSender sender,
        TimeProvider clock, IDataProtectionProvider protection, UserManager<ApplicationUser> users)
    {
        _context = context;
        _sender = sender;
        _clock = clock;
        _protector = protection.CreateProtector("CampusX.UniversityEmailVerification.CodeHash.v1");
        _passwordSetupProtector = protection.CreateProtector("CampusX.SetPassword.CodeHash.v1");
        _users = users;
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private async Task<IDbContextTransaction> BeginMutationAsync()
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Database-owned lock also serializes separate app instances. SMTP runs outside this lock.
            // A single short verification lock is sufficient for this small student project.
            await _context.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource=N'CampusX.UniversityVerification',
                    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51020, 'Verification is busy. Please try again.', 1;
                """);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private async Task<ApplicationUser> GetAccountAsync(Guid id)
    {
        var user = await _context.ApplicationUsers.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id);
        if (user is null || user.IsBlocked)
            throw new UnauthorizedAccessException("This account cannot verify a university email.");
        return user;
    }

    private Task<bool> IsCairoAsync(Guid id) => _context.UserClaims.AnyAsync(claim =>
        claim.UserId == id && claim.ClaimType == StudentUniversities.ClaimType &&
        claim.ClaimValue == StudentUniversities.Cairo);

    private async Task<bool> IsEligibleAsync(ApplicationUser user) =>
        StudentUniversities.IsCairoStudentEmail(user.UniversityEmail) && await IsCairoAsync(user.Id);

    private Task<bool> IsDuplicateAsync(Guid id, string email) =>
        _context.ApplicationUsers.AnyAsync(user => user.Id != id && user.VerifiedUniversityEmail == email);

    private Task<int> FailedAttemptsAsync(Guid id, string email, DateTime now) =>
        _context.UniversityVerificationAttempts.CountAsync(attempt => attempt.AttemptedAt > now.AddMinutes(-15) &&
            (attempt.UserId == id || attempt.Recipient == email));

    private async Task InvalidateAsync(Guid id) =>
        await _context.UniversityEmailVerifications.Where(item => item.UserId == id && item.IsActive)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, false)
                .SetProperty(item => item.CodeHash, string.Empty));

    private static bool IsConfirmed(ApplicationUser user) => user.UniversityEmailConfirmed &&
        user.VerifiedUniversityEmail == StudentUniversities.NormalizeEmail(user.UniversityEmail);

    public async Task<UniversityVerificationViewModel> GetPageAsync(Guid id)
    {
        var user = await GetAccountAsync(id);
        return await BuildPageAsync(user, await IsEligibleAsync(user), passwordSetup: false);
    }

    private async Task<UniversityVerificationViewModel> BuildPageAsync(ApplicationUser user, bool eligible, bool passwordSetup)
    {
        var id = user.Id;
        var email = StudentUniversities.NormalizeEmail(user.UniversityEmail);
        var now = Now;
        var active = await ActiveChallenge(id, email, passwordSetup).AsNoTracking().FirstOrDefaultAsync();
        var lastSend = await _context.UniversityEmailVerifications.Where(item =>
                item.UserId == id || item.Recipient == email)
            .MaxAsync(item => (DateTime?)item.CreatedAt);
        var model = new UniversityVerificationViewModel
        {
            MaskedEmail = Mask(email),
            IsEligible = eligible,
            IsConfirmed = eligible && IsConfirmed(user),
            HasActiveCode = active?.SentAt is not null && active.ExpiresAt > now,
            ExpiresAt = active?.SentAt is not null ? active.ExpiresAt : null,
            ResendAfterSeconds = lastSend.HasValue
                ? Math.Max(0, (int)Math.Ceiling((lastSend.Value.AddSeconds(60) - now).TotalSeconds)) : 0
        };
        // Quotas are based on reservations, including failed deliveries, to prevent retry abuse.
        foreach (var byRecipient in new[] { false, true })
        {
            var sends = await _context.UniversityEmailVerifications
                .Where(item => item.CreatedAt > now.AddHours(-1) &&
                    (byRecipient ? item.Recipient == email : item.UserId == id))
                .OrderBy(item => item.CreatedAt).Select(item => item.CreatedAt).ToListAsync();
            if (sends.Count >= 5)
            {
                model.ResendAfterSeconds = Math.Max(model.ResendAfterSeconds,
                    (int)Math.Ceiling((sends[sends.Count - 5].AddHours(1) - now).TotalSeconds));
                model.Notice = "The hourly send limit has been reached. Please wait before requesting another code.";
            }
        }
        if (await FailedAttemptsAsync(id, email, now) >= 5)
            model.Notice = "Too many incorrect attempts. Wait for the 15-minute attempt window before trying again.";
        else if (active?.SentAt is not null && active.ExpiresAt <= now)
            model.Notice = "Your code has expired. Request a new code.";
        if (!eligible)
            model.Notice = "Only Cairo University undergraduate emails (14 digits followed by @stud.cu.edu.eg) can be verified in this release.";
        return model;
    }

    public Task<(bool Success, string Message)> SendAsync(Guid id) => SendForOperationAsync(id, passwordSetup: false);

    private async Task<(bool Success, string Message)> SendForOperationAsync(Guid id, bool passwordSetup)
    {
        UniversityEmailVerification challenge;
        string code;
        string? passwordStamp = null;
        await using (var transaction = await BeginMutationAsync())
        {
            var user = await GetAccountAsync(id);
            if (passwordSetup)
            {
                if (!await CanSetPasswordAsync(user))
                    return (false, PasswordSetupUnavailable);
                passwordStamp = user.SecurityStamp;
            }
            else
            {
                if (!await IsEligibleAsync(user))
                    return (false, "Use a Cairo University undergraduate email: 14 digits followed by @stud.cu.edu.eg.");
                if (IsConfirmed(user))
                    return (false, "This university email is already verified.");
            }
            var email = passwordSetup ? user.VerifiedUniversityEmail! : StudentUniversities.NormalizeEmail(user.UniversityEmail);
            if (await IsDuplicateAsync(id, email))
                return (false, "This university email is already verified on another account.");
            var now = Now;
            if (await FailedAttemptsAsync(id, email, now) >= 5)
                return (false, "Too many incorrect attempts. Please wait before requesting another code.");
            var last = await _context.UniversityEmailVerifications
                .Where(item => item.UserId == id || item.Recipient == email)
                .MaxAsync(item => (DateTime?)item.CreatedAt);
            if (last.HasValue && last.Value.AddSeconds(60) > now)
                return (false, "Wait at least 60 seconds before requesting another code.");
            if (await _context.UniversityEmailVerifications.CountAsync(item =>
                    item.UserId == id && item.CreatedAt > now.AddHours(-1)) >= 5 ||
                await _context.UniversityEmailVerifications.CountAsync(item =>
                    item.Recipient == email && item.CreatedAt > now.AddHours(-1)) >= 5)
                return (false, "The hourly send limit has been reached. Please try again later.");

            await InvalidateAsync(id);
            code = RandomNumberGenerator.GetInt32(100_000_000).ToString("D8", CultureInfo.InvariantCulture);
            challenge = new UniversityEmailVerification
            {
                UserId = id, Recipient = email, CreatedAt = now, ExpiresAt = now.AddMinutes(10), IsActive = true
            };
            var hash = _hasher.HashPassword(challenge, BoundCode(challenge, code, passwordStamp));
            challenge.CodeHash = passwordSetup ? PasswordSetupPrefix + _passwordSetupProtector.Protect(hash) : _protector.Protect(hash);
            _context.UniversityEmailVerifications.Add(challenge);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        try
        {
            if (passwordSetup)
                await _sender.SendPasswordSetupCodeAsync(challenge.Recipient, code);
            else
                await _sender.SendCodeAsync(challenge.Recipient, code);
        }
        catch (Exception exception) when (exception is SmtpException or OperationCanceledException or
                                          InvalidOperationException or FormatException)
        {
            await using var failed = await BeginMutationAsync();
            await _context.UniversityEmailVerifications.Where(item => item.Id == challenge.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, false)
                    .SetProperty(item => item.CodeHash, string.Empty));
            await failed.CommitAsync();
            // Never log SMTP exceptions, credentials, message contents or codes.
            return (false, "The email could not be sent. Please wait for the cooldown and try again.");
        }

        await using var finalize = await BeginMutationAsync();
        var current = await GetAccountAsync(id);
        await _context.Entry(challenge).ReloadAsync();
        var eligible = passwordSetup
            ? current.SecurityStamp == passwordStamp && await CanSetPasswordAsync(current)
            : await IsEligibleAsync(current);
        if (!challenge.IsActive || challenge.ExpiresAt <= Now ||
            StudentUniversities.NormalizeEmail(current.UniversityEmail) != challenge.Recipient ||
            !eligible)
        {
            challenge.IsActive = false;
            challenge.CodeHash = string.Empty;
            await _context.SaveChangesAsync();
            await finalize.CommitAsync();
            return (false, "This code request is no longer active. Please request a new code.");
        }
        challenge.SentAt = Now;
        await _context.SaveChangesAsync();
        await finalize.CommitAsync();
        return (true, "A code was sent to your university email. It expires in 10 minutes. Check your inbox and spam folder.");
    }

    public async Task<(bool Success, string Message)> VerifyAsync(Guid id, string? code)
    {
        await using var transaction = await BeginMutationAsync();
        var user = await GetAccountAsync(id);
        if (!await IsEligibleAsync(user))
            return (false, "This university email is not eligible for verification.");
        if (IsConfirmed(user))
            return (false, "This code has already been used; your university email is verified.");
        var email = StudentUniversities.NormalizeEmail(user.UniversityEmail);
        var (challenge, error) = await CheckCodeAsync(id, email, code);
        if (challenge is null)
        {
            await transaction.CommitAsync();
            return (false, error!);
        }
        if (await IsDuplicateAsync(id, email))
            return (false, "This university email is already verified on another account.");
        try
        {
            var updated = await _context.ApplicationUsers.Where(item => item.Id == id && !item.IsBlocked &&
                    item.UniversityEmail == user.UniversityEmail)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.UniversityEmail, email)
                    .SetProperty(item => item.VerifiedUniversityEmail, email)
                    .SetProperty(item => item.UniversityEmailConfirmed, true));
            if (updated != 1)
                return (false, "Your account or university email changed. Request a new code.");
            Consume(challenge);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, "University email verified. You can now access the marketplace.");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return (false, "This university email is already verified on another account.");
        }
    }

    public async Task<(bool Success, string Message)> ChangeEmailAsync(Guid id, string? email)
    {
        if (!StudentUniversities.IsCairoStudentEmail(email))
            return (false, "Enter a Cairo University undergraduate email: 14 digits followed by @stud.cu.edu.eg.");
        await using var transaction = await BeginMutationAsync();
        var user = await GetAccountAsync(id);
        var normalized = StudentUniversities.NormalizeEmail(email);
        if (await IsDuplicateAsync(id, normalized))
            return (false, "This university email is already verified on another account.");
        if (normalized == StudentUniversities.NormalizeEmail(user.UniversityEmail) && await IsCairoAsync(id))
            return (false, "The university email has not changed.");
        await InvalidateAsync(id);
        await _context.ApplicationUsers.Where(item => item.Id == id && !item.IsBlocked)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.UniversityEmail, normalized)
                .SetProperty(item => item.UniversityEmailConfirmed, false)
                .SetProperty(item => item.VerifiedUniversityEmail, (string?)null));
        await _context.UserClaims.Where(claim => claim.UserId == id && claim.ClaimType == StudentUniversities.ClaimType)
            .ExecuteDeleteAsync();
        _context.UserClaims.Add(new IdentityUserClaim<Guid>
        {
            UserId = id, ClaimType = StudentUniversities.ClaimType, ClaimValue = StudentUniversities.Cairo
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (true, "University email updated. Verify the new address before accessing the marketplace.");
    }

    private IQueryable<UniversityEmailVerification> ActiveChallenge(Guid id, string email, bool passwordSetup) =>
        _context.UniversityEmailVerifications.Where(item => item.UserId == id && item.IsActive && item.Recipient == email &&
            item.CodeHash.StartsWith(PasswordSetupPrefix) == passwordSetup);

    private async Task<(UniversityEmailVerification? Challenge, string? Error)> CheckCodeAsync(
        Guid id, string email, string? code, string? passwordStamp = null)
    {
        var now = Now;
        if (await FailedAttemptsAsync(id, email, now) >= 5)
            return (null, "Too many incorrect attempts. Wait for the 15-minute attempt window before trying again.");

        var challenge = await ActiveChallenge(id, email, passwordStamp is not null).SingleOrDefaultAsync();
        var expired = challenge is not null && challenge.ExpiresAt <= now;
        if (challenge?.SentAt is not null && !expired && code is { Length: 8 } && code.All(char.IsAsciiDigit))
        {
            try
            {
                var hash = passwordStamp is null ? _protector.Unprotect(challenge.CodeHash)
                    : _passwordSetupProtector.Unprotect(challenge.CodeHash[PasswordSetupPrefix.Length..]);
                if (_hasher.VerifyHashedPassword(challenge, hash, BoundCode(challenge, code, passwordStamp))
                    != PasswordVerificationResult.Failed)
                    return (challenge, null);
            }
            catch (Exception exception) when (exception is CryptographicException or FormatException)
            {
                // Unreadable challenges fail closed and count toward the same attempt limit.
            }
        }

        _context.UniversityVerificationAttempts.Add(new UniversityVerificationAttempt
        {
            UserId = id, Recipient = email, AttemptedAt = now
        });
        if (expired)
        {
            challenge!.IsActive = false;
            challenge.CodeHash = string.Empty;
        }
        await _context.SaveChangesAsync();
        return (null, expired ? "Your code has expired. Request a new code." : "The code is incorrect or no longer active.");
    }

    private void Consume(UniversityEmailVerification challenge)
    {
        challenge.ConsumedAt = Now;
        challenge.IsActive = false;
        challenge.CodeHash = string.Empty;
    }

    private static string BoundCode(UniversityEmailVerification item, string code, string? passwordStamp)
    {
        var binding = $"{item.Id:N}:{item.UserId:N}:{item.Recipient}:{code}";
        return passwordStamp is null ? binding : $"{PasswordSetupPrefix}{binding}:{passwordStamp}";
    }

    private static string Mask(string email)
    {
        var at = email.LastIndexOf('@');
        return at > 3 ? $"{email[..2]}••••{email[(at - 2)..]}" : "University email not set";
    }
}


using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using CampusX.Data.DbContexts;
using CampusX.Enums;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Services;

public sealed class DevelopmentTesterAccess(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    IHttpContextAccessor httpContext,
    IServer server,
    ILookupNormalizer normalizer,
    CampusXDbContext db)
{
    internal const string ClaimType = "campusx:development-tester";
    internal const string ClaimValue = "true";
    private const string Section = "DevelopmentTesterAccess:";

    internal bool SharedMode => IsSharedMode(environment, configuration);

    internal static bool IsEnabled(IWebHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>(Section + "Enabled");

    internal static bool IsSharedProfile(IWebHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>(Section + "TeamTestProfile");

    internal static bool IsSharedMode(IWebHostEnvironment environment, IConfiguration configuration) =>
        IsSharedProfile(environment, configuration) && configuration.GetValue<bool>(Section + "AllowSharedDatabase");

    internal static bool AllowsSharedProvisioning(IConfiguration configuration) =>
        configuration.GetValue<bool>(Section + "AllowSharedProvisioning");

    internal static bool UsesLocalDatabase(IConfiguration configuration) =>
        ParseConnection(configuration.GetConnectionString("CampusXDb")) is { } connection && IsLocal(connection);

    internal static bool UsesApprovedSharedDatabase(IConfiguration configuration, string? connectionString)
    {
        if (!configuration.GetValue<bool>("DatabaseStartup:UseSharedDatabase") ||
            ParseConnection(connectionString) is not { } connection ||
            !string.Equals(connection.DataSource, configuration[Section + "SharedServer"]?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(connection.InitialCatalog, configuration[Section + "SharedDatabase"]?.Trim(), StringComparison.Ordinal))
            return false;

        // Local shared memory/LocalDB can rehearse this mode. A network connection must validate SQL Server TLS.
        return IsLocal(connection) || (connection.Encrypt != SqlConnectionEncryptOption.Optional &&
            !connection.TrustServerCertificate && string.IsNullOrEmpty(connection.HostNameInCertificate) &&
            string.IsNullOrEmpty(connection.ServerCertificate) && connection.ApplicationIntent == ApplicationIntent.ReadWrite);
    }

    internal static string? GetSharedClientFingerprint(IConfiguration configuration)
    {
        var fingerprint = configuration[Section + "ClientCertificateSha256"]?.Trim();
        return fingerprint is { Length: 64 } && fingerprint.All(Uri.IsHexDigit) ? fingerprint : null;
    }

    internal static bool MatchesSharedClient(X509Certificate2? certificate, string? fingerprint)
    {
        var now = DateTime.UtcNow;
        return fingerprint is not null && certificate is not null &&
            certificate.NotBefore.ToUniversalTime() <= now && certificate.NotAfter.ToUniversalTime() >= now &&
            certificate.GetCertHashString(HashAlgorithmName.SHA256).Equals(fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    private static SqlConnectionStringBuilder? ParseConnection(string? connectionString)
    {
        try
        {
            var connection = new SqlConnectionStringBuilder(connectionString);
            return string.IsNullOrWhiteSpace(connection.DataSource) || string.IsNullOrWhiteSpace(connection.InitialCatalog) ||
                connection.FailoverPartner.Length != 0 || connection.AttachDBFilename.Length != 0 ? null : connection;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool IsLocal(SqlConnectionStringBuilder connection)
    {
        // Accept explicitly local transports so network aliases cannot select a shared server.
        var source = connection.DataSource.Split('\\');
        return (source[0].Equals("lpc:.", StringComparison.OrdinalIgnoreCase) && source.Length <= 2)
            || (source[0].Equals("(localdb)", StringComparison.OrdinalIgnoreCase)
                && source.Length == 2 && source[1].Length > 0);
    }

    internal bool IsAllowlisted(string? email)
    {
        var normalized = normalizer.NormalizeEmail(email?.Trim());
        return !string.IsNullOrEmpty(normalized) && configuration.GetSection(Section + "AllowedEmails").GetChildren()
            .Any(entry => normalizer.NormalizeEmail(entry.Value?.Trim()) == normalized);
    }

    internal bool IsAllowed(Guid id, UserRole role, string? email) =>
        httpContext.HttpContext?.User is { Identity.IsAuthenticated: true } user &&
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) && actor == id &&
        CanUseLocalAccess(role, email) && Enrollment(id).Any();

    internal async Task<bool> IsAllowedAsync(Guid id, UserRole role, string? email) =>
        CanUseLocalAccess(role, email) && await Enrollment(id).AnyAsync();

    private bool CanUseLocalAccess(UserRole role, string? email)
    {
        var sharedProfile = IsSharedProfile(environment, configuration);
        var enabled = sharedProfile ? SharedMode : IsEnabled(environment, configuration);
        if (role != UserRole.Student || !enabled || !IsAllowlisted(email))
            return false;
        if (httpContext.HttpContext is not { } context || !IsLoopback(context.Connection.LocalIpAddress) ||
            !IsLoopback(context.Connection.RemoteIpAddress) || !IsLoopbackHost(context.Request.Host.Host))
            return false;

        // Forwarded headers cannot establish a local request. All listener addresses must also be local.
        if (context.Request.Headers.Keys.Any(name => name.Equals("Forwarded", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("X-Original-", StringComparison.OrdinalIgnoreCase)))
            return false;

        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is not { Count: > 0 } || !addresses.All(address =>
            Uri.TryCreate(address, UriKind.Absolute, out var uri) && IsLoopbackHost(uri.Host)))
            return false;

        if (!sharedProfile)
            return UsesLocalDatabase(configuration);

        // Require the actual TLS client certificate; this app does not accept certificates from proxy headers.
        return context.Request.IsHttps &&
            MatchesSharedClient(context.Connection.ClientCertificate, GetSharedClientFingerprint(configuration)) &&
            UsesApprovedSharedDatabase(configuration, db.Database.GetConnectionString());
    }

    private IQueryable<IdentityUserClaim<Guid>> Enrollment(Guid id) => db.UserClaims.Where(claim =>
        claim.UserId == id && claim.ClaimType == ClaimType && claim.ClaimValue == ClaimValue);

    private static bool IsLoopbackHost(string host) => host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IsLoopback(address));

    private static bool IsLoopback(IPAddress? address) => address is not null &&
        IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);
}

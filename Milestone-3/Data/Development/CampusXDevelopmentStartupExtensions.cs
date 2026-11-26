using CampusX.Data.DbContexts;
using CampusX.Data.Seed;
using CampusX.Services;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Data.Development;

internal static class CampusXDevelopmentStartupExtensions
{
    internal static (bool SharedMode, string? ConnectionString) ConfigureCampusXTeamTester(
        this WebApplicationBuilder builder, bool isProvisioning)
    {
        var sharedTesterProfile = DevelopmentTesterAccess.IsSharedProfile(builder.Environment, builder.Configuration);
        var sharedTesterMode = DevelopmentTesterAccess.IsSharedMode(builder.Environment, builder.Configuration);
        var sharedTesterConnection = sharedTesterMode ? builder.Configuration.GetConnectionString("CampusXDb") : null;
        if (sharedTesterProfile && !isProvisioning)
        {
            if (!sharedTesterMode)
                throw new InvalidOperationException("Enable the Team Test profile with the private DevelopmentTesterAccess:AllowSharedDatabase setting.");
            if (!DevelopmentTesterAccess.UsesApprovedSharedDatabase(builder.Configuration, sharedTesterConnection))
                throw new InvalidOperationException("Team tester mode requires the approved SQL target and shared startup mode. Network SQL must validate the server certificate.");

            var fingerprint = DevelopmentTesterAccess.GetSharedClientFingerprint(builder.Configuration)
                ?? throw new InvalidOperationException("Team tester mode requires the local client certificate SHA-256 fingerprint.");
            builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(7148, listener => listener.UseHttps(https =>
            {
                https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                https.ClientCertificateValidation = (certificate, _, _) => DevelopmentTesterAccess.MatchesSharedClient(certificate, fingerprint);
            })));
        }

        return (sharedTesterMode, sharedTesterConnection);
    }

    internal static void InitializeCampusXDevelopmentDatabase(this WebApplication app, bool sharedTesterMode)
    {
        if (!sharedTesterMode && DevelopmentTesterAccess.IsEnabled(app.Environment, app.Configuration) &&
            !DevelopmentTesterAccess.UsesLocalDatabase(app.Configuration))
            throw new InvalidOperationException("Development tester access requires a local SQL Server connection.");

        // Shared team databases must be migrated separately and must not receive development seed data.
        var useSharedDatabase = app.Configuration.GetValue<bool>("DatabaseStartup:UseSharedDatabase");
        if (app.Environment.IsDevelopment() && !useSharedDatabase && !sharedTesterMode)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CampusXDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                db.Database.Migrate();
                SampleProductSeeder.Seed(db);
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception,
                    "CampusX database initialization failed. Check that SQL Server is running and the CampusXDb connection string is valid.");
                throw new InvalidOperationException(
                    "CampusX could not initialize its Development database. Verify SQL Server and the CampusXDb connection string.",
                    exception);
            }

            DevelopmentTestDataSeeder.TrySeed(
                db,
                app.Configuration,
                app.Environment,
                scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
        }
    }
}

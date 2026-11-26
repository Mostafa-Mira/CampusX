using CampusX.Data.Repositories;
using CampusX.Models;
using CampusX.Options;
using System.Threading.RateLimiting;

namespace CampusX.Services;

internal static class CampusXServiceCollectionExtensions
{
    internal static void AddCampusXBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ApplicationUserBL>();
        services.AddScoped<CategoryBL>();
        services.AddScoped<ProductBL>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ReportBL>();
        services.AddScoped<AdminDashboardBL>();
        services.AddScoped<ContactInquiryBL>();

        services.Configure<EmailSmtpOptions>(configuration.GetSection(EmailSmtpOptions.SectionName));
        services.AddScoped<GmailUniversityEmailSender>();
        services.AddScoped<IUniversityEmailSender>(services => services.GetRequiredService<GmailUniversityEmailSender>());
        services.AddScoped<IPasswordRecoveryEmailSender>(services => services.GetRequiredService<GmailUniversityEmailSender>());
        services.AddScoped<PasswordRecoveryBL>();
        services.AddSingleton<PasswordRecoveryQueue>();
        services.AddHostedService(services => services.GetRequiredService<PasswordRecoveryQueue>());
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<UniversityEmailVerificationBL>();

        services.Configure<RevenueOptions>(configuration.GetSection(RevenueOptions.SectionName));
        services.AddScoped<IPaymentProvider, SimulatedPaymentProvider>();
        services.AddScoped<RevenueBL>();
    }

    internal static void AddCampusXRecoveryRateLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(wait.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                await context.HttpContext.Response.WriteAsync("Too many recovery attempts. Wait a few minutes and try again.", cancellationToken);
            };
            foreach (var (policy, limit) in new[] { (PasswordRecoveryQueue.RequestPolicy, 10), (PasswordRecoveryQueue.ResetPolicy, 15) })
            {
                options.AddPolicy(policy, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0
                    }));
            }
        });
    }
}

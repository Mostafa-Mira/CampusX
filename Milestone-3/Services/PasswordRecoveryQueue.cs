using System.Threading.Channels;
using System.Threading.RateLimiting;
using CampusX.Models;

namespace CampusX.Services;

public sealed class PasswordRecoveryQueue(IServiceScopeFactory scopes, ILogger<PasswordRecoveryQueue> logger) : BackgroundService
{
    public const string RequestPolicy = "PasswordRecoveryRequest";
    public const string ResetPolicy = "PasswordRecoveryReset";

    private readonly Channel<(string Email, string ResetPath)> _requests =
        Channel.CreateBounded<(string, string)>(new BoundedChannelOptions(16) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly PartitionedRateLimiter<string> _recipientLimits =
        PartitionedRateLimiter.Create<string, string>(recipient =>
            RateLimitPartition.GetFixedWindowLimiter(recipient, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    public void Enqueue(string email, string resetPath)
    {
        using var lease = _recipientLimits.AttemptAcquire(email.Normalize().ToUpperInvariant());
        if (lease.IsAcquired)
            _requests.Writer.TryWrite((email, resetPath));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in _requests.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PasswordRecoveryBL>().SendLinkAsync(request.Email, request.ResetPath);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                // The exception may carry an address, SMTP details, or a reset link.
                logger.LogWarning("A CampusX password recovery request could not be delivered.");
            }
        }
    }

    public override void Dispose()
    {
        _recipientLimits.Dispose();
        base.Dispose();
    }
}

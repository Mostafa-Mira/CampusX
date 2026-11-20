using System.Net;
using System.Net.Mail;
using CampusX.Options;
using Microsoft.Extensions.Options;

namespace CampusX.Services;

public interface IUniversityEmailSender
{
    Task SendCodeAsync(string recipient, string code);
    Task SendPasswordSetupCodeAsync(string recipient, string code);
}

public interface IPasswordRecoveryEmailSender
{
    Task SendResetLinkAsync(string recipient, Uri link);
    Task SendUniversityProofAsync(string recipient, string code);
}

public sealed class GmailUniversityEmailSender : IUniversityEmailSender, IPasswordRecoveryEmailSender
{
    private readonly EmailSmtpOptions _options;
    public GmailUniversityEmailSender(IOptions<EmailSmtpOptions> options) => _options = options.Value;

    public Task SendCodeAsync(string recipient, string code) => SendAsync(recipient, "Your CampusX university verification code",
        $"Your CampusX verification code is: {code}\n\nIt expires in 10 minutes and can be used once. Never share this code.\nIf you did not request it, ignore this email.");

    public Task SendPasswordSetupCodeAsync(string recipient, string code) => SendAsync(recipient, "Your CampusX password setup code",
        $"Use this code on the CampusX Set Password page: {code}\n\nIt expires in 10 minutes and can be used once. Never share this code.\nIf you did not request it, ignore this email.");

    public Task SendResetLinkAsync(string recipient, Uri link) => SendAsync(recipient, "Reset your CampusX password",
        $"Open the link below to set a new CampusX password.\n\n{link.AbsoluteUri}\n\nThe link expires in 30 minutes and can be used once. If you did not request a reset, ignore this email.");

    public Task SendUniversityProofAsync(string recipient, string code) => SendAsync(recipient, "Confirm your CampusX password reset",
        $"Use this confirmation code on the CampusX password reset page.\n\n{code}\n\nThe reset link goes to your login email. Never share this code. If you did not request a reset, ignore this email.");

    private async Task SendAsync(string recipient, string subject, string body)
    {
        if (!string.Equals(_options.Host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase) ||
            _options.Port != 587 || string.IsNullOrWhiteSpace(_options.UserName) ||
            string.IsNullOrWhiteSpace(_options.AppPassword) || string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("CampusX email delivery is not configured.");

        using var message = new MailMessage();
        message.From = new MailAddress(_options.FromEmail, "CampusX");
        message.To.Add(new MailAddress(recipient));
        message.Subject = subject;
        message.Body = body;
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true, // STARTTLS is mandatory; no plaintext fallback or certificate override.
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.UserName, _options.AppPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await client.SendMailAsync(message, timeout.Token);
    }
}

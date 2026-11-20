using System.Net;
using System.Net.Mail;
using CampusX.Options;
using Microsoft.Extensions.Options;

namespace CampusX.Services;

public interface IUniversityEmailSender
{
    Task SendCodeAsync(string recipient, string code);
}

public sealed class GmailUniversityEmailSender : IUniversityEmailSender
{
    private readonly EmailSmtpOptions _options;
    public GmailUniversityEmailSender(IOptions<EmailSmtpOptions> options) => _options = options.Value;

    public async Task SendCodeAsync(string recipient, string code)
    {
        if (!string.Equals(_options.Host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase) ||
            _options.Port != 587 || string.IsNullOrWhiteSpace(_options.UserName) ||
            string.IsNullOrWhiteSpace(_options.AppPassword) || string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("University email delivery is not configured. Please try again later.");

        using var message = new MailMessage();
        message.From = new MailAddress(_options.FromEmail, "CampusX");
        message.To.Add(new MailAddress(recipient));
        message.Subject = "Your CampusX university verification code";
        message.Body = $"Your CampusX verification code is: {code}\n\nIt expires in 10 minutes and can be used once. Never share this code.\nIf you did not request it, ignore this email.";
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

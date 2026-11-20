namespace CampusX.Options;

public class EmailSmtpOptions
{
    public const string SectionName = "EmailSmtp";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}

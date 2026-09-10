namespace RetroBackend.Config;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool EnableSsl { get; set; }
    public string From { get; set; } = "noreply@remolon.local";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
    public string Provider { get; set; } = "smtp";
    public string? ApiKey { get; set; }
}

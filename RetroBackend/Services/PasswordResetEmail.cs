namespace RetroBackend.Services;

public static class PasswordResetEmail
{
    public const string Subject = "Reset your ReMolon password";

    public static string BuildResetUrl(string frontendBaseUrl, string email, string encodedToken, bool invite = false)
    {
        var baseUrl = frontendBaseUrl.TrimEnd('/');
        var fragment = $"email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";
        if (invite)
            fragment += "&purpose=invite";
        return $"{baseUrl}/reset-password#{fragment}";
    }

    public static string HtmlBody(string resetUrl)
    {
        var safeUrl = System.Net.WebUtility.HtmlEncode(resetUrl);
        return $"""
            <p>We received a request to reset your ReMolon password.</p>
            <p>This link is valid for 30 minutes:</p>
            <p><a href="{safeUrl}">{safeUrl}</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """;
    }
}

namespace RetroBackend.Services;

public static class UserInvitationEmail
{
    public static string Subject => "Your ReMolon account";

    public static string HtmlBody(string email, string setPasswordUrl)
    {
        var safeEmail = System.Net.WebUtility.HtmlEncode(email);
        var safeUrl = System.Net.WebUtility.HtmlEncode(setPasswordUrl);
        return $"""
            <p>An account has been created for you on ReMolon.</p>
            <p><strong>Email:</strong> {safeEmail}</p>
            <p>Choose a password using this link (valid for 30 days):</p>
            <p><a href="{safeUrl}">{safeUrl}</a></p>
            <p>You cannot sign in until you set a password. If the link expires, ask a manager to send a new invitation.</p>
            """;
    }
}

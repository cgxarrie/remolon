namespace RetroBackend.Services;

public static class UserInvitationEmail
{
    public static string Subject => "Your ReMolon account";

    public static string HtmlBody(string email, string temporaryPassword, string loginUrl) =>
        $"""
        <p>An account has been created for you on ReMolon.</p>
        <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(email)}</p>
        <p><strong>Temporary password:</strong> {System.Net.WebUtility.HtmlEncode(temporaryPassword)}</p>
        <p>Sign in here: <a href="{System.Net.WebUtility.HtmlEncode(loginUrl)}">{System.Net.WebUtility.HtmlEncode(loginUrl)}</a></p>
        <p>You will be asked to choose a new password on first login.</p>
        """;
}

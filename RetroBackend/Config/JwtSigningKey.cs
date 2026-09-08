namespace RetroBackend.Config;

public static class JwtSigningKey
{
    public const string Placeholder = "CHANGE_THIS_SECRET_KEY_MIN_32_CHARS_LONG_!!";
    public const int MinimumLength = 32;

    public static string Resolve(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing. Set Jwt__Key (or Jwt:Key) to a secret at least "
                + $"{MinimumLength} characters long. See .env.example.");
        }

        if (key.Length < MinimumLength)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {MinimumLength} characters long.");
        }

        if (string.Equals(key, Placeholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Jwt:Key is the committed placeholder and must be replaced. "
                + "That value is burned; do not use it on any deploy.");
        }

        return key;
    }
}

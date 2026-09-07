using RetroBackend.Models;

namespace RetroBackend.Services;

public static class AvatarImage
{
    public const int MaxBytes = 2 * 1024 * 1024;

    public static bool TryValidate(string? contentType, Stream content, long length, out string normalizedContentType)
    {
        normalizedContentType = string.Empty;
        if (length <= 0 || length > MaxBytes)
            return false;

        normalizedContentType = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedContentType is "image/jpg")
            normalizedContentType = "image/jpeg";

        if (normalizedContentType is not ("image/jpeg" or "image/png" or "image/webp"))
            return false;

        Span<byte> header = stackalloc byte[12];
        var read = content.Read(header);
        if (content.CanSeek)
            content.Position = 0;

        return normalizedContentType switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8
                && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            "image/webp" => read >= 12
                && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P',
            _ => false,
        };
    }

    public static string? UrlFor(AppUser user) =>
        string.IsNullOrWhiteSpace(user.AvatarContentType) ? null : $"/api/users/{user.Id}/avatar";
}

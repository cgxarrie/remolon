namespace RetroBackend.Models;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ThemeKey { get; set; } = "default";
    public string? ThemeHeaderColor { get; set; }
    public string? ThemeHeaderHoverColor { get; set; }
    public string? ThemeAccentColor { get; set; }
    public string? ThemeAccentHoverColor { get; set; }
    public string? ThemeFocusColor { get; set; }
    public List<AppUser> Users { get; set; } = [];
    public List<Retrospective> Retrospectives { get; set; } = [];
}

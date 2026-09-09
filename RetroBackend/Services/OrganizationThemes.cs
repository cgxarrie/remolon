namespace RetroBackend.Services;

/// <summary>
/// Theme keys an organization may store. The presets mirror themePresets in the frontend's
/// theme.tsx, which owns the colors; only "custom" carries colors in the database.
/// </summary>
public static class OrganizationThemes
{
    public const string Custom = "custom";
    public const string Default = "default";

    public static readonly IReadOnlySet<string> Presets = new HashSet<string>
    {
        Default,
        "azure",
        "ocean",
        "lagoon",
        "teal",
        "emerald",
        "forest",
        "meadow",
        "marigold",
        "amber",
        "tangerine",
        "sunset",
        "ember",
        "crimson",
        "rose",
        "blossom",
        "orchid",
        "plum",
        "violet",
        "aurora",
        "midnight",
        "slate",
        "graphite",
        "mocha",
    };

    public static bool IsKnown(string themeKey) => themeKey == Custom || Presets.Contains(themeKey);
}

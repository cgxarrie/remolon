namespace RetroBackend.Services;

public static class ThrowableObjects
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "axe",
        "hammer",
        "sword",
        "brick",
        "tomato",
        "shit",
    };
}

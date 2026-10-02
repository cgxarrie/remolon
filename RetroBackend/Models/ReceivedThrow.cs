namespace RetroBackend.Models;

/// <summary>
/// How many times a user has been hit by one kind of throwable object while none of their browsers were viewing that retrospective.
/// </summary>
public class ReceivedThrow
{
    public string UserId { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastThrownAt { get; set; }

    public AppUser User { get; set; } = null!;
}

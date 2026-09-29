namespace RetroBackend.Models;

/// <summary>
/// How many times a user has been hit by one kind of throwable object since they last cleared the tally.
/// </summary>
public class ReceivedThrow
{
    public string UserId { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastThrownAt { get; set; }

    public AppUser User { get; set; } = null!;
}

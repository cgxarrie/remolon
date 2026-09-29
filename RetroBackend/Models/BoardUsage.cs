namespace RetroBackend.Models;

/// <summary>
/// How often a user opens a board (all sessions that share a title) in one organization.
/// </summary>
public class BoardUsage
{
    public string UserId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int UseCount { get; set; }
    public DateTime LastUsedAt { get; set; }

    public AppUser User { get; set; } = null!;
}

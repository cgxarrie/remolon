namespace RetroBackend.Models;

public class UserRetrospective
{
    public string UserId { get; set; } = string.Empty;
    public Guid RetrospectiveId { get; set; }

    public AppUser User { get; set; } = null!;
    public Retrospective Retrospective { get; set; } = null!;
}

namespace RetroBackend.Models;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<AppUser> Users { get; set; } = [];
    public List<Retrospective> Retrospectives { get; set; } = [];
}

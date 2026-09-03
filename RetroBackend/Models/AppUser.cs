using Microsoft.AspNetCore.Identity;

namespace RetroBackend.Models;

public class AppUser : IdentityUser
{
    public AppUser() { }

    public AppUser(string email, string nickname) : base(email)
    {
        UserName = email;
        Email = email;
        Nickname = nickname;
    }
    // UserName is set to Email when registering
    public string Nickname { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}

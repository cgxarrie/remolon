using System.ComponentModel.DataAnnotations;
using RetroBackend.Auth;

namespace RetroBackend.Dtos;

public record UserSummaryDto(string Id, string Email, string Nickname, string Role);

public record UpdateUserRoleRequest(
    [Required]
    [RegularExpression($"^({Roles.Admin}|{Roles.Manager}|{Roles.StandardUser})$",
        ErrorMessage = "Role must be Admin, Manager, or StandardUser.")]
    string Role
);

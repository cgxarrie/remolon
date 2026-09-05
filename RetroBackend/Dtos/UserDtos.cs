using System.ComponentModel.DataAnnotations;
using RetroBackend.Auth;

namespace RetroBackend.Dtos;

public record UserSummaryDto(
    string Id,
    string Email,
    string Nickname,
    string Role,
    Guid? OrganizationId,
    string? OrganizationName
);

public record CreateUserRequest(
    [Required, EmailAddress] string Email,
    string? Nickname,
    string? Role,
    Guid? OrganizationId
);

public record CreateUserResponse(
    string Id,
    string Email,
    string Nickname,
    string Role,
    bool InvitationEmailSent,
    string? TemporaryPassword
);

public record UpdateUserRoleRequest(
    [Required]
    [RegularExpression($"^({Roles.Manager}|{Roles.StandardUser})$",
        ErrorMessage = "Role must be Manager or StandardUser.")]
    string Role
);


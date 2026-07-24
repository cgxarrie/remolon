using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(50)] string Nickname
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthTokenResponse(string Token, string Email, string Role, string Nickname);

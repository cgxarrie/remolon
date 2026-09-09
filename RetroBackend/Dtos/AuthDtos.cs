using System.ComponentModel.DataAnnotations;
using RetroBackend.Auth;

namespace RetroBackend.Dtos;

public record PublicRegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(IdentityPasswordPolicy.RequiredLength)] string Password,
    [MaxLength(50)] string? Nickname,
    [Required, MaxLength(200)] string OrganizationName
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email
);

public record ForgotPasswordResponse(string Message);

public record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required] string Token,
    [Required, MinLength(IdentityPasswordPolicy.RequiredLength)] string NewPassword
);

public record AuthTokenResponse(
    string Token,
    string Email,
    string Role,
    string Nickname,
    string? OrganizationName,
    string? RefreshToken = null);

public record RefreshTokenRequest([Required] string RefreshToken);

public record InitialPasswordChangeRequest(
    [Required, EmailAddress] string Email,
    [Required] string CurrentPassword,
    [Required, MinLength(IdentityPasswordPolicy.RequiredLength)] string NewPassword
);

public record PasswordChangeRequiredResponse(string Message, bool RequiresPasswordChange);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(IdentityPasswordPolicy.RequiredLength)] string NewPassword
);

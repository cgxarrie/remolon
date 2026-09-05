using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;

public record PublicRegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [MaxLength(50)] string? Nickname,
    [Required, MaxLength(200)] string OrganizationName
);

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(50)] string Nickname,
    Guid? OrganizationId
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
    [Required, MinLength(8)] string NewPassword
);

public record AuthTokenResponse(string Token, string Email, string Role, string Nickname, string? OrganizationName);

public record InitialPasswordChangeRequest(
    [Required, EmailAddress] string Email,
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword
);

public record PasswordChangeRequiredResponse(string Message, bool RequiresPasswordChange);

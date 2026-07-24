using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;

public record AssignUserRequest(
    [Required, EmailAddress] string UserEmail,
    [Required] Guid RetrospectiveId
);

using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;

public record AssignUserRequest(
    [Required, EmailAddress] string UserEmail,
    [Required] Guid RetrospectiveId
);

public record BatchAssignUsersRequest(
    [Required] Guid RetrospectiveId,
    [Required] List<string> UserIds
);

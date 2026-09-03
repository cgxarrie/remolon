using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;

public record OrganizationDto(Guid Id, string Name);

public record SaveOrganizationRequest(
    [Required, MaxLength(200)] string Name
);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
);

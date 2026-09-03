using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;

public record OrganizationThemeDto(
    string ThemeKey,
    string? HeaderColor,
    string? HeaderHoverColor,
    string? AccentColor,
    string? AccentHoverColor,
    string? FocusColor
);

public record SaveOrganizationRequest(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(20)] string ThemeKey = "default",
    [MaxLength(7)] string? HeaderColor = null,
    [MaxLength(7)] string? HeaderHoverColor = null,
    [MaxLength(7)] string? AccentColor = null,
    [MaxLength(7)] string? AccentHoverColor = null,
    [MaxLength(7)] string? FocusColor = null
);

public record OrganizationDto(Guid Id, string Name, OrganizationThemeDto Theme);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
);

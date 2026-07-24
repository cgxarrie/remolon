using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;

public class CloseRetrospectiveRequest
{
    // Reserved for future auth integration.
    public string CurrentUser { get; set; } = string.Empty;
}

namespace RetroBackend.Mappings;

public static class CloseRetrospectiveDtoMappings
{
    public static Models.CloseRetrospectiveRequest ToServiceRequest(this Dtos.CloseRetrospectiveRequest dto) => new()
    {
        CurrentUser = dto.CurrentUser,
    };
}

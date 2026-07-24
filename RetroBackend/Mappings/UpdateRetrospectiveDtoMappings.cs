namespace RetroBackend.Mappings;

public static class UpdateRetrospectiveDtoMappings
{
    public static Models.UpdateRetrospectiveRequest ToServiceRequest(this Dtos.UpdateRetrospectiveRequest dto) => new()
    {
        Title = dto.Title,
        AddColumns = dto.AddColumns?.Select(c => new Models.UpdateRetrospectiveRequestAddColumn
        {
            Title = c.Title,
            Position = c.Position,
        }).ToList(),
        UpdateColumns = dto.UpdateColumns?.Select(c => new Models.UpdateRetrospectiveRequestUpdateColumn
        {
            Id = c.Id,
            Title = c.Title,
            Position = c.Position,
            HeaderColor = c.HeaderColor,
        }).ToList(),
        RemoveColumnIds = dto.RemoveColumnIds,
        RetrospectiveDate = dto.RetrospectiveDate,
    };
}

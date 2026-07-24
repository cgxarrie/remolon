namespace RetroBackend.Mappings;

public static class GetRetrospectiveMappings
{
    public static Dtos.GetColumnDto ToDto(this Models.Column column) => new()
    {
        Id = column.Id,
        Title = column.Title,
        Position = column.Position,
        HeaderColor = column.HeaderColor,
        Items = column.Items.Where(i => i is not Models.ActionItem).Select(i => i.ToDto()).ToList(),
    };

    public static Dtos.GetActionColumnDto ToDto(this Models.ActionColumn column) => new()
    {
        Id = column.Id,
        Title = column.Title,
        Position = column.Position,
        Items = column.Items.OfType<Models.ActionItem>().Select(i => i.ToDto()).ToList(),
    };

    public static Dtos.GetRetrospectiveSummaryDto ToSummaryDto(this Models.Retrospective retro) => new()
    {
        Id = retro.Id,
        Title = retro.Title,
        IsClosed = retro.IsClosed,
        RetrospectiveDate = retro.RetrospectiveDate,
        CreatedAt = retro.CreatedAt,
        UpdatedAt = retro.UpdatedAt,
    };

    public static Dtos.GetRetrospectiveDto ToGetDto(this Models.Retrospective retro) => new()
    {
        Id = retro.Id,
        Title = retro.Title,
        IsClosed = retro.IsClosed,
        RetrospectiveDate = retro.RetrospectiveDate,
        Columns = retro.Columns.Where(c => c is not Models.ActionColumn).Select(c => c.ToDto()).ToList(),
        ActionColumns = retro.Columns.OfType<Models.ActionColumn>().Select(c => c.ToDto()).ToList(),
        CreatedAt = retro.CreatedAt,
        UpdatedAt = retro.UpdatedAt,
    };
}

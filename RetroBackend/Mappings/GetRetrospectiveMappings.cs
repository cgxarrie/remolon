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
        OrganizationId = retro.OrganizationId,
        OrganizationName = retro.Organization?.Name ?? string.Empty,
        Title = retro.Title,
        IsClosed = retro.IsClosed,
        IsRevealed = retro.IsRevealed,
        RetrospectiveDate = retro.RetrospectiveDate,
        CreatedAt = retro.CreatedAt,
        UpdatedAt = retro.UpdatedAt,
    };

    public static Dtos.GetRetrospectiveDto ToGetDto(this Models.Retrospective retro, string currentUserId) => new()
    {
        Id = retro.Id,
        OrganizationId = retro.OrganizationId,
        OrganizationName = retro.Organization?.Name ?? string.Empty,
        Title = retro.Title,
        IsClosed = retro.IsClosed,
        IsRevealed = retro.IsRevealed,
        RetrospectiveDate = retro.RetrospectiveDate,
        Columns = retro.Columns
            .Where(c => c is not Models.ActionColumn)
            .Select(c => new Dtos.GetColumnDto
            {
                Id = c.Id,
                Title = c.Title,
                Position = c.Position,
                HeaderColor = c.HeaderColor,
                Items = c.Items
                    .Where(i => i is not Models.ActionItem)
                    .Where(i => retro.IsRevealed || i.CreatedBy == currentUserId)
                    .Select(i => i.ToDto())
                    .ToList(),
                HiddenAuthorCounts = [],
                HiddenItemCount = HiddenItemCount(c, retro.IsRevealed, currentUserId),
            })
            .ToList(),
        ActionColumns = retro.Columns
            .OfType<Models.ActionColumn>()
            .Select(c => new Dtos.GetActionColumnDto
            {
                Id = c.Id,
                Title = c.Title,
                Position = c.Position,
                Items = c.Items
                    .OfType<Models.ActionItem>()
                    .Where(i => retro.IsRevealed || i.CreatedBy == currentUserId)
                    .Select(i => i.ToDto())
                    .ToList(),
            })
            .ToList(),
        CreatedAt = retro.CreatedAt,
        UpdatedAt = retro.UpdatedAt,
    };

    private static int HiddenItemCount(
        Models.Column column,
        bool isRevealed,
        string currentUserId)
    {
        if (isRevealed) return 0;

        return column.Items.Count(i => i is not Models.ActionItem && i.CreatedBy != currentUserId);
    }
}

namespace RetroBackend.Mappings;

public static class ItemDtoMappings
{
    public static Models.CreateItemRequest ToServiceRequest(this Dtos.CreateItemRequest dto) => new()
    {
        ColumnId = dto.ColumnId,
        Description = dto.Description,
        Position = dto.Position,
    };

    public static Models.UpdateItemRequest ToServiceRequest(this Dtos.UpdateItemRequest dto) => new()
    {
        Description = dto.Description,
        Position = dto.Position,
    };

    public static Models.CreateActionItemRequest ToServiceRequest(this Dtos.CreateActionItemRequest dto) => new()
    {
        Assignee = dto.Assignee,
        ColumnId = dto.ColumnId,
        Description = dto.Description,
        Position = dto.Position,
    };

    public static Models.UpdateActionItemRequest ToServiceRequest(this Dtos.UpdateActionItemRequest dto) => new()
    {
        Description = dto.Description,
        Position = dto.Position,
        Assignee = dto.Assignee,
        IsCompleted = dto.IsCompleted,
    };

    public static Dtos.GetItemDto ToDto(this Models.Item item) => new()
    {
        Id = item.Id,
        ColumnId = item.ColumnId,
        GroupId = item.GroupId,
        Description = item.Description,
        Position = item.Position,
        CreatedBy = item.CreatedBy,
        CreatedByNickname = item.CreatedByNickname,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };

    public static Dtos.GetActionItemDto ToDto(this Models.ActionItem item) => new()
    {
        Id = item.Id,
        ColumnId = item.ColumnId,
        Description = item.Description,
        Position = item.Position,
        CreatedBy = item.CreatedBy,
        CreatedByNickname = item.CreatedByNickname,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
        Assignee = item.Assignee,
        IsCompleted = item.IsCompleted,
        Iterations = item.Iterations,
        ClosedBy = item.ClosedBy,
        ClosedAt = item.ClosedAt,
    };
}

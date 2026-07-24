namespace RetroBackend.Models;

public class CreateItemRequest
{
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByNickname { get; set; } = string.Empty;
    public Guid ColumnId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class UpdateItemRequest
{
    public string? Description { get; set; }
    public int? Position { get; set; }
}

public class CreateActionItemRequest
{
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByNickname { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public Guid ColumnId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class UpdateActionItemRequest
{
    public string? Description { get; set; }
    public int? Position { get; set; }
    public string? Assignee { get; set; }
    public bool? IsCompleted { get; set; }
}

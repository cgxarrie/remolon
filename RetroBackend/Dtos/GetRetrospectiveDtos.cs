namespace RetroBackend.Dtos;

public class GetColumnDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public string? HeaderColor { get; set; }
    public List<GetItemDto> Items { get; set; } = [];
}

public class GetActionColumnDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public List<GetActionItemDto> Items { get; set; } = [];
}

public class GetRetrospectiveSummaryDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public bool IsRevealed { get; set; }
    public DateTime? RetrospectiveDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetRetrospectiveDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public bool IsRevealed { get; set; }
    public DateTime? RetrospectiveDate { get; set; }
    public List<GetColumnDto> Columns { get; set; } = [];
    public List<GetActionColumnDto> ActionColumns { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


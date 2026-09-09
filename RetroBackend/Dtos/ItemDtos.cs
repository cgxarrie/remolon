using System.ComponentModel.DataAnnotations;
using RetroBackend.Models;

namespace RetroBackend.Dtos;

public static class ItemDescriptionLimits
{
    public const int MaxLength = ItemLimits.DescriptionMaxLength;
}

public class CreateItemRequest
{
    [Required]
    public Guid ColumnId { get; set; }

    [Required]
    [MaxLength(ItemDescriptionLimits.MaxLength)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Position { get; set; }
}

public class UpdateItemRequest
{
    [MaxLength(ItemDescriptionLimits.MaxLength)]
    public string? Description { get; set; }
    public int? Position { get; set; }
}

public class GetItemDto
{
    public Guid Id { get; set; }
    public Guid IterationId { get; set; }
    public Guid ColumnId { get; set; }
    public Guid? GroupId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Position { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByNickname { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateActionItemRequest
{
    [Required]
    public Guid ColumnId { get; set; }

    [Required]
    [MaxLength(ItemDescriptionLimits.MaxLength)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Position { get; set; }

    public List<string> Assignees { get; set; } = [];
}

public class UpdateActionItemRequest
{
    [MaxLength(ItemDescriptionLimits.MaxLength)]
    public string? Description { get; set; }
    public int? Position { get; set; }
    public List<string>? Assignees { get; set; }
    public bool? IsCompleted { get; set; }
}

public class GetActionItemDto : GetItemDto
{
    public List<string> Assignees { get; set; } = [];
    public bool IsCompleted { get; set; }
    public int Iterations { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class MergeItemRequest
{
    [Required]
    public Guid TargetItemId { get; set; }
}

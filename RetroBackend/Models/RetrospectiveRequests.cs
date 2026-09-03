using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Models;

public class CreateRetrospectiveRequest
{

    [Required]
    public string CurrentUser { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public List<CreateRetrospectiveRequestColumn> Columns { get; set; } = [];
}

public class CreateRetrospectiveRequestColumn
{
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
}


public class UpdateRetrospectiveRequest
{

    [Required]
    public string CurrentUser { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string? Title { get; set; }
    public List<UpdateRetrospectiveRequestAddColumn>? AddColumns { get; set; }
    public List<UpdateRetrospectiveRequestUpdateColumn>? UpdateColumns { get; set; }
    public List<Guid>? RemoveColumnIds { get; set; }
    public DateTime? RetrospectiveDate { get; set; }
}

public class UpdateRetrospectiveRequestAddColumn
{
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class UpdateRetrospectiveRequestUpdateColumn
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public int? Position { get; set; }
    public string? HeaderColor { get; set; }
}

public class CloseRetrospectiveRequest
{
    [Required]
    public string CurrentUser { get; set; } = string.Empty;
}

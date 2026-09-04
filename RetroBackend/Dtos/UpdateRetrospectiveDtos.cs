using System.ComponentModel.DataAnnotations;
using RetroBackend.Models;

namespace RetroBackend.Dtos;

public class UpdateRetrospectiveRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public List<AddRetrospectiveColumnRequest>? AddColumns { get; set; }
    public List<UpdateRetrospectiveColumnRequest>? UpdateColumns { get; set; }
    public List<Guid>? RemoveColumnIds { get; set; }
}

public class AddRetrospectiveColumnRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int Position { get; set; }
}

public class UpdateRetrospectiveColumnRequest
{
    [Required]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public int? Position { get; set; }

    [MaxLength(7)]
    public string? HeaderColor { get; set; }
}


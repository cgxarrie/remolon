using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;


public class CreateRetrospectiveRequest
{
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




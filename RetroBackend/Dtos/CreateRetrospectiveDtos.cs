using System.ComponentModel.DataAnnotations;

namespace RetroBackend.Dtos;


public class CreateRetrospectiveRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    [Required, MinLength(1)]
    public List<string> ManagerUserIds { get; set; } = [];

    public List<CreateRetrospectiveRequestColumn> Columns { get; set; } = [];
}

public class CreateRetrospectiveRequestColumn
{
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
}




namespace RetroBackend.Models;

public class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

    public string CreatedBy { get; protected set; } = string.Empty;

    protected void SetField<T>(ref T field, T value)
    {
        field = value;
        UpdatedAt = DateTime.UtcNow;
    }

    protected BaseEntity(string createdBy)
    {
        CreatedBy = createdBy;
    }
}
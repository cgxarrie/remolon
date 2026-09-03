namespace RetroBackend.Models;



public class Retrospective : BaseEntity
{
    private string _title = string.Empty;
    private bool _isClosed = false;
    private bool _isRevealed = false;
    private DateTime? _retrospectiveDate;

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public bool IsClosed => _isClosed;
    public bool IsRevealed => _isRevealed;
    public Guid OrganizationId { get; private set; }
    public Organization? Organization { get; private set; }

    public DateTime? RetrospectiveDate
    {
        get => _retrospectiveDate;
        set => SetField(ref _retrospectiveDate, value);
    }

    public List<Column> Columns { get; private set; }


    public Retrospective(string createdBy) : base(createdBy)
    {
        Columns = [];
    }

    // Safe for EF Core to use when materialising from the database.
    // Does NOT create ActionColumns; direct field assignment avoids SetField side-effects.
    public Retrospective(string createdBy, string title, Guid organizationId = default) : base(createdBy)
    {
        _title = title;
        OrganizationId = organizationId;
        Columns = [];
    }

    // Use this factory method when creating a brand-new retrospective so that the
    // default ActionColumns are added after the entity has a stable Id.
    public static Retrospective CreateNew(string createdBy, string title, Guid organizationId = default)
    {
        var retro = new Retrospective(createdBy, title, organizationId);
        retro.Columns.Add(ActionColumn.NewPendingActionItemsColumn("system", retro.Id));
        retro.Columns.Add(ActionColumn.NewActionItemsColumn("system", retro.Id));
        return retro;
    }


    public void AddColumn(string createdBy, string title, int position)
    {
        if (_isClosed) throw new InvalidOperationException("Cannot modify a closed retrospective.");
        Columns.Add(new Column(createdBy, Id, title, position));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveColumn(Guid columnId)
    {
        if (_isClosed) throw new InvalidOperationException("Cannot modify a closed retrospective.");
        var columnToRemove = Columns.FirstOrDefault(c => c.Id == columnId);
        if (columnToRemove != null)
        {
            Columns.Remove(columnToRemove);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateColumn(Guid columnId, string? newTitle, int? newPosition, string? newHeaderColor = null)
    {
        if (_isClosed) throw new InvalidOperationException("Cannot modify a closed retrospective.");
        var columnToUpdate = Columns.FirstOrDefault(c => c.Id == columnId);
        if (columnToUpdate is null) return;
        columnToUpdate.Title = newTitle ?? columnToUpdate.Title;
        columnToUpdate.Position = newPosition ?? columnToUpdate.Position;
        if (newHeaderColor is not null) columnToUpdate.HeaderColor = newHeaderColor;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        SetField(ref _isClosed, true);
    }

    public void Reveal()
    {
        SetField(ref _isRevealed, true);
    }
}



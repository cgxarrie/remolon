namespace RetroBackend.Models;


public class Item : BaseEntity
{
    private string _description = string.Empty;
    private int _position;
    private Guid? _groupId;
    private string _createdByNickname = string.Empty;

    public Guid ColumnId { get; set; }
    public Guid? GroupId => _groupId;

    public string CreatedByNickname
    {
        get => _createdByNickname;
        set => SetField(ref _createdByNickname, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }
    public int Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }

    public void JoinGroup(Guid groupId) => SetField(ref _groupId, (Guid?)groupId);
    public void LeaveGroup() => SetField(ref _groupId, (Guid?)null);

    public Item(string createdBy, string createdByNickname, Guid columnId,
        string description, int position) : base(createdBy)
    {
        ColumnId = columnId;
        _description = description;
        _position = position;
        _createdByNickname = createdByNickname;
    }
}

public class ActionItem : Item
{
    private bool _isCompleted = false;
    private string _asseignee = string.Empty;
    private int _iterations = 0;
    private string? _closedBy;
    private DateTime? _closedAt;

    public bool IsCompleted
    {
        get => _isCompleted;
        private set => SetField(ref _isCompleted, value);
    }

    public string Assignee
    {
        get => _asseignee;
        private set => SetField(ref _asseignee, value);
    }

    public int Iterations
    {
        get => _iterations;
        private set => SetField(ref _iterations, value);
    }

    public string? ClosedBy
    {
        get => _closedBy;
        private set => SetField(ref _closedBy, value);
    }

    public DateTime? ClosedAt
    {
        get => _closedAt;
        private set => SetField(ref _closedAt, value);
    }

    public ActionItem(string createdBy, string createdByNickname, string assignee, Guid columnId,
        string description, int position) : base(createdBy, createdByNickname, columnId, description, position)
    {
        Assignee = assignee;
    }

    public ActionItem(string createdBy, string createdByNickname, string assignee, Guid columnId,
        string description, int position, int iterations) : base(createdBy, createdByNickname, columnId, description, position)
    {
        Assignee = assignee;
        _iterations = iterations;
    }

    public void Complete(string closedBy)
    {
        IsCompleted = true;
        ClosedBy = closedBy;
        ClosedAt = DateTime.UtcNow;
    }

    public void Assign(string assignee)
    {
        Assignee = assignee;
    }
}
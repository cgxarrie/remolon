using Microsoft.AspNetCore.Http.HttpResults;

namespace RetroBackend.Models;


public class Column : BaseEntity
{

    private string _title = string.Empty;
    private int _position;
    private string? _headerColor;

    protected bool _isEditable = true;

    public Guid RetrospectiveId { get; private set; }

    public bool IsEditable => _isEditable;

    public List<Item> Items { get; private set; } = [];

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }
    public int Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }
    public string? HeaderColor
    {
        get => _headerColor;
        set => SetField(ref _headerColor, value);
    }

    public Column(string createdBy, Guid retroId, string title, int position) : base(createdBy)
    {
        RetrospectiveId = retroId;
        _title = title;
        _position = position;
    }

    protected Column(string createdBy) : base(createdBy) { }
}


public class ActionColumn : Column
{

    public ActionColumn(string createdBy, Guid retroId, string title, int position) :
    base(createdBy, retroId, title, position)
    {
        _isEditable = false;
    }

    protected ActionColumn(string createdBy) : base(createdBy) { _isEditable = false; }

    public static ActionColumn NewPendingActionItemsColumn(string createdBy, Guid retroId) =>
        new ActionColumn(createdBy, retroId, "Pending Action Items", 0);

    public static ActionColumn NewActionItemsColumn(string createdBy, Guid retroId) =>
        new ActionColumn(createdBy, retroId, "Action Items", int.MaxValue);
}


using RetroBackend.Models;
using RetroBackend.Repositories;

namespace RetroBackend.Services;

public class RetrospectiveService : IRetrospectiveService
{
    private readonly IRetrospectiveRepository _repository;

    public RetrospectiveService(IRetrospectiveRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<Retrospective>> GetAllAsync() =>
        _repository.GetAllAsync();

    public Task<IEnumerable<Retrospective>> GetAllForUserAsync(string userId) =>
        _repository.GetAllForUserAsync(userId);

    public Task<Retrospective?> GetByIdAsync(Guid id) =>
        _repository.GetByIdAsync(id);

    public async Task<Retrospective> CreateAsync(CreateRetrospectiveRequest request)
    {
        var retro = Retrospective.CreateNew(request.CurrentUser, request.Title);
        request.Columns.ForEach(c => retro.AddColumn(request.CurrentUser, c.Title, c.Position));
        return await _repository.AddAsync(retro);
    }

    public async Task<Retrospective?> UpdateAsync(Guid id, UpdateRetrospectiveRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return null;
        if (existing.IsClosed) return null;

        if (request.Title is not null) existing.Title = request.Title;
        if (request.RetrospectiveDate.HasValue) existing.RetrospectiveDate = request.RetrospectiveDate.Value;
        request.RemoveColumnIds?.ForEach(existing.RemoveColumn);
        request.UpdateColumns?.ForEach(c => existing.UpdateColumn(c.Id, c.Title, c.Position, c.HeaderColor));
        request.AddColumns?.ForEach(c => existing.AddColumn(request.CurrentUser, c.Title, c.Position));

        return await _repository.UpdateAsync(existing);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return _repository.DeleteAsync(id);
    }

    public async Task<Retrospective?> CloseAsync(Guid id, CloseRetrospectiveRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null || existing.IsClosed) return null;

        existing.Close();
        await _repository.UpdateAsync(existing);

        var newRetroDraft = Retrospective.CreateNew(request.CurrentUser, existing.Title);

        // Copy regular (non-action) columns
        foreach (var col in existing.Columns.Where(c => c is not ActionColumn))
            newRetroDraft.AddColumn(request.CurrentUser, col.Title, col.Position);

        // Carry over action items to the new retro's Pending Action Items column
        var pendingColumn = newRetroDraft.Columns
            .OfType<ActionColumn>()
            .First(c => c.Title == "Pending Action Items");

        var position = 0;

        // 1. Items still pending from the previous "Pending Action Items" column
        var existingPendingColumn = existing.Columns
            .OfType<ActionColumn>()
            .FirstOrDefault(c => c.Title == "Pending Action Items");

        if (existingPendingColumn is not null)
        {
            foreach (var item in existingPendingColumn.Items.OfType<ActionItem>().Where(i => !i.IsCompleted))
            {
                pendingColumn.Items.Add(new ActionItem(
                    request.CurrentUser,
                    item.CreatedByNickname,
                    item.Assignee,
                    pendingColumn.Id,
                    item.Description,
                    position++,
                    item.Iterations + 1));
            }
        }

        // 2. Uncompleted items from "Action Items" column
        var actionColumn = existing.Columns
            .OfType<ActionColumn>()
            .FirstOrDefault(c => c.Title == "Action Items");

        if (actionColumn is not null)
        {
            foreach (var item in actionColumn.Items.OfType<ActionItem>().Where(i => !i.IsCompleted))
            {
                pendingColumn.Items.Add(new ActionItem(
                    request.CurrentUser,
                    item.CreatedByNickname,
                    item.Assignee,
                    pendingColumn.Id,
                    item.Description,
                    position++,
                    item.Iterations + 1));
            }
        }

        var newRetro = await _repository.AddAsync(newRetroDraft);
        await _repository.CopyUserAssignmentsAsync(existing.Id, newRetro.Id);
        return newRetro;
    }
}

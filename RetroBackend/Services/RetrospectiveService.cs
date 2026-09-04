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
        var retro = Retrospective.CreateNew(request.CurrentUser, request.Title, request.OrganizationId);
        request.Columns.ForEach(c => retro.AddColumn(request.CurrentUser, c.Title, c.Position));
        return await _repository.AddAsync(retro);
    }

    public async Task<Retrospective?> UpdateAsync(Guid id, UpdateRetrospectiveRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return null;
        if (existing.IsClosed) return null;

        if (request.Title is not null) existing.Title = request.Title;
        request.RemoveColumnIds?.ForEach(existing.RemoveColumn);
        request.UpdateColumns?.ForEach(c => existing.UpdateColumn(c.Id, c.Title, c.Position, c.HeaderColor));
        request.AddColumns?.ForEach(c => existing.AddColumn(request.CurrentUser, c.Title, c.Position));

        return await _repository.UpdateAsync(existing);
    }

    public async Task<Retrospective?> RevealAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return null;
        if (existing.IsRevealed) return existing;

        existing.Reveal();
        return await _repository.UpdateAsync(existing);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return _repository.DeleteAsync(id);
    }

    public async Task<Retrospective?> CloseAsync(Guid id, CloseRetrospectiveRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null || existing.IsClosed || !existing.IsRevealed) return null;

        var actingUser = string.IsNullOrWhiteSpace(request.CurrentUser)
            ? existing.CreatedBy
            : request.CurrentUser;

        existing.Close();
        await _repository.UpdateAsync(existing);

        var newRetroDraft = Retrospective.CreateNew(actingUser, existing.Title, existing.OrganizationId);

        // Copy regular (non-action) columns
        foreach (var col in existing.Columns.Where(c => c is not ActionColumn))
            newRetroDraft.AddColumn(actingUser, col.Title, col.Position);

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
                    actingUser,
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
                    actingUser,
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

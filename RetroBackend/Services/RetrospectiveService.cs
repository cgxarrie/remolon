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

        existing.Close();
        await _repository.UpdateAsync(existing);

        var actingUser = string.IsNullOrWhiteSpace(request.CurrentUser) ? existing.CreatedBy : request.CurrentUser;
        return await CreateIterationFromSourceAsync(existing, actingUser) ?? existing;
    }

    public async Task<Retrospective?> CreateNextIterationAsync(Guid id, string currentUser)
    {
        var source = await _repository.GetByIdAsync(id);
        if (source is null || !source.IsClosed) return null;

        var actingUser = string.IsNullOrWhiteSpace(currentUser) ? source.CreatedBy : currentUser;
        return await CreateIterationFromSourceAsync(source, actingUser);
    }

    private async Task<Retrospective?> CreateIterationFromSourceAsync(Retrospective source, string actingUser)
    {
        if (await _repository.HasOpenWithTitleAsync(source.OrganizationId, source.Title))
            return null;

        var next = Retrospective.CreateNew(actingUser, source.Title, source.OrganizationId);

        foreach (var col in source.Columns.Where(c => c is not ActionColumn))
            next.AddColumn(actingUser, col.Title, col.Position);

        CopyActionItemsAsPending(source, next, actingUser);

        var created = await _repository.AddAsync(next);
        await _repository.CopyUserAssignmentsAsync(source.Id, created.Id);
        return created;
    }

    private static void CopyActionItemsAsPending(Retrospective source, Retrospective next, string actingUser)
    {
        var pendingColumn = next.Columns
            .OfType<ActionColumn>()
            .First(c => c.Title == "Pending Action Items");

        var position = 0;

        var existingPendingColumn = source.Columns
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

        var actionColumn = source.Columns
            .OfType<ActionColumn>()
            .FirstOrDefault(c => c.Title == "Action Items");

        if (actionColumn is not null)
        {
            foreach (var item in actionColumn.Items.OfType<ActionItem>())
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
    }

    public Task<bool> HasOpenWithTitleAsync(Guid organizationId, string title) =>
        _repository.HasOpenWithTitleAsync(organizationId, title);
}

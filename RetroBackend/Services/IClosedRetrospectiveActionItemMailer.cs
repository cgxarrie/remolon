using RetroBackend.Models;

namespace RetroBackend.Services;

public interface IClosedRetrospectiveActionItemMailer
{
    Task NotifyAsync(Retrospective retrospective, CancellationToken cancellationToken = default);
}

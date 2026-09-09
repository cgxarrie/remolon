using RetroBackend.Models;
using RetroBackend.Services;

namespace RetroBackend.Tests.Fakes;

public sealed class NoOpClosedRetrospectiveActionItemMailer : IClosedRetrospectiveActionItemMailer
{
    public Task NotifyAsync(Retrospective retrospective, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace RetroBackend.Hubs;

public sealed class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

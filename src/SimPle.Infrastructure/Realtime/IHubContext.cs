namespace SimPle.Infrastructure.Realtime;

// Placeholder interfaces for Module 7 (Real-Time Presence, Lobby, Chat).
// The Application layer depends on these abstractions; the concrete SignalR implementations
// will live in Infrastructure once Microsoft.AspNetCore.SignalR is wired in.

public interface IPresenceNotifier
{
    Task UserOnlineAsync(Guid userId, CancellationToken ct = default);
    Task UserOfflineAsync(Guid userId, CancellationToken ct = default);
    Task FriendStatusChangedAsync(Guid friendId, string status, string activity, CancellationToken ct = default);
}

public interface ILobbyNotifier
{
    Task LobbyUpdatedAsync(string lobbyCode, object lobbyState, CancellationToken ct = default);
    Task PlayerJoinedAsync(string lobbyCode, object slot, CancellationToken ct = default);
    Task PlayerLeftAsync(string lobbyCode, Guid userId, CancellationToken ct = default);
    Task GameStartingAsync(string lobbyCode, Guid sessionId, CancellationToken ct = default);
}

public interface IGameNotifier
{
    Task MatchStateUpdatedAsync(Guid sessionId, object state, CancellationToken ct = default);
    Task MoveAcceptedAsync(Guid sessionId, object move, CancellationToken ct = default);
    Task MoveRejectedAsync(Guid sessionId, Guid playerId, string reason, CancellationToken ct = default);
    Task MatchEndedAsync(Guid sessionId, object result, CancellationToken ct = default);
}

public interface IHardwareNotifier
{
    Task DeviceConnectedAsync(Guid deviceId, CancellationToken ct = default);
    Task DeviceInputReceivedAsync(Guid deviceId, string eventType, string payload, CancellationToken ct = default);
}

using SimPle.Domain.Common;

namespace SimPle.Domain.Lobbies;

public class Lobby : Entity
{
    public string Code { get; private set; } = default!;
    public Guid GameId { get; private set; }
    public Guid HostUserId { get; private set; }
    public LobbyPrivacy Privacy { get; private set; } = LobbyPrivacy.Private;
    public int MaxSlots { get; private set; } = 4;
    public string TimeControl { get; private set; } = "Blitz 3+2";
    public bool IsRanked { get; private set; }
    public bool AiFillEnabled { get; private set; }
    public LobbyStatus Status { get; private set; } = LobbyStatus.Open;
    public DateTime ExpiresAt { get; private set; } = DateTime.UtcNow.AddHours(2);
    public Guid? GameSessionId { get; private set; }

    private readonly List<LobbySlot> _slots = [];
    public IReadOnlyList<LobbySlot> Slots => _slots.AsReadOnly();

    private Lobby() { }

    public static Lobby Create(Guid gameId, Guid hostUserId, LobbyPrivacy privacy, int maxSlots, bool isRanked)
    {
        var lobby = new Lobby
        {
            Code = GenerateCode(),
            GameId = gameId,
            HostUserId = hostUserId,
            Privacy = privacy,
            MaxSlots = maxSlots,
            IsRanked = isRanked,
        };
        lobby._slots.Add(new LobbySlot { LobbyId = lobby.Id, UserId = hostUserId, SeatIndex = 0, IsHost = true, IsReady = true });
        return lobby;
    }

    public void Close() { Status = LobbyStatus.Closed; Touch(); }
    public void Start(Guid sessionId) { Status = LobbyStatus.InGame; GameSessionId = sessionId; Touch(); }
    public void ToggleReady(Guid userId) { _slots.FirstOrDefault(s => s.UserId == userId)?.ToggleReady(); Touch(); }

    private static string GenerateCode() =>
        $"SP-{Random.Shared.Next(0, 99):D2}{(char)Random.Shared.Next('A', 'Z')}-{Random.Shared.Next(0, 99):D2}";
}

public class LobbySlot
{
    public Guid LobbyId { get; set; }
    public int SeatIndex { get; set; }
    public Guid? UserId { get; set; }
    public bool IsHost { get; set; }
    public bool IsAi { get; set; }
    public string? AiDifficulty { get; set; }
    public bool IsReady { get; set; }
    public void ToggleReady() => IsReady = !IsReady;
}

public enum LobbyPrivacy { Private, Public }
public enum LobbyStatus { Open, Closed, InGame }

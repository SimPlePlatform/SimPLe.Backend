using SimPle.Domain.Common;

namespace SimPle.Domain.GameHost;

/// <summary>
/// A hosted game session. Game-specific logic is delegated to IGameEngine implementations.
/// </summary>
public class GameSession : Entity
{
    public Guid GameId { get; private set; }
    public Guid? LobbyId { get; private set; }
    public Guid HostUserId { get; private set; }
    public GameSessionStatus Status { get; private set; } = GameSessionStatus.Waiting;
    public GameMode Mode { get; private set; }
    public string TimeControl { get; private set; } = "Blitz 3+2";
    public bool IsRanked { get; private set; }
    public int CurrentRound { get; private set; }
    public int TotalRounds { get; private set; } = 1;
    public string? StateJson { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }

    private readonly List<MatchPlayer> _players = [];
    public IReadOnlyList<MatchPlayer> Players => _players.AsReadOnly();

    private readonly List<Move> _moves = [];
    public IReadOnlyList<Move> Moves => _moves.AsReadOnly();

    private GameSession() { }

    public static GameSession Create(Guid gameId, Guid hostUserId, GameMode mode, bool isRanked = false) => new()
    {
        GameId = gameId,
        HostUserId = hostUserId,
        Mode = mode,
        IsRanked = isRanked,
    };

    public void AddPlayer(Guid userId, int seatIndex) =>
        _players.Add(new MatchPlayer { UserId = userId, SeatIndex = seatIndex, SessionId = Id });

    public void Start() { Status = GameSessionStatus.Active; StartedAt = DateTime.UtcNow; Touch(); }
    public void Pause() { Status = GameSessionStatus.Paused; Touch(); }
    public void Resume() { Status = GameSessionStatus.Active; Touch(); }
    public void Finish() { Status = GameSessionStatus.Completed; EndedAt = DateTime.UtcNow; Touch(); }
    public void Cancel() { Status = GameSessionStatus.Cancelled; EndedAt = DateTime.UtcNow; Touch(); }

    public void UpdateState(string stateJson) { StateJson = stateJson; Touch(); }

    public void RecordMove(Guid playerId, string moveData) =>
        _moves.Add(new Move { SessionId = Id, PlayerId = playerId, MoveData = moveData, PlayedAt = DateTime.UtcNow });
}

public class MatchPlayer
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public int SeatIndex { get; set; }
    public bool IsReady { get; set; }
    public bool IsAi { get; set; }
    public string? AiDifficulty { get; set; }
    public MatchResult? Result { get; set; }
    public int EloDelta { get; set; }
    public int Score { get; set; }
}

public class Move
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid PlayerId { get; set; }
    public string MoveData { get; set; } = default!;
    public DateTime PlayedAt { get; set; }
    public bool WasValid { get; set; } = true;
}

public enum GameSessionStatus { Waiting, Active, Paused, Completed, Cancelled }
public enum GameMode { SoloVsAi, FriendPrivate, FriendPublic, QuickMatch, Ranked }
public enum MatchResult { Win, Loss, Draw, Forfeit, Disconnect, Timeout }

namespace SimPle.Domain.GameHost;

/// <summary>
/// Each game type provides an implementation. The platform calls these interfaces
/// without knowing the game-specific rules.
/// </summary>
public interface IGameEngine
{
    string GameSlug { get; }
    object CreateInitialState(GameSession session);
    MoveValidationResult ValidateMove(object state, Move move);
    object ApplyMove(object state, Move move);
    bool IsGameOver(object state, GameSession session);
    MatchResult DetermineWinner(object state, GameSession session, Guid playerId);
    int CalculateEloDelta(bool won, int playerElo, int opponentElo, bool isRanked);
}

public interface IGameStateSerializer
{
    string GameSlug { get; }
    string Serialize(object state);
    object Deserialize(string json);
}

public interface IAIOpponentService
{
    string GameSlug { get; }
    Move GenerateMove(object state, string difficulty, Guid aiPlayerId);
}

public interface IGameStatsService
{
    string GameSlug { get; }
    Task RecordMatchResultAsync(GameSession session, CancellationToken ct = default);
}

public sealed record MoveValidationResult(bool IsValid, string? RejectionReason = null)
{
    public static MoveValidationResult Valid() => new(true);
    public static MoveValidationResult Invalid(string reason) => new(false, reason);
}

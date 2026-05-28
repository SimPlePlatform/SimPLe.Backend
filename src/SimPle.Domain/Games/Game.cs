using SimPle.Domain.Common;

namespace SimPle.Domain.Games;

/// <summary>
/// Catalog entry for a game. The actual game logic lives in the GameHost module.
/// </summary>
public class Game : Entity
{
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Rules { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public string Difficulty { get; private set; } = "Medium";
    public string EstimatedDuration { get; private set; } = "5-10 min";
    public int MinPlayers { get; private set; } = 1;
    public int MaxPlayers { get; private set; } = 2;
    public bool SupportsSolo { get; private set; } = true;
    public bool SupportsAi { get; private set; } = true;
    public bool SupportsMultiplayer { get; private set; } = true;
    public bool SupportsRanked { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }

    public IReadOnlyList<string> Tags { get; private set; } = [];
    public IReadOnlyList<string> AiDifficultyLevels { get; private set; } = ["Easy", "Medium", "Hard"];

    private Game() { }

    public static Game Create(
        string slug, string name, string description, string rules,
        string category, int minPlayers, int maxPlayers) => new()
    {
        Slug = slug,
        Name = name,
        Description = description,
        Rules = rules,
        Category = category,
        MinPlayers = minPlayers,
        MaxPlayers = maxPlayers,
    };
}

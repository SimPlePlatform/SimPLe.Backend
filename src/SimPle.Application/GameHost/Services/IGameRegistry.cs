using SimPle.Domain.GameHost;

namespace SimPle.Application.GameHost.Services;

/// <summary>
/// Registry of all installed game engines. Future games register themselves here.
/// </summary>
public interface IGameRegistry
{
    IGameEngine GetEngine(string gameSlug);
    IReadOnlyList<string> RegisteredGameSlugs { get; }
    bool IsRegistered(string gameSlug);
}

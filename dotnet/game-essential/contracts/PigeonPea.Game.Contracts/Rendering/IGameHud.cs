namespace PigeonPea.Game.Contracts.Rendering;

using PigeonPea.Game.Contracts.Models;

public interface IGameHud
{
    string Id { get; }

    void Initialize(HudContext context);

    void Run(GameState initialState);

    void Shutdown();
}

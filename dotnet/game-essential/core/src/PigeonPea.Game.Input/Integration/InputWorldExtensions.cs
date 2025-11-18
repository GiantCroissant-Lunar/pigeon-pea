using MessagePipe;
using PigeonPea.Game.Input.Integration;

namespace PigeonPea.Game.Input.Integration;

/// <summary>
/// Extension methods for GameWorld to add input system support.
/// </summary>
public static class InputWorldExtensions
{
    /// <summary>
    /// Adds input system integration to GameWorld.
    /// </summary>
    public static GameWorldInputIntegration AddInputSystem(
        this GameWorld world,
        IPublisher<MoveInputEvent>? movePublisher = null,
        IPublisher<AttackInputEvent>? attackPublisher = null,
        IPublisher<InteractInputEvent>? interactPublisher = null)
    {
        var inputIntegration = new GameWorldInputIntegration(
            world,
            movePublisher,
            attackPublisher,
            interactPublisher);

        return inputIntegration;
    }

    /// <summary>
    /// Enables gameplay input map for the given input integration.
    /// </summary>
    public static void EnableGameplayInput(this GameWorldInputIntegration inputIntegration)
    {
        inputIntegration.SwitchToGameplayMap();
    }

    /// <summary>
    /// Enables UI input map for the given input integration.
    /// </summary>
    public static void EnableUIInput(this GameWorldInputIntegration inputIntegration)
    {
        inputIntegration.SwitchToUIMap();
    }
}

using MessagePipe;
using NexusInput;
using NexusInput.Actions;
using NexusInput.Json;
using PigeonPea.Game.Input.Devices;
using PigeonPea.Game.Input.Events;
using SadRogue.Primitives;

namespace PigeonPea.Game.Input.Integration;

/// <summary>
/// Integrates NexusInput with GameWorld.
/// </summary>
public sealed class GameWorldInputIntegration
{
    private readonly InputSystem _inputSystem;
    private readonly InputActionAsset _playerControls;
    private readonly GameWorld _gameWorld;

    private readonly IPublisher<MoveInputEvent>? _movePublisher;
    private readonly IPublisher<AttackInputEvent>? _attackPublisher;
    private readonly IPublisher<InteractInputEvent>? _interactPublisher;

    public GameWorldInputIntegration(
        GameWorld gameWorld,
        IPublisher<MoveInputEvent>? movePublisher = null,
        IPublisher<AttackInputEvent>? attackPublisher = null,
        IPublisher<InteractInputEvent>? interactPublisher = null)
    {
        _gameWorld = gameWorld;
        _movePublisher = movePublisher;
        _attackPublisher = attackPublisher;
        _interactPublisher = interactPublisher;

        _inputSystem = new InputSystem();

        // Register console keyboard device
        var keyboard = new ConsoleKeyboardDevice();
        _inputSystem.RegisterDevice(keyboard);

        // Load default player controls
        var json = LoadDefaultPlayerControls();
        _playerControls = InputActionAssetJson.FromJson(json);
        _inputSystem.RegisterAsset(_playerControls);

        // Enable gameplay map
        var gameplayMap = _playerControls.GetMap("Gameplay");
        gameplayMap?.Enable();

        // Register callbacks
        RegisterCallbacks(gameplayMap!);
    }

    private void RegisterCallbacks(InputActionMap gameplayMap)
    {
        // Move action
        var moveAction = gameplayMap.GetAction("Move");
        moveAction?.OnPerformed(context =>
        {
            var direction = context.ReadValue<NexusInput.Controls.Vector2>();
            var point = new Point((int)direction.X, (int)direction.Y);

            _gameWorld.TryMovePlayer(point);
            _movePublisher?.Publish(new MoveInputEvent { Direction = point, Timestamp = (float)context.Time });
        });

        // Attack action
        var attackAction = gameplayMap.GetAction("Attack");
        attackAction?.OnPerformed(context =>
        {
            // Attack in current direction (or wait for direction input)
            _attackPublisher?.Publish(new AttackInputEvent { Timestamp = (float)context.Time });
        });

        // Interact action
        var interactAction = gameplayMap.GetAction("Interact");
        interactAction?.OnPerformed(context =>
        {
            _gameWorld.TryPickupItem();
            _interactPublisher?.Publish(new InteractInputEvent { Timestamp = (float)context.Time });
        });

        // Inventory action
        var inventoryAction = gameplayMap.GetAction("Inventory");
        inventoryAction?.OnPerformed(context =>
        {
            // Open inventory UI (publish event)
            // For now, just try to use first item as a demo
            _gameWorld.TryUseItem(0);
        });

        // Pause action
        var pauseAction = gameplayMap.GetAction("Pause");
        pauseAction?.OnPerformed(context =>
        {
            // Pause game (publish event)
            // For now, just switch to UI map to demonstrate
            SwitchToUIMap();
        });
    }

    public void Update(double deltaTime)
    {
        _inputSystem.Update(deltaTime);
    }

    public void SwitchToUIMap()
    {
        _playerControls.GetMap("Gameplay")?.Disable();
        _playerControls.GetMap("UI")?.Enable();
    }

    public void SwitchToGameplayMap()
    {
        _playerControls.GetMap("UI")?.Disable();
        _playerControls.GetMap("Gameplay")?.Enable();
    }

    private string LoadDefaultPlayerControls()
    {
        // For simplicity, embed JSON or load from file
        // In production, read from Assets/DefaultPlayerControls.inputactions
        var assembly = typeof(GameWorldInputIntegration).Assembly;
        var resourceName = "PigeonPea.Game.Input.Assets.DefaultPlayerControls.inputactions";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

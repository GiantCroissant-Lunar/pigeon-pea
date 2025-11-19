using MessagePipe;
using PigeonPea.Game.Input.Events;
using PigeonPea.Game.Input.Integration;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;

namespace InputDemoConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== PigeonPea Input System Demo ===");
        Console.WriteLine("Controls:");
        Console.WriteLine("  WASD/Arrow Keys - Move");
        Console.WriteLine("  Space - Attack");
        Console.WriteLine("  E - Interact/Pickup");
        Console.WriteLine("  I - Inventory (use first item)");
        Console.WriteLine("  Escape - Pause/Menu");
        Console.WriteLine("  Q - Quit");
        Console.WriteLine();

        // Setup MessagePipe for event handling
        var services = new ServiceCollection()
            .AddMessagePipe()
            .BuildServiceProvider();

        var movePublisher = services.GetRequiredService<IPublisher<MoveInputEvent>>();
        var attackPublisher = services.GetRequiredService<IPublisher<AttackInputEvent>>();
        var interactPublisher = services.GetRequiredService<IPublisher<InteractInputEvent>>();

        // Subscribe to events for logging
        var moveDisposable = services.GetRequiredService<ISubscriber<MoveInputEvent>>()
            .Subscribe(evt => Console.WriteLine($"[EVENT] Move: {evt.Direction}"));

        var attackDisposable = services.GetRequiredService<ISubscriber<AttackInputEvent>>()
            .Subscribe(evt => Console.WriteLine("[EVENT] Attack triggered"));

        var interactDisposable = services.GetRequiredService<ISubscriber<InteractInputEvent>>()
            .Subscribe(evt => Console.WriteLine("[EVENT] Interact triggered"));

        try
        {
            // Create game world
            var gameWorld = new GameWorld(40, 25);

            // Add input system integration
            var inputIntegration = gameWorld.AddInputSystem(
                movePublisher,
                attackPublisher,
                interactPublisher);

            Console.WriteLine("Game world created with input system integration.");
            Console.WriteLine($"Player starts at position: {gameWorld.PlayerEntity.Get<Position>().Point}");
            Console.WriteLine();

            // Game loop
            var running = true;
            var frameCount = 0;

            while (running)
            {
                // Update input system
                inputIntegration.Update(0.016); // ~60 FPS

                // Update game world
                gameWorld.Update(0.016);

                // Render simple status every 30 frames
                if (frameCount % 30 == 0)
                {
                    RenderStatus(gameWorld);
                }

                frameCount++;

                // Simple delay to prevent 100% CPU usage
                Thread.Sleep(16); // ~60 FPS

                // Check for quit key (handled separately for demo)
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Quitting...");
                        running = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            // Cleanup subscriptions
            moveDisposable.Dispose();
            attackDisposable.Dispose();
            interactDisposable.Dispose();

            Console.WriteLine("Demo ended. Press any key to exit.");
            Console.ReadKey();
        }
    }

    static void RenderStatus(GameWorld gameWorld)
    {
        var playerPos = gameWorld.PlayerEntity.Get<Position>();
        var playerHealth = gameWorld.PlayerEntity.Get<Health>();
        var playerExp = gameWorld.PlayerEntity.Get<Experience>();

        // Clear previous status and show new status
        Console.SetCursorPosition(0, 10);
        Console.WriteLine("=== Game Status ===");
        Console.WriteLine($"Position: {playerPos.Point}");
        Console.WriteLine($"Health: {playerHealth.Current}/{playerHealth.Maximum}");
        Console.WriteLine($"Level: {playerExp.Level} (XP: {playerExp.CurrentXP}/{playerExp.XPToNextLevel})");
        Console.WriteLine($"Inventory: {gameWorld.PlayerEntity.Get<Inventory>().Items.Count} items");

        // Show nearby entities
        var nearbyEntities = 0;
        var fov = gameWorld.PlayerEntity.Get<FieldOfView>();
        var query = new Arch.Core.QueryDescription().WithAll<Position, Health>();
        
        gameWorld.EcsWorld.Query(in query, (Arch.Core.Entity entity, ref Position pos, ref Health health) =>
        {
            if (entity != gameWorld.PlayerEntity && fov.VisibleTiles.Contains(pos.Point))
            {
                nearbyEntities++;
            }
        });

        Console.WriteLine($"Visible entities: {nearbyEntities}");
        Console.WriteLine(new string('=', 50));
    }
}

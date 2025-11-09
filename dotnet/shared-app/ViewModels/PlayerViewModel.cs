using Arch.Core;
using ReactiveUI;
using SadRogue.Primitives;
using PigeonPea.Shared.Components;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// ViewModel for player state with reactive property change notifications.
/// Exposes player state from ECS in a format suitable for UI binding.
/// </summary>
public class PlayerViewModel : ReactiveObject
{
    private int _health;
    private int _maxHealth;
    private int _level;
    private int _experience;
    private string _name = string.Empty;
    private Point _position;

    /// <summary>
    /// Current health points.
    /// </summary>
    public int Health
    {
        get => _health;
        set => this.RaiseAndSetIfChanged(ref _health, value);
    }

    /// <summary>
    /// Maximum health points.
    /// </summary>
    public int MaxHealth
    {
        get => _maxHealth;
        set => this.RaiseAndSetIfChanged(ref _maxHealth, value);
    }

    /// <summary>
    /// Player level.
    /// </summary>
    public int Level
    {
        get => _level;
        set => this.RaiseAndSetIfChanged(ref _level, value);
    }

    /// <summary>
    /// Current experience points.
    /// </summary>
    public int Experience
    {
        get => _experience;
        set => this.RaiseAndSetIfChanged(ref _experience, value);
    }

    /// <summary>
    /// Player name.
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// Player position on the map.
    /// </summary>
    public Point Position
    {
        get => _position;
        set => this.RaiseAndSetIfChanged(ref _position, value);
    }

    /// <summary>
    /// Formatted health display (e.g., "75/100").
    /// </summary>
    public string HealthDisplay => $"{Health}/{MaxHealth}";

    /// <summary>
    /// Health as a percentage (0.0 to 1.0).
    /// </summary>
    public double HealthPercentage => MaxHealth > 0 ? (double)Health / MaxHealth : 0.0;

    /// <summary>
    /// Formatted level display (e.g., "Level 5").
    /// </summary>
    public string LevelDisplay => $"Level {Level}";

    /// <summary>
    /// Updates the ViewModel properties from an ECS player entity.
    /// </summary>
    /// <param name="world">The ECS world containing the entity.</param>
    /// <param name="playerEntity">The player entity to sync from.</param>
    public void Update(World world, Entity playerEntity)
    {
        if (!world.IsAlive(playerEntity))
        {
            return;
        }

        // Update health
        if (world.TryGet<Health>(playerEntity, out var health))
        {
            Health = health.Current;
            MaxHealth = health.Maximum;
        }

        // Update experience and level
        if (world.TryGet<Experience>(playerEntity, out var experience))
        {
            Level = experience.Level;
            Experience = experience.CurrentXP;
        }

        // Update name
        if (world.TryGet<PlayerComponent>(playerEntity, out var playerComponent))
        {
            Name = playerComponent.Name;
        }

        // Update position
        if (world.TryGet<Position>(playerEntity, out var position))
        {
            Position = position.Point;
        }
    }
}

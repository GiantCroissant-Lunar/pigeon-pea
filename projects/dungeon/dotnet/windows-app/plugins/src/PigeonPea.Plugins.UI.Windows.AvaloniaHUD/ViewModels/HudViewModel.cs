using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PigeonPea.Plugins.UI.Windows.AvaloniaHUD.ViewModels;

/// <summary>
/// View model for the game HUD.
/// </summary>
public class HudViewModel : ReactiveObject
{
    private readonly BehaviorSubject<GameState> _gameStateSubject;

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    public GameState? GameState { get; private set; }

    /// <summary>
    /// Gets the collection of notifications.
    /// </summary>
    public ObservableCollection<NotificationItem> Notifications { get; }

    /// <summary>
    /// Gets the player's current health.
    /// </summary>
    [Reactive] public int PlayerHealth { get; private set; } = 100;

    /// <summary>
    /// Gets the player's maximum health.
    /// </summary>
    [Reactive] public int PlayerMaxHealth { get; private set; } = 100;

    /// <summary>
    /// Gets the player's current level.
    /// </summary>
    [Reactive] public int PlayerLevel { get; private set; } = 1;

    /// <summary>
    /// Gets the player's experience points.
    /// </summary>
    [Reactive] public long PlayerExperience { get; private set; } = 0;

    /// <summary>
    /// Gets the player's gold amount.
    /// </summary>
    [Reactive] public long PlayerGold { get; private set; } = 0;

    /// <summary>
    /// Gets the current quest name or description.
    /// </summary>
    [Reactive] public string? CurrentQuest { get; private set; }

    /// <summary>
    /// Gets the current location name.
    /// </summary>
    [Reactive] public string? CurrentLocation { get; private set; }

    /// <summary>
    /// Gets whether the inventory panel is visible.
    /// </summary>
    [Reactive] public bool IsInventoryVisible { get; private set; }

    /// <summary>
    /// Gets whether the character panel is visible.
    /// </summary>
    [Reactive] public bool IsCharacterVisible { get; private set; }

    /// <summary>
    /// Gets whether the minimap is visible.
    /// </summary>
    [Reactive] public bool IsMinimapVisible { get; private set; } = true;

    /// <summary>
    /// Gets whether debug information is visible.
    /// </summary>
    [Reactive] public bool IsDebugVisible { get; private set; }

    /// <summary>
    /// Gets the health percentage (0-100).
    /// </summary>
    public double HealthPercentage => PlayerMaxHealth > 0 ? (double)PlayerHealth / PlayerMaxHealth * 100 : 0;

    /// <summary>
    /// Gets the experience percentage to next level.
    /// </summary>
    public double ExperiencePercentage => CalculateExperiencePercentage();

    /// <summary>
    /// Initializes a new instance of HudViewModel.
    /// </summary>
    public HudViewModel()
    {
        Notifications = new ObservableCollection<NotificationItem>();
        _gameStateSubject = new BehaviorSubject<GameState>(null!);
    }

    /// <summary>
    /// Updates the view model from the current game state.
    /// </summary>
    /// <param name="gameState">The current game state.</param>
    public void UpdateFromGameState(GameState gameState)
    {
        if (gameState == null)
        {
            return;
        }

        // Update game state reference
        GameState = gameState;
        _gameStateSubject.OnNext(gameState);

        // Extract and update UI-relevant data
        // Note: This is a simplified example - real implementation would
        // parse the actual GameState structure
        UpdatePlayerStats(gameState);
        UpdateLocationInfo(gameState);
        UpdateQuestInfo(gameState);
    }

    /// <summary>
    /// Adds a notification to the HUD.
    /// </summary>
    /// <param name="id">Notification ID.</param>
    /// <param name="message">Notification message.</param>
    /// <param name="type">Notification type.</param>
    public void AddNotification(string id, string message, NotificationType type)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(message))
        {
            return;
        }

        var notification = new NotificationItem
        {
            Id = id,
            Message = message,
            Type = type,
            Timestamp = DateTime.UtcNow
        };

        // Remove existing notification with same ID
        var existing = Notifications.FirstOrDefault(n => n.Id == id);
        if (existing != null)
        {
            Notifications.Remove(existing);
        }

        // Add new notification
        Notifications.Insert(0, notification);

        // Limit notifications to prevent memory issues
        while (Notifications.Count > 10)
        {
            Notifications.RemoveAt(Notifications.Count - 1);
        }
    }

    /// <summary>
    /// Removes a notification from the HUD.
    /// </summary>
    /// <param name="id">Notification ID to remove.</param>
    public void RemoveNotification(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        var notification = Notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null)
        {
            Notifications.Remove(notification);
        }
    }

    /// <summary>
    /// Toggles the inventory panel visibility.
    /// </summary>
    public void ToggleInventory()
    {
        IsInventoryVisible = !IsInventoryVisible;
    }

    /// <summary>
    /// Toggles the character panel visibility.
    /// </summary>
    public void ToggleCharacter()
    {
        IsCharacterVisible = !IsCharacterVisible;
    }

    /// <summary>
    /// Toggles the minimap visibility.
    /// </summary>
    public void ToggleMinimap()
    {
        IsMinimapVisible = !IsMinimapVisible;
    }

    /// <summary>
    /// Toggles the debug information visibility.
    /// </summary>
    public void ToggleDebug()
    {
        IsDebugVisible = !IsDebugVisible;
    }

    /// <summary>
    /// Updates player statistics from game state.
    /// </summary>
    /// <param name="gameState">The game state.</param>
    private void UpdatePlayerStats(GameState gameState)
    {
        // This is a simplified implementation
        // In a real scenario, you'd extract actual player data from GameState
        PlayerHealth = Math.Max(0, PlayerHealth - 1); // Simulate damage
        PlayerMaxHealth = 100;
        PlayerLevel = Math.Max(1, PlayerLevel + (int)(DateTime.UtcNow.Millisecond % 100));
        PlayerExperience = PlayerExperience + 50;
        PlayerGold = PlayerGold + 10;
    }

    /// <summary>
    /// Updates location information from game state.
    /// </summary>
    /// <param name="gameState">The game state.</param>
    private void UpdateLocationInfo(GameState gameState)
    {
        // Simplified location extraction
        CurrentLocation = $"Zone {DateTime.UtcNow.Second % 10}";
    }

    /// <summary>
    /// Updates quest information from game state.
    /// </summary>
    /// <param name="gameState">The game state.</param>
    private void UpdateQuestInfo(GameState gameState)
    {
        // Simplified quest extraction
        CurrentQuest = DateTime.UtcNow.Second % 30 == 0 ? "Main Quest: Defeat the Dragon" : null;
    }

    /// <summary>
    /// Calculates experience percentage to next level.
    /// </summary>
    /// <returns>Experience percentage (0-100).</returns>
    private double CalculateExperiencePercentage()
    {
        // Simple formula: 1000 XP per level
        var currentLevelXP = PlayerLevel * 1000L;
        var nextLevelXP = (PlayerLevel + 1) * 1000L;
        var progress = PlayerExperience - currentLevelXP;
        var required = nextLevelXP - currentLevelXP;

        return required > 0 ? Math.Clamp((double)progress / required * 100, 0, 100) : 100;
    }
}

/// <summary>
/// Represents a notification item in the HUD.
/// </summary>
public class NotificationItem
{
    /// <summary>
    /// Gets or sets the notification ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets the notification timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets the display color based on notification type.
    /// </summary>
    public string Color => Type switch
    {
        NotificationType.Info => "#2196F3",      // Blue
        NotificationType.Success => "#4CAF50",    // Green
        NotificationType.Warning => "#FF9800",    // Orange
        NotificationType.Error => "#F44336",      // Red
        NotificationType.Achievement => "#9C27B0", // Purple
        NotificationType.Quest => "#FFD700",      // Gold
        NotificationType.Combat => "#FF5722",     // Deep Orange
        NotificationType.Item => "#607D8B",      // Blue Gray
        NotificationType.Character => "#795548",   // Brown
        NotificationType.Chat => "#00BCD4",       // Light Blue
        NotificationType.Network => "#009688",     // Teal
        NotificationType.System => "#9E9E9E"       // Gray
    };
}

namespace PigeonPea.Game.Contracts.UI;

/// <summary>
/// Types of UI notifications.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Informational notification (neutral).
    /// </summary>
    Info,

    /// <summary>
    /// Success notification (positive outcome).
    /// </summary>
    Success,

    /// <summary>
    /// Warning notification (caution).
    /// </summary>
    Warning,

    /// <summary>
    /// Error notification (negative outcome).
    /// </summary>
    Error,

    /// <summary>
    /// System message (debug or system status).
    /// </summary>
    System,

    /// <summary>
    /// Achievement unlocked notification.
    /// </summary>
    Achievement,

    /// <summary>
    /// Quest update notification.
    /// </summary>
    Quest,

    /// <summary>
    /// Combat related notification.
    /// </summary>
    Combat,

    /// <summary>
    /// Inventory or item related notification.
    /// </summary>
    Item,

    /// <summary>
    /// Character status update notification.
    /// </summary>
    Character,

    /// <summary>
    /// Chat or dialogue notification.
    /// </summary>
    Chat,

    /// <summary>
    /// Network or multiplayer notification.
    /// </summary>
    Network
}

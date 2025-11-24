namespace PigeonPea.Game.Contracts.UI;

using PigeonPea.Game.Contracts.Models;

/// <summary>
/// User interface contract for game UI/HUD plugins.
/// </summary>
public interface IUserInterface
{
    /// <summary>
    /// Unique identifier for the UI implementation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// UI capabilities supported by this implementation.
    /// </summary>
    UICapabilities Capabilities { get; }

    /// <summary>
    /// Initialize the UI system with the given context.
    /// </summary>
    /// <param name="context">UI initialization context.</param>
    void Initialize(UIContext context);

    /// <summary>
    /// Update UI elements based on current game state.
    /// </summary>
    /// <param name="state">Current game state.</param>
    void Update(GameState state);

    /// <summary>
    /// Show a specific UI panel or screen.
    /// </summary>
    /// <param name="panelId">Identifier of the panel to show.</param>
    void ShowPanel(string panelId);

    /// <summary>
    /// Hide a specific UI panel or screen.
    /// </summary>
    /// <param name="panelId">Identifier of the panel to hide.</param>
    void HidePanel(string panelId);

    /// <summary>
    /// Show a notification message to the user.
    /// </summary>
    /// <param name="message">The notification message.</param>
    /// <param name="type">Type of notification.</param>
    void ShowNotification(string message, NotificationType type);

    /// <summary>
    /// Get the root UI control for embedding in the host application.
    /// </summary>
    /// <returns>The root control object (framework-specific).</returns>
    object GetRootControl();

    /// <summary>
    /// Shutdown and cleanup UI resources.
    /// </summary>
    void Shutdown();
}

namespace PigeonPea.Game.Contracts.UI;

/// <summary>
/// Context for UI initialization and configuration.
/// </summary>
public class UIContext
{
    /// <summary>
    /// Service provider for dependency injection.
    /// </summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Width of the UI area.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the UI area.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// UI theme configuration.
    /// </summary>
    public UITheme? Theme { get; set; }

    /// <summary>
    /// Whether the UI should be in debug mode (shows additional info).
    /// </summary>
    public bool IsDebugMode { get; set; }

    /// <summary>
    /// Target frames per second for UI updates.
    /// </summary>
    public int TargetFPS { get; set; } = 60;

    /// <summary>
    /// Scale factor for UI elements.
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>
    /// Whether animations should be enabled.
    /// </summary>
    public bool EnableAnimations { get; set; } = true;

    /// <summary>
    /// Custom configuration data.
    /// </summary>
    public Dictionary<string, object> CustomData { get; } = new();
}

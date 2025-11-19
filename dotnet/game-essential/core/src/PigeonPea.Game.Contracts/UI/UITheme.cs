namespace PigeonPea.Game.Contracts.UI;

/// <summary>
/// UI theme configuration for visual styling.
/// </summary>
public class UITheme
{
    /// <summary>
    /// Unique identifier for the theme.
    /// </summary>
    public string Id { get; set; } = "default";

    /// <summary>
    /// Display name of the theme.
    /// </summary>
    public string Name { get; set; } = "Default Theme";

    /// <summary>
    /// Primary color for UI elements.
    /// </summary>
    public string PrimaryColor { get; set; } = "#0078D7";

    /// <summary>
    /// Secondary color for UI elements.
    /// </summary>
    public string SecondaryColor { get; set; } = "#FF6B35";

    /// <summary>
    /// Background color for UI panels.
    /// </summary>
    public string BackgroundColor { get; set; } = "#1E1E1E";

    /// <summary>
    /// Text color for UI elements.
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Accent color for highlights and active elements.
    /// </summary>
    public string AccentColor { get; set; } = "#00BC8C";

    /// <summary>
    /// Border color for UI elements.
    /// </summary>
    public string BorderColor { get; set; } = "#3C3C3C";

    /// <summary>
    /// Font family for UI text.
    /// </summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>
    /// Font size for UI text.
    /// </summary>
    public double FontSize { get; set; } = 14.0;

    /// <summary>
    /// Whether the theme is dark mode.
    /// </summary>
    public bool IsDarkTheme { get; set; } = true;

    /// <summary>
    /// Border radius for UI elements.
    /// </summary>
    public double BorderRadius { get; set; } = 4.0;

    /// <summary>
    /// Opacity for UI panels (0.0 to 1.0).
    /// </summary>
    public double PanelOpacity { get; set; } = 0.9;

    /// <summary>
    /// Custom theme properties.
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();

    /// <summary>
    /// Creates a default light theme.
    /// </summary>
    public static UITheme CreateLightTheme()
    {
        return new UITheme
        {
            Id = "light",
            Name = "Light Theme",
            PrimaryColor = "#0078D7",
            SecondaryColor = "#6C757D",
            BackgroundColor = "#FFFFFF",
            TextColor = "#212529",
            AccentColor = "#0078D7",
            BorderColor = "#DEE2E6",
            IsDarkTheme = false
        };
    }

    /// <summary>
    /// Creates a default dark theme.
    /// </summary>
    public static UITheme CreateDarkTheme()
    {
        return new UITheme
        {
            Id = "dark",
            Name = "Dark Theme",
            PrimaryColor = "#FFFFFF",
            SecondaryColor = "#9E9E9E",
            BackgroundColor = "#1E1E1E",
            TextColor = "#FFFFFF",
            AccentColor = "#00BC8C",
            BorderColor = "#3C3C3C",
            IsDarkTheme = true
        };
    }
}

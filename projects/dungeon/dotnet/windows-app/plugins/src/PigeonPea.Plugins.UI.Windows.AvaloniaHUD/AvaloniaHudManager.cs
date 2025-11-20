using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Hud.Services;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.UI;
using PigeonPea.Plugin;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;

namespace PigeonPea.Plugins.UI.Windows.AvaloniaHUD;

/// <summary>
/// Avalonia-based HUD manager implementing both game and app UI contracts.
/// </summary>
public class AvaloniaHudManager : IUserInterface, IService
{
    private readonly ILogger<AvaloniaHudManager> _logger;
    private readonly Dictionary<string, Control> _panels;
    private readonly Dictionary<string, DateTime> _activeNotifications;
    private UIContext? _context;
    private GameHUD? _mainHud;
    private HudViewModel? _viewModel;

    /// <summary>
    /// Gets the unique identifier for this UI implementation.
    /// </summary>
    public string Id { get; } = "avalonia-hud-windows";

    /// <summary>
    /// Gets the UI capabilities supported by this implementation.
    /// </summary>
    public UICapabilities Capabilities { get; } =
        UICapabilities.HUD |
        UICapabilities.Menus |
        UICapabilities.Dialogs |
        UICapabilities.Tooltips |
        UICapabilities.Notifications |
        UICapabilities.Inventory |
        UICapabilities.CharacterStatus |
        UICapabilities.Animations |
        UICapabilities.Theming;

    /// <summary>
    /// Gets the root UI control for embedding.
    /// </summary>
    public Control? RootControl { get; private set; }

    /// <summary>
    /// Initializes a new instance of AvaloniaHudManager.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AvaloniaHudManager(ILogger<AvaloniaHudManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _panels = new Dictionary<string, Control>();
        _activeNotifications = new Dictionary<string, DateTime>();
    }

    /// <summary>
    /// Initialize the UI system with the given context.
    /// </summary>
    /// <param name="context">UI initialization context.</param>
    public void Initialize(UIContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _logger.LogInformation("Initializing Avalonia HUD Manager with {Width}x{Height}",
            context.Width, context.Height);

        // Initialize Avalonia if not already running
        if (Application.Current == null)
        {
            var builder = BuildAvaloniaApp();
            var app = builder.Build();

            // Don't start the application, just build it for control creation
            app.SetupWithoutStarting();
        }

        // Create view model
        _viewModel = new HudViewModel();

        // Create main HUD control
        _mainHud = new GameHUD
        {
            DataContext = _viewModel,
            Width = context.Width,
            Height = context.Height
        };

        // Apply theme if provided
        if (context.Theme != null)
        {
            ApplyTheme(context.Theme);
        }

        RootControl = _mainHud;

        _logger.LogInformation("Avalonia HUD Manager initialized successfully");
    }

    /// <summary>
    /// Update UI elements based on current game state.
    /// </summary>
    /// <param name="state">Current game state.</param>
    public void Update(GameState state)
    {
        if (_viewModel == null || state == null)
        {
            _logger.LogWarning("Cannot update UI: ViewModel or GameState is null");
            return;
        }

        AvaloniaThreadSafetyExtensions.EnsureOnUiThread(() =>
        {
            _viewModel.UpdateFromGameState(state);
        });

        _logger.LogTrace("UI updated with game state");
    }

    /// <summary>
    /// Show a specific UI panel or screen.
    /// </summary>
    /// <param name="panelId">Identifier of the panel to show.</param>
    public void ShowPanel(string panelId)
    {
        if (string.IsNullOrEmpty(panelId))
        {
            _logger.LogWarning("Cannot show panel: panelId is null or empty");
            return;
        }

        AvaloniaThreadSafetyExtensions.EnsureOnUiThread(() =>
        {
            if (_panels.TryGetValue(panelId, out var panel))
            {
                panel.IsVisible = true;
                panel.ZIndex = 1000; // Bring to front
                _logger.LogDebug("Panel {PanelId} shown", panelId);
            }
            else
            {
                _logger.LogWarning("Panel {PanelId} not found", panelId);
            }
        });
    }

    /// <summary>
    /// Hide a specific UI panel or screen.
    /// </summary>
    /// <param name="panelId">Identifier of the panel to hide.</param>
    public void HidePanel(string panelId)
    {
        if (string.IsNullOrEmpty(panelId))
        {
            _logger.LogWarning("Cannot hide panel: panelId is null or empty");
            return;
        }

        AvaloniaThreadSafetyExtensions.EnsureOnUiThread(() =>
        {
            if (_panels.TryGetValue(panelId, out var panel))
            {
                panel.IsVisible = false;
                panel.ZIndex = 0;
                _logger.LogDebug("Panel {PanelId} hidden", panelId);
            }
            else
            {
                _logger.LogWarning("Panel {PanelId} not found", panelId);
            }
        });
    }

    /// <summary>
    /// Show a notification message to the user.
    /// </summary>
    /// <param name="message">The notification message.</param>
    /// <param name="type">Type of notification.</param>
    public void ShowNotification(string message, NotificationType type)
    {
        if (string.IsNullOrEmpty(message))
        {
            _logger.LogWarning("Cannot show notification: message is null or empty");
            return;
        }

        var notificationId = Guid.NewGuid().ToString("N")[..8];

        AvaloniaThreadSafetyExtensions.EnsureOnUiThread(() =>
        {
            _viewModel?.AddNotification(notificationId, message, type);
        });

        _activeNotifications[notificationId] = DateTime.UtcNow;

        _logger.LogDebug("Notification shown: {Message} ({Type})", message, type);

        // Auto-hide notification after 5 seconds
        Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
        {
            HideMessage(notificationId);
        }, TaskScheduler.Default);
    }

    /// <summary>
    /// Gets the root UI control for embedding in the host application.
    /// </summary>
    /// <returns>The root control object (framework-specific).</returns>
    public object GetRootControl()
    {
        return RootControl ?? throw new InvalidOperationException("UI not initialized. Call Initialize() first.");
    }

    /// <summary>
    /// Show a message (IService implementation).
    /// </summary>
    /// <param name="messageId">Message identifier.</param>
    public void ShowMessage(string messageId)
    {
        ShowNotification(messageId, NotificationType.Info);
    }

    /// <summary>
    /// Hide a message (IService implementation).
    /// </summary>
    /// <param name="messageId">Message identifier.</param>
    public void HideMessage(string messageId)
    {
        if (string.IsNullOrEmpty(messageId))
        {
            return;
        }

        AvaloniaThreadSafetyExtensions.EnsureOnUiThread(() =>
        {
            _viewModel?.RemoveNotification(messageId);
        });

        _activeNotifications.Remove(messageId);
        _logger.LogDebug("Message hidden: {MessageId}", messageId);
    }

    /// <summary>
    /// Shutdown and cleanup UI resources.
    /// </summary>
    public void Shutdown()
    {
        _logger.LogInformation("Shutting down Avalonia HUD Manager");

        AvaloniaThreadSafetyExtensions.EnsureOnUiThread(() =>
        {
            if (RootControl != null)
            {
                RootControl.DataContext = null;
                RootControl = null;
            }

            _panels.Clear();
            _activeNotifications.Clear();
            _viewModel = null;
            _mainHud = null;
        });

        _logger.LogInformation("Avalonia HUD Manager shutdown complete");
    }

    /// <summary>
    /// Register a panel for management.
    /// </summary>
    /// <param name="panelId">Panel identifier.</param>
    /// <param name="panel">Panel control.</param>
    internal void RegisterPanel(string panelId, Control panel)
    {
        if (string.IsNullOrEmpty(panelId) || panel == null)
        {
            return;
        }

        _panels[panelId] = panel;
        _logger.LogDebug("Panel registered: {PanelId}", panelId);
    }

    /// <summary>
    /// Build the Avalonia application configuration.
    /// </summary>
    /// <returns>Avalonia app builder.</returns>
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    }

    /// <summary>
    /// Apply theme settings to the UI.
    /// </summary>
    /// <param name="theme">Theme to apply.</param>
    private void ApplyTheme(UITheme theme)
    {
        if (RootControl == null)
        {
            return;
        }

        // This would be implemented to apply theme colors and styles
        // For now, just log the theme application
        _logger.LogDebug("Applying theme: {ThemeId}", theme.Id);
    }
}

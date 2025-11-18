using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Game.Inventory;
using PigeonPea.Shared;
using PigeonPea.Shared.ViewModels;
using Terminal.Gui;
using IInventoryService = PigeonPea.Game.Contracts.Inventory.Services.IService;

namespace PigeonPea.Plugins.Hud.TerminalGui;

public class TerminalGuiHud : IGameHud
{
    private ILogger? _logger;
    private HudContext? _context;
    private IRenderer? _renderer;

    public string Id => "terminal-gui-hud";

    public void Initialize(HudContext context)
    {
        _context = context;
        _logger = context.Services.GetService(typeof(ILogger<TerminalGuiHud>)) as ILogger<TerminalGuiHud>;
        _logger?.LogInformation("Terminal.Gui HUD initialized");
    }

    public void Run(GameState initialState)
    {
        Application.Init();
        try
        {
            var hudLogLines = new List<string>();
            InventoryViewModel? inventoryViewModel = null;

            // Discover HUD panel descriptors contributed by plugins
            try
            {
                if (_context?.Services.GetService(typeof(IRegistry)) is IRegistry registry)
                {
                    var discoverMessage = "[TerminalGuiHud] Discovering HUD panels via IRegistry...";
                    _logger?.LogInformation(discoverMessage);
                    hudLogLines.Add(discoverMessage);
                    var panels = new List<HudPanelDescriptor>();
                    foreach (var provider in registry.GetAll<IHudPanelDescriptorProvider>())
                    {
                        foreach (var panel in provider.GetPanels())
                        {
                            panels.Add(panel);
                        }
                    }

                    if (panels.Count == 0)
                    {
                        _logger?.LogInformation("No HUD panels discovered via IHudPanelDescriptorProvider.");
                        var msg = "[TerminalGuiHud] No HUD panels discovered.";
                        hudLogLines.Add(msg);
                    }
                    else
                    {
                        foreach (var panel in panels.OrderBy(p => p.Order))
                        {
                            _logger?.LogInformation(
                                "HUD panel: {Id} ({Name}), region={Region}, order={Order}",
                                panel.Id,
                                panel.DisplayName,
                                panel.Region,
                                panel.Order);
                            var msg = $"[TerminalGuiHud] HUD panel: {panel.Id} ({panel.DisplayName}), region={panel.Region}, order={panel.Order}";
                            hudLogLines.Add(msg);
                        }
                    }

                    // Resolve inventory service and view model if available
                    if (registry.IsRegistered<IInventoryService>())
                    {
                        var inventoryService = registry.Get<IInventoryService>();

                        hudLogLines.Add($"[TerminalGuiHud] Inventory service available: {inventoryService.GetType().FullName}");

                        inventoryViewModel = _context?.Services.GetService(typeof(InventoryViewModel)) as InventoryViewModel;

                        if (inventoryViewModel != null)
                        {
                            hudLogLines.Add("[TerminalGuiHud] Inventory demo is currently disabled (no GameWorld integration).");
                        }
                    }
                    else
                    {
                        hudLogLines.Add("[TerminalGuiHud] No inventory service registered in registry.");
                    }

                    // Resolve renderer plugin if available (e.g., ANSI terminal renderer)
                    if (registry.IsRegistered<IRenderer>())
                    {
                        _renderer = registry.Get<IRenderer>();
                        var rendererType = _renderer.GetType().FullName ?? "(unknown)";
                        _logger?.LogInformation("[TerminalGuiHud] Renderer plugin detected: {RendererType}", rendererType);
                        hudLogLines.Add($"[TerminalGuiHud] Renderer plugin detected: {rendererType}");
                    }
                    else
                    {
                        hudLogLines.Add("[TerminalGuiHud] No renderer plugin registered in registry.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error while discovering HUD panels.");
                hudLogLines.Add($"[TerminalGuiHud] Error while discovering HUD panels: {ex.Message}");
            }

            var top = new Toplevel
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Main content frame (inventory or placeholder), leaves space at bottom for log
            FrameView mainFrame;
            if (inventoryViewModel != null && inventoryViewModel.Slots.Count > 0)
            {
                mainFrame = new FrameView
                {
                    Title = "Inventory",
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(7)
                };

                var slotLines = inventoryViewModel.Slots
                    .Select(slot => slot.IsEmpty
                        ? $"[{slot.SlotIndex}] (empty)"
                        : $"[{slot.SlotIndex}] {slot.DefinitionId} x{slot.Quantity}")
                    .ToList();

                var listView = new ListView
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                listView.SetSource(new ObservableCollection<string>(slotLines));

                mainFrame.Add(listView);
                hudLogLines.Add("[TerminalGuiHud] Active panel: Inventory");
            }
            else
            {
                mainFrame = new FrameView
                {
                    Title = "Pigeon Pea HUD (Terminal.Gui)",
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(7)
                };

                var label = new Label
                {
                    Text = "Terminal.Gui HUD plugin placeholder",
                    X = Pos.Center(),
                    Y = Pos.Center()
                };

                mainFrame.Add(label);
                hudLogLines.Add("[TerminalGuiHud] Active panel: Placeholder");
            }

            top.Add(mainFrame);

            // Log panel at the bottom
            var logFrame = new FrameView
            {
                Title = "Log",
                X = 0,
                Y = Pos.Bottom(mainFrame),
                Width = Dim.Fill(),
                Height = 7
            };

            var logView = new TextView
            {
                ReadOnly = true,
                WordWrap = true,
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            // Summarize useful keybindings for the user
            hudLogLines.Add("[TerminalGuiHud] Keybindings:");
            hudLogLines.Add("[TerminalGuiHud]   Tab: switch focus between panels (inventory/log)");
            hudLogLines.Add("[TerminalGuiHud]   Up/Down: navigate list items in the focused panel");
            hudLogLines.Add("[TerminalGuiHud]   Use your terminal window controls or Ctrl+C to exit run");

            if (hudLogLines.Count == 0)
            {
                hudLogLines.Add("[TerminalGuiHud] (no HUD logs)");
            }

            logView.Text = string.Join(Environment.NewLine, hudLogLines) + Environment.NewLine;

            logFrame.Add(logView);
            top.Add(logFrame);

            Application.Run(top);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    public void Shutdown()
    {
        _logger?.LogInformation("Terminal.Gui HUD shutting down");
    }
}

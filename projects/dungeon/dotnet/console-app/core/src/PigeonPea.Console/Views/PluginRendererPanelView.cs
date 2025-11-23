using PigeonPea.Game.Contracts.Models;
using GameRenderContext = PigeonPea.Game.Contracts.Rendering.RenderContext;
using GameRenderer = PigeonPea.Game.Contracts.Rendering.IRenderer;
using RenderSurface = PigeonPea.Game.Contracts.Rendering.IRenderSurface;
using SharedRenderer = PigeonPea.Rendering.Contracts.IRenderer;
using SharedRenderTarget = PigeonPea.Rendering.Contracts.IRenderTarget;
using PigeonPea.Contracts.Input.Services;
using SadRogue.Primitives;
using Terminal.Gui;
using Arch.Core;
using PigeonPea.Shared.Components;
using IPersistenceService = PigeonPea.Game.Contracts.Persistence.Services.IService;
using IStatsService = PigeonPea.Game.Contracts.Stats.Services.IService;
using IAvatarService = PigeonPea.Game.Contracts.Avatar.Services.IService;
using IInventoryService = PigeonPea.Game.Contracts.Inventory.Services.IService;

namespace PigeonPea.Console;

public partial class PluginRendererPanelView : View
{
    private readonly GameRenderer _pluginRenderer;
    private readonly GameRenderContext _renderContext;
    private readonly GameState _gameState;
    private readonly SharedRenderer _surfaceRenderer;
    private readonly SharedRenderTarget _renderTarget;
    private readonly RenderSurface _surfaceAdapter;
    private readonly IService? _inputService;
    private readonly DungeonInputHandler? _inputHandler;
    private readonly DungeonInputState _inputState = new DungeonInputState();
    private readonly World? _world;
    private readonly IPersistenceService? _persistenceService;
    private readonly IStatsService? _statsService;
    private readonly IAvatarService? _avatarService;
    private readonly IInventoryService? _inventoryService;
    private bool _paused;
    private bool _initialized;

    public PluginRendererPanelView(
        GameRenderer pluginRenderer,
        GameRenderContext renderContext,
        GameState gameState,
        IService? inputService,
        World? world = null,
        IPersistenceService? persistenceService = null,
        IStatsService? statsService = null,
        IAvatarService? avatarService = null,
        IInventoryService? inventoryService = null)
    {
        _pluginRenderer = pluginRenderer;
        _renderContext = renderContext;
        _gameState = gameState;
        _inputService = inputService;
        _world = world;
        _persistenceService = persistenceService;
        _statsService = statsService;
        _avatarService = avatarService;
        _inventoryService = inventoryService;

        _surfaceRenderer = new TerminalGuiRenderer(new PigeonPea.Console.Rendering.AsciiRenderer(true));
        _renderTarget = new TerminalGuiRenderTarget(this);
        _surfaceRenderer.Initialize(_renderTarget);

        _surfaceAdapter = new PluginRenderSurfaceAdapter(_surfaceRenderer);
        _renderContext.Surface = _surfaceAdapter;

        if (_inputService != null)
        {
            _inputHandler = new DungeonInputHandler(_inputService);
        }

        _initialized = false;
    }

    protected override bool OnDrawingContent()
    {
        var width = Viewport.Width;
        var height = Viewport.Height;
        if (width <= 0 || height <= 0)
        {
            return true;
        }

        _renderContext.Width = width;
        _renderContext.Height = height;

        if (_surfaceRenderer is TerminalGuiRenderer tgui)
        {
            tgui.SetDriver(Driver);
        }

        // Update player position from input before rendering
        UpdatePlayerFromInput();
        UpdateGameState();

        if (!_initialized)
        {
            _pluginRenderer.Initialize(_renderContext);
            _initialized = true;
        }

        _pluginRenderer.Render(_gameState);

        DrawDebugHud();
        return true;
    }
}

public partial class PluginRendererPanelView
{
    private void UpdatePlayerFromInput()
    {
        if (_inputHandler == null)
        {
            return;
        }

        _inputHandler.Update(_gameState, _inputState);

        if (_inputState.PauseJustPressed)
        {
            _paused = !_paused;
        }

        if (_inputState.SaveJustPressed && _world != null && _persistenceService != null)
        {
            _persistenceService.SaveWorld(_world, "quicksave");
        }

        if (_inputState.LoadJustPressed && _world != null && _persistenceService != null)
        {
            _persistenceService.LoadWorld(_world, "quicksave");
        }
    }

    private void DrawDebugHud()
    {
        if (Driver == null)
        {
            return;
        }

        var status = _paused ? "PAUSED" : "RUNNING";
        var text = $"[{status}] Atk:{_inputState.AttackPressed} Int:{_inputState.InteractPressed} Inv:{_inputState.InventoryPressed} Pause:{_inputState.PausePressed} Save:{_inputState.SavePressed} Load:{_inputState.LoadPressed}";

        Driver.Move(0, 0);
        Driver.AddStr(text);
    }

    private void UpdateGameState()
    {
        if (_world == null) return;

        // Find player entity
        var query = new QueryDescription().WithAll<PlayerComponent>();
        Entity playerEntity = Entity.Null;

        _world.Query(in query, (Entity entity) =>
        {
            playerEntity = entity;
        });

        if (playerEntity != Entity.Null)
        {
            if (_statsService != null)
            {
                _gameState.Stats = _statsService.GetStats(_world, playerEntity);
            }
            if (_avatarService != null)
            {
                _gameState.Avatar = _avatarService.GetAvatar(_world, playerEntity);
            }
            if (_inventoryService != null)
            {
                _gameState.Inventory = _inventoryService.GetInventory(playerEntity);
            }
        }
    }
}

public class PluginRenderSurfaceAdapter : RenderSurface
{
    private readonly SharedRenderer _renderer;

    public PluginRenderSurfaceAdapter(SharedRenderer renderer)
    {
        _renderer = renderer;
    }

    public void BeginFrame()
    {
        _renderer.BeginFrame();
    }

    public void EndFrame()
    {
        _renderer.EndFrame();
    }

    public void Clear(byte r, byte g, byte b)
    {
        _renderer.Clear(new SadRogue.Primitives.Color(r, g, b));
    }

    public void SetViewport(int x, int y, int width, int height)
    {
        _renderer.SetViewport(new PigeonPea.Rendering.Contracts.Viewport(x, y, width, height));
    }

    public void DrawText(int x, int y, string text, byte foregroundR, byte foregroundG, byte foregroundB, byte backgroundR, byte backgroundG, byte backgroundB)
    {
        var fg = new SadRogue.Primitives.Color(foregroundR, foregroundG, foregroundB);
        var bg = new SadRogue.Primitives.Color(backgroundR, backgroundG, backgroundB);
        _renderer.DrawText(x, y, text, fg, bg);
    }
}

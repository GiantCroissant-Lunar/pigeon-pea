using System;
using MessagePipe;
using ObservableCollections;
using PigeonPea.Shared.Events;
using PigeonPea.Shared.Scale;
using ReactiveUI;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// HUD-facing view model for scale and mode selection.
/// Wraps ScaleModeController and exposes reactive properties so
/// UI layers (like TerminalHudApplication) can bind or call into it.
/// </summary>
public sealed class HudScaleViewModel : ReactiveObject
{
    private readonly ScaleModeController _controller;
    private readonly IPublisher<ScaleModeChangedEvent>? _modeChangedPublisher;

    /// <summary>
    /// Available modes that the HUD can cycle through.
    /// Kept as an observable list for potential future binding.
    /// </summary>
    public ObservableList<ScaleMode> AvailableModes { get; }

    public HudScaleViewModel(IPublisher<ScaleModeChangedEvent>? modeChangedPublisher = null)
    {
        _controller = new ScaleModeController(ScaleRegistry.Default);
        _modeChangedPublisher = modeChangedPublisher;

        AvailableModes = new ObservableList<ScaleMode>
        {
            ScaleMode.World,
            ScaleMode.DungeonFine,
            ScaleMode.DungeonCoarse
        };
    }

    /// <summary>
    /// Current logical mode (world / dungeon-fine / dungeon-coarse).
    /// </summary>
    public ScaleMode Mode => _controller.CurrentMode;

    /// <summary>
    /// Scale configuration associated with the current mode.
    /// </summary>
    public ScaleConfig CurrentScale => _controller.CurrentScale;

    /// <summary>
    /// Advance to the next mode in a simple cycle:
    /// World -> DungeonFine -> DungeonCoarse -> World.
    /// Publishes a ScaleModeChangedEvent via MessagePipe when the mode changes.
    /// </summary>
    public void CycleMode()
    {
        var previous = _controller.CurrentMode;
        var trigger = previous switch
        {
            ScaleMode.World => ScaleTrigger.EnterDungeonFine,
            ScaleMode.DungeonFine => ScaleTrigger.EnterDungeonCoarse,
            ScaleMode.DungeonCoarse => ScaleTrigger.ReturnToWorld,
            _ => ScaleTrigger.ReturnToWorld
        };

        _controller.Fire(trigger);

        // Notify bindings that mode/scale have changed
        this.RaisePropertyChanged(nameof(Mode));
        this.RaisePropertyChanged(nameof(CurrentScale));

        _modeChangedPublisher?.Publish(new ScaleModeChangedEvent
        {
            OldMode = previous,
            NewMode = _controller.CurrentMode
        });
    }
}

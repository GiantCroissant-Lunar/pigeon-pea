using System;
using Stateless;

namespace PigeonPea.Shared.Scale;

/// <summary>
/// Logical rendering/interaction modes (not the full game state machine yet).
/// This sits on top of ScaleConfig so each mode has an associated physical scale.
/// </summary>
public enum ScaleMode
{
    World,
    DungeonFine,
    DungeonCoarse
}

public enum ScaleTrigger
{
    EnterDungeonFine,
    EnterDungeonCoarse,
    ReturnToWorld
}

/// <summary>
/// Small wrapper around Stateless to provide a mode + associated ScaleConfig.
/// For now this is HUD-local and starts in World mode; later we can thread
/// triggers from gameplay (enter dungeon, exit, vehicles, etc.).
/// </summary>
public sealed class ScaleModeController
{
    private readonly StateMachine<ScaleMode, ScaleTrigger> _fsm;
    private readonly ScaleRegistry _registry;

    public ScaleModeController(ScaleRegistry registry, ScaleMode initialMode = ScaleMode.World)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _fsm = new StateMachine<ScaleMode, ScaleTrigger>(initialMode);

        ConfigureStateMachine(_fsm);
    }

    public ScaleMode CurrentMode => _fsm.State;

    public ScaleConfig CurrentScale => CurrentMode switch
    {
        ScaleMode.World => _registry.Get("world"),
        ScaleMode.DungeonFine => _registry.Get("dungeon-fine"),
        ScaleMode.DungeonCoarse => _registry.Get("dungeon-coarse"),
        _ => _registry.Get("world")
    };

    public void Fire(ScaleTrigger trigger) => _fsm.Fire(trigger);

    private static void ConfigureStateMachine(StateMachine<ScaleMode, ScaleTrigger> fsm)
    {
        // World -> Dungeon
        fsm.Configure(ScaleMode.World)
           .Permit(ScaleTrigger.EnterDungeonFine, ScaleMode.DungeonFine)
           .Permit(ScaleTrigger.EnterDungeonCoarse, ScaleMode.DungeonCoarse);

        // DungeonFine -> World or DungeonCoarse
        fsm.Configure(ScaleMode.DungeonFine)
           .Permit(ScaleTrigger.ReturnToWorld, ScaleMode.World)
           .Permit(ScaleTrigger.EnterDungeonCoarse, ScaleMode.DungeonCoarse);

        // DungeonCoarse -> World or DungeonFine
        fsm.Configure(ScaleMode.DungeonCoarse)
           .Permit(ScaleTrigger.ReturnToWorld, ScaleMode.World)
           .Permit(ScaleTrigger.EnterDungeonFine, ScaleMode.DungeonFine);
    }
}

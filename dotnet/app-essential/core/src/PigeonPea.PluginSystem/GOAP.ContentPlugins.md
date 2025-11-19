# GOAP Content Plugin Integration Design

This document captures the planned integration between the **plugin system** and the
**GOAP/AI runtime** so that content-authoring projects can provide game-specific
GOAP goals, actions, and executors without breaking layering.

The intent is for the plugin-system side to implement discovery and DI wiring,
while game-essential (`PigeonPea.Game.AI`) remains engine-side and unaware of
content-authoring assemblies.

---

## 1. Goal

**High-level goal**

- Enable **content-authoring** to define and own **game-specific GOAP content**:
  - GOAP goals
  - GOAP actions
  - Action executors
- Keep **layering intact**:
  - `game-essential` (e.g. `PigeonPea.Game.AI`) is engine-side and **does not reference** content-authoring.
  - Content-authoring assemblies reference `PigeonPea.Game.AI` (and other low-level contracts) and are loaded as plugins.
- At runtime, **plugins from content-authoring inject GOAP content** into
  `PigeonPea.Game.AI` via **plugin + DI infrastructure**.

Key constraint:

> Game-essential must not reference content-authoring.


---

## 2. Existing GOAP runtime (engine side)

All of this already exists in **game-essential** (and is considered the
"consumer" side for GOAP content):

- Core (from `_lib/nexus-goap`):
  - `WorldState.WorldState`
  - `GoapAction` (Preconditions, Effects, Cost, `IActionExecutor? Executor`)
  - `GoapGoal` (DesiredState, Priority, `IGoalEvaluator? Evaluator`)
  - `Planner`, `Plan`, `PlanningResult`

- ECS integration (e.g. `PigeonPea.Game.AI`):
  - **Components**:
    - `GoapAgentComponent` – `AvailableGoals`, `AvailableActions`, `NeedsReplan`, `PlanningFrequency`.
    - `GoalComponent` – `CurrentGoal`, `LastEvaluationTime`.
    - `PlanComponent` – `CurrentPlan`, `CurrentActionIndex`, `LastPlanningTime`.
  - **Adapter**:
    - `PerceptionToWorldStateAdapter` – converts `PerceptionData + Entity` into a
      `WorldState` snapshot (using Vision/Hearing/Knowledge/Awareness).
  - **Systems**:
    - `GoalEvaluationSystem.Update(world, currentTime, evaluationInterval)`
    - `PlanningSystem.Update(world, currentTime, planningInterval)`
    - `ActionExecutionSystem.Update(world)` – calls `action.Executor.Execute(entity, action)`.
    - `PlanMonitoringSystem.Update(world, currentTime)`

From the GOAP side, everything is ready to **consume** goals/actions; what we
need is a plugin-friendly mechanism to **supply** them from content-authoring.


---

## 3. Desired plugin model (high level)

### 3.1 Contracts (engine-side, low-level)

Contracts are defined in a low-level project reachable by both game-essential
and content-authoring (likely `PigeonPea.Game.AI` or a close sibling). Example
interfaces:

```csharp
public interface IGoapArchetype
{
    string ArchetypeId { get; }

    // Called when an entity with this archetype is created.
    void ConfigureAgent(
        Entity entity,
        ref GoapAgentComponent agent,
        ref GoalComponent goal,
        ref PlanComponent plan);
}
```

Optionally, a higher-level module contract:

```csharp
public interface IGoapContentModule
{
    void Register(IGoapRegistry registry);
}

public interface IGoapRegistry
{
    void RegisterGoalFactory(string id, Func<GoapGoal> factory);
    void RegisterActionFactory(string id, Func<GoapAction> factory);
}
```

These contracts:

- Live on the engine side (no dependency on content-authoring).
- Are **implemented by content-authoring assemblies**.


### 3.2 Implementations in content-authoring

Content-authoring projects:

- Reference `PigeonPea.Game.AI` (and other needed contracts).
- Implement `IGoapArchetype` and/or `IGoapContentModule`.

Examples:

```csharp
public sealed class GoblinMeleeArchetype : IGoapArchetype { ... }
public sealed class SkeletonArcherArchetype : IGoapArchetype { ... }
```

Each archetype:

- Creates and configures **game-specific** `GoapGoal` instances, e.g.:
  - `EngageTargetGoal`
  - `SurviveGoal`
  - `PatrolGoal`
- Creates **game-specific** `GoapAction` instances with wired executors, e.g.:
  - `MoveToPositionAction`
  - `AttackTargetAction`
  - `FleeFromThreatAction`
  - `UseHealthPotionAction`
- Assigns them into `GoapAgentComponent.AvailableGoals` / `.AvailableActions`.
- Optionally sets an `IGoalEvaluator` for dynamic priorities.

`IGoapContentModule` implementations can perform more coarse-grained
registration in a central `IGoapRegistry` (e.g., shared factories, common
actions/goals reused across archetypes).


### 3.3 Runtime discovery & DI wiring

At startup, **plugin host responsibilities** include:

- Load content assemblies (from plugin folders / config) via existing
  `PluginLoader` infrastructure.
- Discover GOAP-related types in those assemblies:
  - All implementations of `IGoapArchetype`.
  - All implementations of `IGoapContentModule`.
  - All implementations of `IActionExecutor` (see section 4).
- Register them into the existing DI pipeline
  (`IServiceCollection` / `IAppServiceRoot`) so they can be resolved per
  archetype ID / executor ID.

At **entity spawn** (or when applying an archetype):

- Some central system knows the **archetype ID string** for the entity.
- It resolves the corresponding `IGoapArchetype` from DI and calls
  `ConfigureAgent` to populate GOAP components.
- From that point on, GOAP systems operate purely on data (`GoapAgentComponent`,
  `GoalComponent`, `PlanComponent`, `GoapGoal`, `GoapAction`).

This keeps `game-essential` agnostic of which concrete goals/actions exist,
while content-authoring defines them.


---

## 4. Action executors via DI

`GoapAction` already has:

```csharp
public IActionExecutor? Executor { get; set; }
```

We want plugin content to provide **executors** that call into game systems
(movement, combat, abilities, pathfinding, etc.).

### 4.1 Engine-side factory contract

Define in game-essential (or another low-level shared project):

```csharp
public interface IGoapActionExecutorFactory
{
    IActionExecutor GetExecutor(string executorId);
}
```

### 4.2 Content plugin responsibilities

Content plugins:

- Implement specific `IActionExecutor` classes, e.g.:
  - `MoveToPositionExecutor` – uses pathfinding/dungeon control.
  - `AttackTargetExecutor` – uses ability system / combat.
  - `FleeExecutor`, `UseHealthPotionExecutor`, etc.
- Register these executors in DI and expose them through
  `IGoapActionExecutorFactory` (e.g. via a dictionary keyed by executor ID).

### 4.3 Archetype usage

When building a `GoapAction`, archetypes:

- Call `factory.GetExecutor("move_to_target")` or similar, and
- Assign the result to `action.Executor`.

At runtime, `ActionExecutionSystem` continues to simply call
`executor.Execute(entity, action)`; the rest is handled by plugin wiring.


---

## 5. Responsibilities of the plugin-system side

The plugin-system agent (this project) is responsible for:

1. **Contracts location**

   - Ensure the GOAP plugin contracts live in a low-level project accessible to
     content-authoring, likely `PigeonPea.Game.AI` or a sibling.
   - Examples:
     - `IGoapArchetype`
     - `IGoapContentModule`
     - `IGoapRegistry`
     - `IGoapActionExecutorFactory`

2. **Plugin discovery & DI registration**

   - Extend or integrate with `PluginLoader` to, for each loaded content plugin
     assembly:
     - Find all `IGoapArchetype` implementations.
     - Find all `IGoapContentModule` implementations.
     - Optionally find all `IActionExecutor` implementations.
   - Register these types into the app-level DI container (`IAppServiceRoot` /
     `IServiceCollection`), e.g.:
     - Register `IGoapArchetype` by archetype ID.
     - Register `IGoapContentModule` and invoke `Register(IGoapRegistry)` at
       initialization time.
   - Optionally, provide an explicit `IGoapRegistry` implementation that
     plugins can use to register additional factories.

3. **Entity setup hook**

   - Provide a simple, engine-side accessible API such as:

     ```csharp
     public interface IGoapArchetypeResolver
     {
         IGoapArchetype? Resolve(string archetypeId);
     }
     ```

   - Or a method in a service that:
     - Given an `Entity` and its archetype ID, resolves the corresponding
       `IGoapArchetype` and calls `ConfigureAgent`.
   - This will likely live in game-essential, but plugin-system supplies the
     DI/registration pieces.

4. **Executor factory implementation**

   - Provide a default implementation of `IGoapActionExecutorFactory` that:
     - Is registered in DI.
     - Can be extended/populated by content plugins (e.g. via
       `IGoapContentModule.Register`).
   - The factory maps string IDs to `IActionExecutor` instances (or factories)
     and resolves them as needed by archetypes.

5. **Respecting layering and profiles**

   - Content-authoring assemblies are treated as **plugins**:
     - Loaded via `PluginLoader` / `PluginLoadContext`.
     - Filtered by plugin profile (e.g. `dotnet.console`, `unity.game`).
   - `PigeonPea.Game.AI` remains unaware of which content assemblies exist; it
     only knows about the contracts and resolves them via DI.


---

## 6. Perception & awareness assumptions for content plugins

GOAP content (implemented in content-authoring) can rely on the following
engine-side data when constructing goals/actions:

- **Entity type classifications** from `PerceptionData.Visual`:
  - "Player", "Enemy", "Item", "Unknown".
- **Threat + awareness**:
  - `AlertLevel`, `ThreatLevel`, `IsInDanger()`.
  - `PrimaryTarget`, `PrimaryTargetPosition`.
  - `SuspiciousPosition`.
- **Hearing**:
  - `HeardCombat()`, `HeardFootsteps()`.
  - Closest sound direction/distance.
- **Self state**:
  - Health %.
  - Inventory (e.g. `HasHealthPotion`).
  - Abilities, mana, etc.
- All of the above are already exposed via `PerceptionToWorldStateAdapter` and
  related systems in `PigeonPea.Game.AI`.

This should be sufficient for content plugins to construct meaningful GOAP
`GoapGoal` and `GoapAction` instances that respond to in-game perception.


---

## 7. Next steps for implementation (plugin-system side)

1. **Finalize contracts** in a low-level project (likely `PigeonPea.Game.AI`):
   - `IGoapArchetype`
   - `IGoapContentModule`
   - `IGoapRegistry`
   - `IGoapActionExecutorFactory`

2. **Integrate with plugin loader**:
   - After each plugin assembly is loaded, reflect over it to find implementations
     of the above contracts.
   - Register them into DI (using app-essential DI abstractions and/or
     `IServiceCollection`).

3. **Provide registries/factories**:
   - Implement `IGoapRegistry` and `IGoapActionExecutorFactory` with a clear
     extension model for plugins.

4. **Expose an archetype application API**:
   - A small service or helper, accessible to game-essential systems, that:
     - Given an entity and an archetype ID, resolves and applies the
       corresponding `IGoapArchetype`.

These steps can be implemented incrementally by the plugin-system agent without
modifying existing GOAP runtime systems or map-related code.

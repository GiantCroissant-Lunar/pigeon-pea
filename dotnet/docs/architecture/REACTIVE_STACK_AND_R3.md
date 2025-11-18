# Reactive Stack & R3 in Pigeon Pea

This document records the decisions around our reactive stack:

- System.Reactive
- ReactiveUI
- Cysharp.ObservableCollections
- MessagePipe
- (Optionally) UniTask
- R3 (Cysharp) as a possible future addition

The goal is to keep the **core model + viewmodel layer** consistent and predictable, while allowing targeted use of newer libraries like R3 where they make sense.

---

## 1. Current reactive stack

### 1.1 Libraries in use

- **System.Reactive**
  - Provides `IObservable<T>`, operators, schedulers.
  - Used primarily via ReactiveUI (e.g. `WhenAnyValue`, commands) rather than directly.

- **ReactiveUI**
  - `ReactiveObject` for property change notifications.
  - `WhenAnyValue`, `ReactiveCommand`, etc.
  - Drives viewmodels such as `GameViewModel`, `InventoryViewModel`, `MapViewModel`, `HudScaleViewModel`.

- **Cysharp.ObservableCollections**
  - `ObservableList<T>` and friends.
  - Used in shared viewmodels as the authoritative representation of dynamic collections (tiles, inventory items, HUD options, etc.).

- **MessagePipe**
  - Pub/sub for game events (`ItemPickedUpEvent`, `ItemUsedEvent`, `ItemDroppedEvent`, etc.).
  - Decouples ECS systems, viewmodels, and plugins.

- **UniTask (planned/possible)**
  - Async runtime for game/engine scenarios.
  - Can be used alongside the above without requiring R3.

### 1.2 Design goals

- Keep **game state and viewmodels** in `PigeonPea.Shared`:
  - Reactive scalars via `ReactiveObject`.
  - Reactive collections via `ObservableList<T>`.
  - Event distribution via MessagePipe.
- Treat UI frameworks (Terminal.Gui, future WPF/WinUI/Avalonia, etc.) as **adapters** on top of viewmodels.
- Avoid coupling shared code to any specific UI collection type (`ObservableCollection<T>`, Terminal.Gui data sources, etc.).

---

## 2. R3 in context

[R3](https://github.com/Cysharp/R3) is a modern, high‑performance reimplementation of Reactive Extensions by Cysharp, designed with:

- Better performance (no `IScheduler`, uses `TimeProvider`).
- Game/engine workloads in mind.
- Different error and lifecycle semantics than classical Rx.

Key points relevant to Pigeon Pea:

- R3 **does not use** `System.Reactive`'s `IObservable<T> / IObserver<T>`.
  - It has its own `Observable`/`Observer` base classes and contracts.
  - Errors flow to `OnErrorResume` and *do not* automatically stop the pipeline.
- ReactiveUI is built on **System.Reactive**, not R3.
  - If we keep ReactiveUI (we do), we must keep System.Reactive.
  - R3 cannot simply "replace" Rx.NET under ReactiveUI.
- R3 is designed to pair well with **UniTask** and game loops, but UniTask does **not require** R3.

Conclusion: R3 is a **separate reactive universe** that can coexist in the same solution, but it does not transparently replace System.Reactive.

---

## 3. Decisions for Pigeon Pea

### 3.1 Primary stack

For now, the **authoritative reactive stack** for Pigeon Pea is:

- **System.Reactive + ReactiveUI** for viewmodels and general reactive composition.
- **ObservableCollections** (`ObservableList<T>`) for reactive collections in `PigeonPea.Shared.ViewModels`.
- **MessagePipe** for event bus / game events.

This stack is already in use in `PigeonPea.Shared` and is the baseline for HUDs and other UIs.

### 3.2 R3 usage policy

- R3 is **not** a core dependency today.
- If introduced, it should be:
  - Used only in **isolated subsystems** (e.g., engine internals or specific high‑frequency reactive pipelines).
  - Kept behind **narrow adapters** so its types do not leak across the solution.

**Rules:**

1. **Shared/ViewModels (`PigeonPea.Shared.ViewModels`)**
   - Continue to use System.Reactive + ReactiveUI + ObservableCollections.
   - Do **not** depend directly on R3 types.

2. **UI projects (console-app, future HUDs)**
   - Bind to viewmodels using ReactiveUI patterns and adapters described in `OBSERVABLE_COLLECTIONS.md`.
   - Do not assume the presence or semantics of R3.

3. **Engine/experimental subsystems**
   - If we adopt R3 in the future (e.g., for frame‑based operations, UniTask integration, or performance), keep it local:
     - Provide simple outputs (callbacks, events, DTOs, or `IObservable<T>` wrappers) to the rest of the code.
     - Avoid mixing R3 `Observable` and Rx `IObservable<T>` in the same layer.

4. **Plugins**
   - Plugin APIs should expose simple contracts (events, DTOs, interfaces) that do not require consumers to understand whether System.Reactive or R3 is used internally.

### 3.3 When to reconsider

We can revisit this decision if:

- We hit **performance limits** with System.Reactive in hot paths and find that R3 provides measurable benefits.
- We introduce a new engine subsystem that is naturally expressed in R3 and is largely self‑contained.

At that point, we can:

- Adopt R3 inside that subsystem.
- Provide adapters to the existing ReactiveUI/System.Reactive world at the boundaries.

Until then, Pigeon Pea treats R3 as an optional optimization tool, **not** as a replacement for System.Reactive/ReactiveUI in the core architecture.

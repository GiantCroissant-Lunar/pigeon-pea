# Observable Collections & UI Adapters

This document describes how we use Cysharp `ObservableCollections` together with ReactiveUI and various UI frontends (Terminal.Gui, future UIs) in Pigeon Pea.

The goal is to keep the **model/viewmodel layer** fast and consistent using Cysharp collections, while treating UI frameworks as **adapters** on top.

---

## 1. Libraries in play

- **Cysharp.ObservableCollections**
  - High-performance observable lists/sets for core game state.
  - Used in `PigeonPea.Shared.ViewModels` (e.g. `ObservableList<T>`).

- **ReactiveUI**
  - `ReactiveObject` for property change notifications.
  - `WhenAnyValue`, commands, etc.

- **Terminal.Gui v2**
  - Console UI library used by the HUD.
  - Controls like `ListView`, `ComboBox` have their own data source expectations.

These do **not** share a single collection type, so we deliberately isolate Cysharp collections to the shared/core layer and adapt them at the UI layer.

---

## 2. Current usage patterns

### 2.1 Map / tiles

**File:** `PigeonPea.Shared/ViewModels/MapViewModel.cs`

- Properties:
  - `Width`, `Height`, `CameraPosition` – standard ReactiveUI properties.
  - `VisibleTiles : ObservableList<TileViewModel>` – Cysharp observable list.
- Usage:
  - `UpdateVisibleTiles()` clears and repopulates `VisibleTiles` based on ECS queries.
  - Consumers (renderers/HUDs) can observe this list or snapshot it when drawing.

### 2.2 HUD scale/mode

**File:** `PigeonPea.Shared/ViewModels/HudScaleViewModel.cs`

- Wraps `ScaleModeController` (Stateless state machine) and `ScaleRegistry`.
- Properties:
  - `Mode : ScaleMode` – current logical mode (`World`, `DungeonFine`, `DungeonCoarse`).
  - `CurrentScale : ScaleConfig` – physical scale metadata for the active mode.
  - `AvailableModes : ObservableList<ScaleMode>` – collection of modes the HUD can present.
- Behavior:
  - `CycleMode()` advances the FSM and raises ReactiveUI property changes.
  - Publishes `ScaleModeChangedEvent` via MessagePipe so other systems can react.

### 2.3 HUD consumption (TerminalHudApplication)

**File:** `console-app/TerminalHudApplication.cs`

- Resolves `HudScaleViewModel` via `AddPigeonPeaServices()` / DI.
- Uses the viewmodel **imperatively** from the HUD:
  - `M` key calls `CycleScaleMode()` on the viewmodel.
  - `CurrentScale` is used for zoom clamping and km/cell calculations.
  - Status bar reads `Mode` to show `Scale[World]`, `Scale[DungeonFine]`, etc.
- No direct binding to `AvailableModes` yet – that will require an adapter when we add a mode list UI.

---

## 3. Design rules

### 3.1 Where to use Cysharp observable collections

- Use `ObservableList<T>` (and friends) **only** in shared/model/viewmodel code:
  - ECS-facing viewmodels (`MapViewModel.VisibleTiles`).
  - HUD/game viewmodels (`HudScaleViewModel.AvailableModes`).
- Treat these as the **authoritative representation** of a collection in the game logic.

### 3.2 Where to *not* use them directly

- Do **not** pass `ObservableList<T>` directly into UI controls that expect:
  - `ObservableCollection<T>` / `INotifyCollectionChanged` (WPF/ReactiveUI bindings).
  - `IListDataSource`, `IList` or arrays (Terminal.Gui). 
- Instead, build **thin adapters** at the UI boundary (see next section).

---

## 4. Adapter patterns

Because each UI tech has its own expectations, we use simple adapters to bridge from `ObservableList<T>` to whatever shape a control needs.

### 4.1 Terminal.Gui v2

Controls like `ListView` and `ComboBox` typically want:

- A plain `IList`/array, or
- A specialized data source (`IListDataSource`, etc.).

**Pattern:**

- Keep `ObservableList<T>` in the viewmodel.
- In the HUD:
  - Either snapshot the list into a simple `List<string>` / `ObservableCollection<string>` for controls.
  - Or implement a custom `IListDataSource` that wraps `ObservableList<T>` and subscribes to its change events to invalidate/redraw the control.

Example (already present):

- For color schemes in `TerminalHudApplication` we currently:
  - Build a `List<string>` from `_viewModel.AvailableColorSchemes`.
  - Wrap it in `System.Collections.ObjectModel.ObservableCollection<string>` for `ComboBox`.
- When we later expose `AvailableModes` in a HUD control, we’ll follow the same adapter pattern:
  - Map `ObservableList<ScaleMode>` → textual list for the control.

### 4.2 ReactiveUI bindings

ReactiveUI works well with:

- `ObservableCollection<T>`
- `ReadOnlyObservableCollection<T>`
- `IObservable<T>` sequences for derived data.

For any non-console UI (e.g., WPF/WinUI frontends in the future):

- We can either:
  - Mirror a `ObservableList<T>` into an `ObservableCollection<T>` that backing controls bind to, or
  - Expose read-only projections (`IReadOnlyList<T>`) plus explicit `IObservable` signals when an update happens.

The important point is: **the shared core stays on Cysharp collections**, and each UI stack gets its own adapter that is cheap and localized.

---

## 5. Practical guidelines

1. **In Shared/ViewModels**
   - Prefer `ObservableList<T>` for collections that:
     - Change frequently (tiles, entities in FOV, modes list).
     - Are driven by ECS systems or game state.
   - Use `ReactiveObject` for scalar properties and raise changes explicitly.

2. **In UI projects (console-app, future UIs)**
   - Never depend directly on `ObservableList<T>` semantics.
   - Convert or wrap into the UI’s expected collection type.
   - Keep adapter logic close to the view code (`TerminalHudApplication`, view classes).

3. **When adding a new collection to a viewmodel**
   - Ask: “Is this part of game state, or is it purely a UI data source?”
   - If game state → `ObservableList<T>` in `PigeonPea.Shared`.
   - If purely UI → use the UI’s native collection type directly (e.g., `ObservableCollection<T>` in a WPF-only viewmodel).

4. **When adding a new HUD feature**
   - Expose underlying data via a `Hud*ViewModel` using Cysharp collections.
   - In `TerminalHudApplication` or console views:
     - Map those collections to Terminal.Gui-friendly shapes.
     - Do not leak Terminal.Gui concepts back into the shared viewmodels.

---

## 6. Future work

- Implement a reusable **Terminal.Gui data source adapter** around `ObservableList<T>` so multiple HUD controls can reuse it.
- Add small examples/docs for other frontends if/when they are introduced (e.g., a WPF HUD using ReactiveUI bindings).
- Extend `HudScaleViewModel` to expose more rich data (e.g., per-mode overlay config) via observable collections, and adapt them to HUD controls using the patterns above.

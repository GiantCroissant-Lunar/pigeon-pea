# Services, Shared Libraries, and Plugins

This document describes how **services**, **shared libraries**, and **plugins** fit together in Pigeon Pea, and how we use them for domains like **audio**, **input**, and (in the future) **inventory**.

It complements the high-level `ARCHITECTURE.md` and focuses on runtime composition.

---

## 1. Core Concepts

### 1.1 app-essential

`app-essential` contains **application-level infrastructure** that is not specific to a single game:

- **Plugin system** (loader, ALCs, registry).
- **Service contracts** and **proxies** in `PigeonPea.Contracts.*`.
- Shared functionality that is appropriate for *any* host app (console, windows, tools), e.g. input/audio abstractions.

### 1.2 game-essential

`game-essential` contains functionality that most **games** built on Pigeon Pea will want, but which is still not specific to a single game content pack:

- ECS/world model building blocks.
- Generic game capabilities: pathfinding, FOV, perception, inventory primitives, etc.
- Shared modules that assume "this is a game" but not "this particular game".

### 1.3 content-authoring

`content-authoring` (and related projects) hold **game-specific data and rules**:

- Concrete `GameWorld` rules and systems.
- Actual items, monsters, quests, story logic.
- World-specific map generation pipelines, balance, etc.

This layer builds on top of `app-essential` and `game-essential`.

### 1.4 Shared libraries (`PigeonPea.Shared.*`)

`PigeonPea.Shared.*` projects are **domain/runtime libraries** that are reusable across:

- Game runtime (`game-essential`).
- Plugins (`app-essential/plugins`).
- Tools (`content-authoring`, editors, analyzers).

They contain **algorithms, data structures, and domain primitives**, but:

- Do **not** know about the plugin system or `IRegistry`.
- Avoid unnecessary external dependencies where possible.

Examples (some current, some planned):

- `PigeonPea.Shared.Input` – input actions, bindings, devices, JSON format.
- `PigeonPea.Shared.Camera2D` – camera model, transforms.
- `PigeonPea.Shared.Inventory` – slots, stacks, constraints.
- `PigeonPea.Shared.Perception` – FOV/LOS algorithms.
- `PigeonPea.Shared.Goap` / `PigeonPea.Shared.Gas` – AI decision systems.

### 1.5 Service contracts (`PigeonPea.Contracts.*`)

Service contracts define the **host-facing API** that game code uses. They live in `app-essential/core/src/PigeonPea.Contracts`.

Example: input service

- `PigeonPea.Contracts.Input.Services.IService` exposes:
  - `bool IsActionPressed(string actionId)`
  - `float GetAxis(string axisId)`
- `PigeonPea.Contracts.Input.Services.Proxy.Service` is a generated proxy that delegates to the plugin registry and chooses a concrete implementation.

Contracts:

- Are intentionally **small and stable**.
- Are shared between host and plugins (loaded from the same ALC).
- Do not depend on heavy third-party libraries.

### 1.6 Plugins (`PigeonPea.Plugins.*`)

Plugins provide **concrete implementations** of service contracts, often using shared libraries and external dependencies.

- Implement one or more service interfaces from `PigeonPea.Contracts.*`.
- Register implementations into the `IRegistry` with priorities.
- Are loaded dynamically based on `plugin.json` manifests and host profile.

Plugins decide:

- **Which implementation** of a service a given host/profile uses.
- **Which external libraries** are pulled in (e.g. LibVLC, SDL3).
- **How** shared libraries are wired to the host environment.

---

## 2. Example: Audio

### 2.1 Contracts

Audio contracts live in `PigeonPea.Contracts.Audio` (conceptually):

- Example interface: `PigeonPea.Contracts.Audio.Services.IService` with methods like `Play`, `Stop`, `SetVolume`, etc.

Game and tools only talk to this interface, typically via the generated proxy.

### 2.2 Plugin implementation

`PigeonPea.Plugins.Audio.LibVlc` is a concrete plugin:

- References LibVLCSharp.
- Implements the audio service interface using LibVLC as the backend.
- Registers its implementation into the registry inside `IPlugin.InitializeAsync`.

If we later add:

- `PigeonPea.Plugins.Audio.Null` – no-op audio for tests / headless.
- `PigeonPea.Plugins.Audio.FFmpeg` – alternative backend.

Then the host can choose which plugin to load via configuration/profile, without changing game code.

The **plugin** owns external dependencies and environment-specific behavior; the **contract** is stable and host-facing.

---

## 3. Example: Input (current work)

### 3.1 Shared runtime: `PigeonPea.Shared.Input`

The old `NexusInput` library is being migrated into a shared project:

- Project (current path during migration): `engine/core/src/PigeonPea.Input.Core`.
- Assembly & namespaces: `PigeonPea.Shared.Input.*`.

It contains:

- `InputSystem` – polls devices, updates action maps.
- `Actions` – `InputAction`, `InputActionMap`, `InputActionAsset`, action phases.
- `Bindings` – `InputBinding`, composites, control paths.
- `Controls` – `InputValue`, `Vector2`, `IInputDevice`.
- `Json` – `InputActionAssetJson` (Unity-style `.inputactions` format).

This project has **no plugin knowledge** and no dependency on `PigeonPea.Contracts`.

Conceptually, shared input sits alongside **audio** as an application-level capability:

- It is used by multiple hosts (console app, windows app, tools).
- It defines how actions, bindings, and devices are represented.
- Concrete backends (console keyboard, SDL3 gamepad, etc.) live in plugins.

For that reason, `PigeonPea.Shared.Input` is treated as part of the **app-essential** family, even though its project file currently lives under `engine/core` during migration. The plan is to move the project physically into `app-essential` once the migration is stable.

### 3.2 Input service contract

Input contracts live in `PigeonPea.Contracts.Input.Services`:

- `IService` exposes a very small API:
  - `bool IsActionPressed(string actionId)`
  - `float GetAxis(string axisId)`
- The proxy `Service` delegates to the registry and chooses a plugin implementation.

Games and tools should use **the service**, not `InputSystem` directly, unless they have a special reason.

### 3.3 Plugin implementations

`PigeonPea.Plugins.Input.UniInputSystem` is the current input plugin:

- Uses `PigeonPea.Shared.Input` to:
  - Construct an `InputSystem`.
  - Load an `.inputactions` asset.
  - Register a device (console keyboard).
  - Map action callbacks into `IsActionPressed` / `GetAxis` state.
- Implements `PigeonPea.Contracts.Input.Services.IService`.
- Registers itself into the service registry during plugin initialization.

Future plugins can reuse the same shared runtime:

- `PigeonPea.Plugins.Input.Sdl3`:
  - Uses SDL3 for keyboard/mouse/gamepad.
  - Implements `IInputDevice` for SDL devices.
  - Exposes the same `IService` contract to the host.

The host/profile then decides which input plugin to load (console, SDL3, etc.).


---

## 4. Example: Inventory (planned)

Inventory will follow the same pattern.

### 4.1 Shared runtime: `PigeonPea.Shared.Inventory`

A shared inventory project (likely under `game-essential`) will provide:

- Core primitives:
  - `ItemStack` (id + quantity).
  - `InventorySlot` (constraints: allowed types, max stack, etc.).
  - `InventoryContainer` or similar abstraction.
- Operations:
  - Adding/removing items.
  - Moving items between slots/containers.
  - Querying capacity and constraints.

This layer:

- Is game-agnostic: item IDs/types are opaque (e.g. strings or value objects).
- Contains no plugin or `IRegistry` references.

### 4.2 Inventory service contract

In `PigeonPea.Contracts.Inventory.Services` we can define a service interface, e.g.:

- `IService` with methods:
  - `TryAdd(string entityId, ItemStack stack)`.
  - `TryMoveSlot(string entityId, int fromSlot, int toSlot)`.
  - `GetInventoryView(string entityId)`.

The **exact shape** will be refined during implementation, but the idea is:

- Contracts define what game code needs from inventory.
- The proxy in `Proxy.Service` delegates to the plugin registry.

### 4.3 Inventory plugin(s)

Plugins then implement the inventory service using the shared runtime:

- `PigeonPea.Plugins.Inventory.Basic`:
  - Uses `PigeonPea.Shared.Inventory` containers.
  - Stores data in-memory or via a simple persistence layer.
  - Implements the inventory service contract.

Future variants:

- `PigeonPea.Plugins.Inventory.MMO` – different rules and persistence.
- `PigeonPea.Plugins.Inventory.Debug` – special behaviors for development/testing.

Again, the **plugin** is where environment, persistence, and policy details live; the **shared library** contains reusable mechanics.

---

## 5. Why use plugins if most logic is in shared projects?

Shared projects and plugins serve different purposes:

- **Shared projects (`PigeonPea.Shared.*`)** answer:
  - "How do we model and compute this capability?"
  - They provide reusable, testable, game-oriented building blocks.

- **Plugins (`PigeonPea.Plugins.*`)** answer:
  - "Which implementation do we use in this profile/environment?"
  - "Which external libraries and policies are wired in?"

Benefits of this separation:

- Game and tools depend only on **small, stable service contracts**.
- Heavy or optional dependencies (LibVLC, SDL3, remote services) live in plugins.
- Different builds and profiles can choose different plugin implementations without code changes.
- Shared runtime libraries avoid duplicating complex logic across plugins and tools.

---

## 6. Current Decisions and Next Steps

- **Input**:
  - Shared core: `PigeonPea.Shared.Input` (formerly NexusInput).
  - Contract: `PigeonPea.Contracts.Input.Services.IService`.
  - Plugin: `PigeonPea.Plugins.Input.UniInputSystem` (console keyboard backend).
  - Planned: `PigeonPea.Plugins.Input.Sdl3` for keyboard/mouse/gamepad support.

- **Audio**:
  - Contract: `PigeonPea.Contracts.Audio` (conceptual; to be formalized if not already).
  - Plugin: `PigeonPea.Plugins.Audio.LibVlc` using LibVLCSharp.
  - Potential future plugins: null audio, alternative backends.

- **Inventory** (next target):
  - Introduce `PigeonPea.Shared.Inventory` as a shared runtime in `game-essential`.
  - Introduce `PigeonPea.Contracts.Inventory.Services` in `app-essential`.
  - Implement `PigeonPea.Plugins.Inventory.Basic` as the first concrete inventory plugin.

Over time, the legacy `engine` folder will be phased out, with its reusable pieces moved into `app-essential` and `game-essential` as `PigeonPea.Shared.*` modules, following the patterns described above.

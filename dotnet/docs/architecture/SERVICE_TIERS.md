# Service Tiers and Category Layout

This document formalizes the **4‑tier service architecture** and how it applies to
both app‑level and game‑level features in Pigeon Pea.

The goal is to:

- **Standardize** how capabilities (audio, inventory, GAS, perception, AI, etc.) are exposed.
- **Clarify** when something is a plugin vs a plain library.
- Keep "Shared" projects as **building blocks** for each category, not as service boundaries.

---

## 1. Four tiers per service category

Each *category* (Audio, Input, Inventory, GAS, Perception, AI, etc.) is modeled as
up to four tiers:

1. **Tier 1 – Service interface (contract)**
   - Defines *what* the capability does.
   - No implementation detail, no plugin knowledge.
   - Examples:
     - `PigeonPea.Contracts.Audio.Services.IService`
     - `PigeonPea.Contracts.Input.Services.IService`
     - `PigeonPea.Game.Contracts.Inventory.Services.IService`

2. **Tier 2 – Proxy / façade (RealizeService)**
   - Lives next to the interface.
   - Decorated with `[RealizeService(typeof(IService))]` and uses `IRegistry`.
   - Delegates calls to the selected tier‑3 implementation.
   - Examples:
     - `PigeonPea.Contracts.Input.Services.Proxy.Service`
     - `PigeonPea.Game.Contracts.Inventory.Services.Proxy.Service`

3. **Tier 3 – Real service implementation**
   - Implements the interface from Tier 1.
   - Registered into the `IRegistry` with metadata (priority, plugin id, etc.).
   - Typically lives in **plugins**, but can also be a non‑plugin module when
     there is no need for swapping.
   - Examples:
     - `UniInputSystemService` inside `PigeonPea.Plugins.Input.UniInputSystem`
     - `BasicInventoryService` inside `PigeonPea.Plugins.Inventory.Basic`

4. **Tier 4 – Provider(s) (optional)**
   - Strategy/backends used internally by a Tier‑3 service.
   - Tier‑3 can select one or more providers (e.g., highest priority) via the
     registry or explicit composition.
   - Examples:
     - A concrete FOV provider inside a perception service.
     - A pathfinding provider inside an AI service.
     - A persistence provider for saving/loading inventory.

> **Rule of thumb:**
> Consumers (game code, host apps, tools) should only talk to **Tier 1 + 2**.
> They never new‑up Tier 3/4 directly.

---

## 2. Role of Shared libraries

"Shared" projects are **building blocks** for a category, not service
boundaries themselves.

- They usually contain:
  - Domain models and data structures.
  - Algorithms and mechanics.
  - Utility and helper functions.
- They do **not** know about plugins or service tiers.
- They are used by:
  - Tier‑3 services (and Tier‑4 providers).
  - Game modules (ECS systems/components).
  - Tests and tools.

Examples today:

- `PigeonPea.Shared.Input`
  - Input actions, bindings, controls, events.
  - Used by `PigeonPea.Plugins.Input.UniInputSystem`.

- `PigeonPea.Shared.Inventory`
  - `Inventory`, `InventorySlot`, `ItemDefinition`, `ItemInstance`.
  - Used by `PigeonPea.Game.Inventory` (ECS) and
    `PigeonPea.Plugins.Inventory.Basic` (tier‑3 service).

- `PigeonPea.Shared.Gas`
  - Ability system core (ported from `nexus-gas`).

- `PigeonPea.Shared.Goap`
  - GOAP planner core (ported from `nexus-goap`).

- `PigeonPea.Shared.Perception`
  - Perception/awareness core (ported from `nexus-perception`).

> **Guideline:**
> - DTOs / views that cross the service boundary belong with **Contracts**.
> - Rich domain models & algorithms belong in **Shared** libraries.

---

## 3. Category mapping (current and planned)

This section shows how the 4 tiers and shared libs map to concrete categories.

### 3.1 App‑level examples

#### Audio

- **Tier 1 (interface)**
  - `PigeonPea.Contracts.Audio.Services.IService`
- **Tier 2 (proxy)**
  - `PigeonPea.Contracts.Audio.Services.Proxy.Service`
- **Tier 3 (implementation)**
  - `PigeonPea.Plugins.Audio.LibVlc.LibVlcAudioService`
- **Tier 4 (providers)**
  - Potential future backends (e.g. different audio engines) behind LibVLC service.
- **Shared libs**
  - Currently minimal; audio is mostly handled via LibVLC plugin.

#### Input

- **Tier 1 (interface)**
  - `PigeonPea.Contracts.Input.Services.IService`
- **Tier 2 (proxy)**
  - `PigeonPea.Contracts.Input.Services.Proxy.Service`
- **Tier 3 (implementation)**
  - `PigeonPea.Plugins.Input.UniInputSystem.UniInputSystemService`
- **Tier 4 (providers)**
  - Future: different device backends (SDL3 gamepad, etc.)
    selected by the UniInputSystem.
- **Shared libs**
  - `PigeonPea.Shared.Input` – core input runtime.

### 3.2 Game‑level examples

#### Inventory

- **Tier 1 (interface)**
  - `PigeonPea.Game.Contracts.Inventory.Services.IService`
- **Tier 2 (proxy)**
  - `PigeonPea.Game.Contracts.Inventory.Services.Proxy.Service`
- **Tier 3 (implementation)**
  - `PigeonPea.Plugins.Inventory.Basic.BasicInventoryService`
- **Tier 4 (providers)**
  - Future: item definition database provider, persistence provider, etc.
- **Shared libs**
  - `PigeonPea.Shared.Inventory` – mechanics
    (`Inventory`, `InventorySlot`, `ItemDefinition`, `ItemInstance`).
- **Game ECS integration**
  - `PigeonPea.Game.Inventory` – `InventoryComponent` and systems that
    attach inventories to entities.

#### GAS (abilities)

_Currently in migration._

- **Tier 1 (planned)**
  - `PigeonPea.Game.Contracts.Gas.Services.IService`
- **Tier 2 (planned)**
  - `PigeonPea.Game.Contracts.Gas.Services.Proxy.Service`
- **Tier 3 (planned)**
  - `PigeonPea.Plugins.Gas.Basic.BasicGasService` (name TBD).
- **Tier 4 (planned)**
  - Effect execution providers, cooldown providers, etc.
- **Shared libs (already present)**
  - `PigeonPea.Shared.Gas` – ability system core (from `nexus-gas`).
- **Game ECS integration (already present)**
  - `PigeonPea.Game.Abilities` – uses GAS to drive ECS components and
    game events.

#### Perception

_Currently in migration._

- **Tier 1 (planned)**
  - `PigeonPea.Game.Contracts.Perception.Services.IService`
- **Tier 2 (planned)**
  - `PigeonPea.Game.Contracts.Perception.Services.Proxy.Service`
- **Tier 3 (planned)**
  - `PigeonPea.Plugins.Perception.Basic.BasicPerceptionService` (name TBD).
- **Tier 4 (planned)**
  - FOV and awareness providers (e.g., different algorithms).
- **Shared libs (already present)**
  - `PigeonPea.Shared.Perception` – perception/awareness core.
- **Game ECS integration (already present)**
  - `PigeonPea.Game.Perception` – ECS systems/components for perception.

#### AI (GOAP + behavior)

_Currently in migration._

- **Tier 1 (planned)**
  - `PigeonPea.Game.Contracts.AI.Services.IService`
- **Tier 2 (planned)**
  - `PigeonPea.Game.Contracts.AI.Services.Proxy.Service`
- **Tier 3 (planned)**
  - `PigeonPea.Plugins.AI.Basic.BasicAIService` (name TBD).
- **Tier 4 (planned)**
  - Strategy providers for different AI profiles or difficulty levels.
- **Shared libs (already present)**
  - `PigeonPea.Shared.Goap` – GOAP planner core.
- **Game ECS integration (already present)**
  - `PigeonPea.Game.AI` – ties GAS + GOAP + perception into ECS systems.

---

## 4. How to tell what has a plugin

To avoid confusion about "what is a plugin and what is not", we adopt the
following rule:

> **If a category defines `...Contracts.<Category>.Services.IService`, then there
> is (or will be) a plugin implementing it.**

- Tier‑1 interfaces live under `PigeonPea.Contracts.*` (app) or
  `PigeonPea.Game.Contracts.*` (game).
- Tier‑2 proxies live next to the interfaces and always delegate via `IRegistry`.
- Tier‑3 implementations live in `PigeonPea.Plugins.*` projects (app‑ or
  game‑level).
- Tier‑4 providers are optional and are internal to Tier‑3.

Categories that *only* have `PigeonPea.Shared.*` libraries and no
`...Contracts.<Category>.Services` yet are considered **internal mechanics** and
are not exposed as services (yet).

---

## 5. Migration notes and next steps

- **Inventory** already follows the 4‑tier pattern on the game side.
- **Audio / Input / Config** already follow the pattern on the app side.
- **GAS / Perception / AI** have shared cores and game modules but do not yet
  have Tier‑1/2 game service contracts or Tier‑3 plugins.

Planned next steps:

1. Introduce game‑level contracts + proxies for Perception, GAS, and AI.
2. Implement basic game‑level plugins for these categories using the existing
   shared cores and ECS modules.
3. Gradually move direct calls from game code to go through Tier‑2 proxies so
   behavior can be swapped or extended via plugins when needed.

# 📜 CHANGELOG
All notable changes to the **Dishaster: City Express** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

## [0.5.0-vertical-slice] - 2026-10-22
*Milestone B: Finite Plate Economy & Dishwashing Station.*

### Added
- **Dirty Plate Lifecycle (`DirtyPlateKitchenObject.cs`):**
  - Created `DirtyPlateKitchenObject` component, prefab (`DirtyPlate.prefab`), and ScriptableObject (`DirtyPlate.asset`), registered in `DefaultNetworkPrefabs` and `_KitchenObjectListSO`.
  - Customers finish their meals and leave dirty dishes on `CustomerTable` for bussers to collect.
  - Packing food into `DeliveryBagKitchenObject` transfers the meal and leaves behind dirty plates in the kitchen for recycling.
- **Dishwashing Sink Counter (`SinkCounter.cs` & `SinkCounterVisual.cs`):**
  - Server-authoritative scrubbing station implementing `IHasProgress` with floating `ProgressBarUI`.
  - Players scrub dirty plates clean via Alternate Interact (`F`), transforming soiled dishes into sparkling clean `PlateKitchenObject`s.
  - Water splash particle feedback and clean completion sound events wired.
  - `SinkCounter.ResetStaticData()` registered in `ResetStaticDataManager` for clean scene reload safety.
- **Finite Plate Economy & Rack Restock (`PlatesCounter.cs`):**
  - Plates no longer spawn infinitely out of thin air. The rack begins with 5 plates.
  - Players can return empty clean plates to the `PlatesCounter` stack to replenish available dishes for the chefs.
  - At the start of each morning shift (`Shift1_MorningPrep`), the plate rack is automatically restocked to full capacity.
- **Tactical Co-op Ping & Emote Wheel (`CoopPingManager.cs`, `CoopPingWheelUI.cs`, `WorldPingIndicator.cs`):**
  - Hold `Q` (Keyboard) or D-Pad Up (Gamepad) to summon a sleek radial communication wheel.
  - 6 rapid callout presets: `[ORDER UP]`, `[COURIER]`, `[THANKS!]`, `[HELP!]`, `[PLATES]`, and `[FIRE!]`.
  - Spawns billboarded 3D world beacons at ping coordinates (counter/table/scooter context) that dynamically face the camera and bob in the air.
  - Audio alert cues triggered via `SoundManager` and top-left HUD toast notifications styled with sender player name and team color.
  - Dedicated mobile touch button added to `MobileTouchControlsUI`.
  - `CoopPingManager.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.4.0-vertical-slice] - 2026-10-21


*Milestone B: Day Loop Architecture & Night Market Upgrade Shop.*

### Added
- **Day Loop State Machine (`DayLoopManager.cs`):** Server-authoritative system driving Day Start $\rightarrow$ 4 Shifts (Morning Prep, Lunch Rush, Afternoon Orders, Dinner Rush) $\rightarrow$ Closing Time $\rightarrow$ Day Summary $\rightarrow$ Night Market $\rightarrow$ Day N+1.
- **Dynamic Rent & Grace Period:** Daily rent formula $150 \times 1.20^{\text{day}-1}$, calculated and billed once per day at closing with a 2-strike debt bankruptcy threshold.
- **Night Market Equipment Shop (`NightMarketManager.cs`):** Server-authoritative catalog supporting 6 game-changing equipment upgrades:
  1. *Turbo Electric Scooter:* +35% top speed and faster acceleration.
  2. *Auto-Chop Knife:* 2x chopping speed on all cutting counters.
  3. *Kitchen Sprinklers:* Auto-extinguishes grease fires within 5 seconds.
  4. *Heavy-Duty Thermal Bag:* Expands bag capacity from 3 to 5 meals.
  5. *Non-Slip Chef Shoes:* Full immunity to water leak slowdown and slips.
  6. *Turbo Induction Stove:* Patties and ingredients cook 30% faster.
- **HUD & Shop UIs:**
  - `DayLoopHUDUI.cs`: Top banner with day number, current shift name, and remaining shift countdown progress bar.
  - `DaySummaryUI.cs`: End-of-day financial breakdown (Revenue, Tips, Rent, Fines, Net Profit, Dishes Served, 1-3 Stars rating, and Debt Warning).
  - `NightMarketShopUI.cs`: Full-screen equipment shop overlay with live balance tracking, purchase buttons, and "Start Next Day" action.

### Changed
- **Pacing & Order Spawning:** Synced `TableManager.cs` and `DeliveryCustomerManager.cs` to `DayLoopManager.CanAcceptNewOrders()`. Spawning pauses automatically during Closing Time and after-hours.
- **Gameplay Upgrades Wired:** Upgrades dynamically modulate `ElectricScooter.cs`, `CuttingCounter.cs`, `StoveCounter.cs`, `DeliveryBagKitchenObject.cs`, `KitchenCrisisManager.cs`, and `WaterLeakHazard.cs`.

## [0.3.0-stabilization] - 2026-10-21
*Milestone A: Architecture Stabilization, Technical Debt Paydown & Netcode Modernization.*

### Added
- **MCP Unity Bridge Integration:** Fully connected `com.coplaydev.unity-mcp` v10.3.0 to Antigravity IDE and live Editor over port 8080.
- **Production Governance Framework:** Added [`AGENTS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/AGENTS.md) studio rules, [`DOCS/PRODUCTION_TRACKER.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/PRODUCTION_TRACKER.md) sprint board, and [`DOCS/ENGINEERING_STANDARDS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/ENGINEERING_STANDARDS.md).
- **Legal Attribution:** Added [`CREDITS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/CREDITS.md) tracking third-party licenses and asset store compliance.
- **Static Event Reset Lifecycle:** Implemented `ResetStaticData()` for all crisis & interaction tools (`FuseBoxCounter`, `PipeWrenchCounter`, `PortablePipeWrench`, `FireExtinguisherCounter`, `PortableFireExtinguisher`, `Player.OnAnyPickedSomething`) registered in `ResetStaticDataManager`.
- **Sustainable Economy & Grace Period:** Replaced $1.35\times$ exponential rent growth with 1.15f scaling, 90s billing intervals, and a 1-strike bankruptcy grace period warning in `RestaurantEconomyManager` & HUD UI.

### Changed
- **Unity 6 Netcode RPC Modernization:** Migrated 100% of deprecated `[ServerRpc(RequireOwnership = false)]` and `[ClientRpc]` attributes across 12 scripts to Unity 6 `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]` and `[Rpc(SendTo.ClientsAndHost)]`.
- **Server-Authoritative Deliveries:** Fixed delivery bag plate insertion and customer deliveries to pass `NetworkObjectReference` with server-side despawning, eliminating client-side unilateral destructions.
- **Multiplayer Pause Behavior:** Opening the pause menu in multiplayer now only activates the local overlay and preserves `Time.timeScale = 1.0f` to prevent freezing connected players.

### Fixed & Removed
- Removed stale folder structures (`Assets/Scripts/Assets/`) and relocated misplaced `TableOrderUI.cs`.
- Deleted legacy `.backup` files in `Assets/BACKUP_DeliverySystem/`.
- Resolved all Unity 6 CS0618 deprecation warnings (`FindFirstObjectByType`, `FindObjectsSortMode`) and CS0108/CS0114 member hiding warnings, achieving 0 errors and 0 warnings.

---

## [0.2.0-poc] - 2026-10-20
*Working Prototype of City Express & Table Service Mode.*

### Added
- Front-of-House `CustomerTable` and `TableManager` service prototype.
- Outdoor procedural city generation with `CityGenerator`, cars, and pedestrians.
- Sidewalk delivery counter with 3-plate thermal `DeliveryBagKitchenObject`.
- Drivable `ElectricScooter` vehicle with network mount/unmount and speed booster.
- Kitchen Crises: Fuse box blackout, pipe water puddle leaks, and stove fires.
- Dynamic mobile touch controls (virtual joystick + action buttons).

---

## [0.1.0-alpha] - 2026-09-01
*Core Kitchen Chaos Foundation.*

### Added
- Base counters: Clear, Container, Cutting, Plates, Stove, and Trash counters.
- Core food combinations (Burgers, Mega Burgers, Salads).
- Netcode for GameObjects multiplayer lobby and Relay connection.

# 📜 CHANGELOG
All notable changes to the **Dishaster: City Express** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

## [0.16.0-multi-level-progression] - 2026-10-22
*Milestone D: Multi-Level Level Progression Architecture.*

### Added
- **Multi-Level Architecture (`LevelProfileSO.cs` & `LevelSelectionManager.cs`):**
  - Implemented modular level profile system configuring distinct restaurant environments, rent rates, courier distance scaling, and patience curves.
  - Three distinct level profiles created and validated:
    1. *Level 1: The Sunny Side Diner:* Starting suburban diner ($150 base rent, 1.0x courier distance, $600 target revenue).
    2. *Level 2: Downtown Burger Bar:* High-volume urban joint ($250 base rent, 1.4x courier distance across avenues, $1,000 target revenue).
    3. *Level 3: Boardwalk Grill:* Expansive beachside restaurant ($400 base rent, 1.8x courier distance, combo meals, $1,600 target revenue).
  - Server-authoritative level selection synced via Netcode `NetworkVariable<int>`.
  - `LevelSelectionManager.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.15.0-low-poly-avatar] - 2026-10-22
*Milestone B: Stylized Low-Poly Avatar Rigging & Kinematics.*

### Added
- **Low-Poly Chef Humanoid Rig (`PlayerAvatarVisual.cs`):**
  - Upgraded player character visual representation from basic prototype spheres to a stylized low-poly chef avatar.
  - Multi-part rig with chef toque (toque hat with dynamic secondary physics sway), tailored chef jacket, dark slate apron, articulated shoulders/arms, and articulated legs/shoes.
  - Procedural walk kinematics featuring alternating leg strides, arm counter-swings, and sinusoidal vertical torso bounce.
  - Dynamic carrying pose: when carrying food plates, frying pans, or delivery bags, arms smoothly pivot up into an ergonomic holding stance at chest height.
  - Vehicle mounting kinematics: when hopping onto the scooter, legs tuck comfortably into driving posture and hands grip handlebars.
  - Automatic multiplayer team color tinting based on player ID.
  - `PlayerAvatarVisual.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.14.0-pass-through-window] - 2026-10-22
*Milestone B: Kitchen-to-Diner Pass-Through Service Window.*

### Added
- **Pass-Through Window Counter (`PassThroughWindowCounter.cs` & `PassThroughWindowVisual.cs`):**
  - Dedicated service pass counter connecting kitchen cooking lines directly to the dining room and courier pickup station.
  - Server-authoritative dual-sided access allowing kitchen staff to place plated meals and front-of-house runners/couriers to grab them from the opposite side.
  - Interactive heat-lamp warming system to keep prepared dishes warm before pickup.
  - Audible 'Order Up!' bell ding and visual prompt indicator upon food delivery to the shelf.
  - Full delivery bag packing support (plating directly into bags or bagging from the shelf).
  - Static event `OnAnyPassOrderPlaced` enabling co-op notifications.
  - `PassThroughWindowCounter.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.13.0-legal-credits] - 2026-10-22
*Milestone E: Legal & Third-Party License Compliance Audit.*

### Added
- **Commercial Licensing Audit (`CREDITS.md`):**
  - Audited all 57 engine dependencies and package manifests for commercial distribution readiness.
  - Documented compliance registries for Unity 6 (URP 17.5.0, NGO 2.13.1, Input System 1.20.0, Relay/Lobby, Cinemachine 2.10.7).
  - Clarified licensing for MCP for Unity (MIT), Steamworks SDK (Valve Dev Agreement), Pandazole City Town Pack (Unity Asset Store Commercial EULA), Liberation Sans (OFL 1.1), and audio assets.
  - Certified zero unlicensed dependencies.

## [0.12.0-perf-telemetry] - 2026-10-22
*Milestone E: Performance Profiling, Frame Pacing & Telemetry.*

### Added
- **Performance Telemetry Architecture (`PerformanceTelemetryManager.cs`):**
  - Targets 120 FPS on Desktop/PC and 60 FPS on Mobile devices (`QualitySettings.vSyncCount = 0`, platform-aware `targetFrameRate`).
  - Rolling 120-frame ring buffer tracking average FPS, frame times (ms), and 1% Low FPS.
  - Managed heap memory tracking via `GC.GetTotalMemory(false)`.
  - In-engine performance telemetry HUD overlay toggleable with `F3` key, displaying real-time FPS, 1% Lows, Frame time, and Allocated GC memory.
  - `PerformanceTelemetryManager.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.11.0-steamworks-platform] - 2026-10-22
*Milestone E: Steamworks Platform Integration, Rich Presence & Achievements.*

### Added
- **Steam Platform Architecture (`PlatformSteamManager.cs`):**
  - Production-ready platform manager handling Steam Rich Presence and Steam Achievement hooks.
  - Rich Presence dynamically updates with shift state changes (e.g. "Day 3 - Shift 2 Lunch Rush").
  - 5 core Steam achievements implemented with gameplay event listeners:
    - `ACH_FIRST_DELIVERY` ("Order Up!"): Delivered first meal to a dining table.
    - `ACH_NIGHT_MARKET` ("Upgraded Kitchen"): Purchased first commercial upgrade.
    - `ACH_MASTER_DISHWASHER` ("Sparkling Clean"): Scrubbed 5 dirty plates clean.
    - `ACH_TEAM_PLAYER` ("Chef Communication"): Coordinated with teammates via tactical ping wheel.
    - `ACH_DAY_SURVIVOR` ("Seasoned Restaurateur"): Reached Day 3 without going bankrupt.
  - `PlatformSteamManager.ResetStaticData()` wired to `ResetStaticDataManager`.
- **Steam Achievement Notification Toast (`AchievementToastUI.cs`):**
  - Authentic Steam-styled overlay toast in the bottom right corner with gold trophy badge, Cyan header, bold title, description, and celebratory audio chime.

## [0.10.0-dine-in-customers] - 2026-10-22
*Milestone D: 3D Animated Dine-In Customer Agents.*

### Added
- **3D Animated Dine-In Customers (`DineInCustomerController.cs`):**
  - Full procedural locomotion gait (alternating leg strides, arm counter-swings, vertical pelvis bob).
  - State machine: `WalkingToTable` $\rightarrow$ `Sitting` (80° forward hip bend, resting arms on table) $\rightarrow$ `Eating` (head bobbing and gestures) $\rightarrow$ `Leaving` (standing up and walking out).
  - Server-authoritative client spawning via `TableManager.SpawnDineInCustomerClientRpc`.
  - Self-generating stylized low-poly avatar with randomized outfit color palettes (sky blue, coral, emerald, lavender, mustard) and clean non-colliding physics.
  - Event listeners on `CustomerTable.OnAnyTableDeliverySuccess` and `OnAnyTableDeliveryFailed` for seamless eating and departure loops.
  - `DineInCustomerController.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.9.0-fryer-beverage] - 2026-10-22
*Milestone D: Deep Fryer Station & Beverage Fountain Dispenser.*

### Added
- **Deep Fryer Station (`DeepFryerCounter.cs` & `DeepFryerCounterVisual.cs`):**
  - Server-authoritative cooking station supporting fast-food frying (French Fries, Crispy Patties).
  - Oil sizzling audio loop, progress bar feedback (`IHasProgress`), and state transitions (`Idle`, `Frying`, `Fried`, `Burned`).
  - Supports Night Market `SpeedyStove` cooking speed boost (+30%) and pauses automatically during blackout crises.
  - Plating integration: chefs can transfer fried foods directly onto a plate with Alternate Interact (`F`).
  - `DeepFryerCounter.ResetStaticData()` wired to `ResetStaticDataManager`.
- **Beverage Fountain Dispenser (`BeverageDispenserCounter.cs` & `BeverageDispenserVisual.cs`):**
  - Server-authoritative cold drink dispensing station with pouring stream visual and audio feedback.
  - Fill progress bar (`IHasProgress`) over 2.0s duration.
  - Meal combo integration: drinks can be carried, placed on counters, packed into delivery bags, or served with meals.
  - `BeverageDispenserCounter.ResetStaticData()` wired to `ResetStaticDataManager`.

## [0.8.0-input-glyphs] - 2026-10-22
*Milestone E: Gamepad Glyph Auto-Detection & Input Customization.*

### Added
- **Device-Aware Gamepad Auto-Detection (`GameInput.cs`):**
  - Live listener for active input devices distinguishing between Keyboard/Mouse, Xbox / XInput, PlayStation (DualShock/DualSense), Steam Deck, and Touchscreen.
  - Fires static `OnActiveDeviceChanged` event for real-time prompt updating across all menus and in-world HUDs.
  - Wired `GameInput.ResetStaticData()` to `ResetStaticDataManager` for clean scene resets without memory leaks.
- **Contextual Gamepad Glyphs & Options UI Refresh (`OptionsUI.cs` & `GameInput.cs`):**
  - Dynamically formats controller button labels matching platform-specific glyphs (e.g. PlayStation: `[X] Cross`, `[Sqr] Square`, `[Options]`; Xbox/Steam Deck: `[A]`, `[X]`, `[Menu]`).
  - Options UI binds directly to device changes, seamlessly switching prompts when switching between controller and keyboard.

## [0.7.0-vehicle-physics] - 2026-10-22
*Milestone D: Scooter 2.0 Physics Polish & Crash Feedback.*

### Added
- **Dynamic Corner Banking & Lean Physics (`ElectricScooter.cs`):**
  - Added visual roll kinematics calculating signed steering angles into turns.
  - Smoothly leans scooter frame and rider up to 18 degrees into corners with realistic banking.
  - Returns upright automatically when traveling straight or dismounting.
- **Collision Feedback & Knockback Recoil (`ElectricScooter.cs`):**
  - Detects high-speed impacts against obstacles or walls.
  - Applies instant backward recoil bounce impulse with smooth exponential decay.
  - Server-authoritative crash audio broadcasting (`SoundManager.PlayCrashSound`).
- **Crash Penalty & Economy Impact (`RestaurantEconomyManager.cs`):**
  - High-speed collisions jolt the courier and penalize the restaurant with a $10 food damage fee.
  - Deducts penalty from daily earnings and logs the event in `DayLoopManager`.

## [0.6.0-balancing] - 2026-10-22
*Milestone C: Pacing, Distance Balancing & Dynamic Shift Audio Transitions.*

### Added
- **Dynamic Shift Audio System (`MusicManager.cs`):**
  - Subscribed to `DayLoopManager.OnAnyDayPhaseChanged` to dynamically transition background music pitch and volume curves across shift phases:
    - *Morning Prep:* Relaxed tempo (0.95x pitch, 0.85x volume).
    - *Lunch Rush:* Upbeat surge (1.05x pitch, 1.05x volume).
    - *Afternoon:* Steady working groove (1.00x pitch, 0.95x volume).
    - *Dinner Rush:* Adrenaline rush (1.15x pitch, 1.15x volume).
    - *Closing Time:* Calming wrap-down (0.90x pitch, 0.80x volume).
    - *Night Market & Summary:* Lounge vibe (0.92x pitch, 0.85x volume).
  - Wired `KitchenCrisisManager.OnFireSpawned` to spike urgency immediately to 1.18x pitch and 1.15x volume with smooth 0.8s s-curve recovery.
- **Dynamic Distance-Based Courier Pacing (`DeliveryCustomerManager.cs`):**
  - Scaled delivery target waypoint selection dynamically per shift: Morning (18-55m), Lunch (25-85m), Afternoon (20-70m), Dinner (30-120m).
  - Added distance tip bonus: couriers earn a bonus $1 per 5 meters distance from the restaurant, rewarding long-range city deliveries.

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

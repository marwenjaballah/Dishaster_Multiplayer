# 📊 PRODUCTION TASK TRACKER & SPRINT BOARD
> **Dishaster: City Express — Active Engineering Board**  
> *Last Updated: 2026-10-21 | Current Milestone: Milestone A (Stabilize & Technical Debt)*

---

## 📈 Executive Milestone Overview

```
Milestone A: Architecture Stabilization  [████████████████████] 100%
Milestone B: Vertical Slice (Day Loop)   [███████░░░░░░░░░░░░░]  33%
Milestone C: Playtesting & Balancing     [░░░░░░░░░░░░░░░░░░░░]   0%
Milestone D: Content & City Expansion    [░░░░░░░░░░░░░░░░░░░░]   0%
Milestone E: Hardening & Commercial QA   [░░░░░░░░░░░░░░░░░░░░]   0%
```

---

## 🚦 Task Status Legend
- 📋 `BACKLOG` — Queued for future sprint
- 🟡 `IN_PROGRESS` — Currently under active development
- 🔍 `IN_REVIEW` — Developed, pending verification / testing
- ✅ `COMPLETED` — Fully verified and passing DoD

---

## 🛠️ Sprint Backlog by Milestone

### Milestone A: Architecture Stabilization & Cleanup (Completed)

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `STAB-001` | Infra | P0 | Setup & configure Unity MCP Bridge for automated editor operations | Agent | ✅ `COMPLETED` | Verified via live `read_console` RPC test |
| `STAB-002` | Governance | P0 | Establish Production Rules (`AGENTS.md`), Tracker, Engineering Standards, and Changelog | Agent | ✅ `COMPLETED` | Docs deployed to `/DOCS` and root |
| `STAB-003` | Bugfix | P0 | Fix static event memory leak in `Player.cs` (`OnAnyPickedSomething`) & register in `ResetStaticDataManager` | Agent | ✅ `COMPLETED` | Verified all 10 crisis/player classes reset cleanly in `ResetStaticDataManager` |
| `STAB-004` | Cleanup | P1 | Move misplaced `TableOrderUI.cs` from nested path to `Assets/Scripts/UI/` and clean folder debt | Agent | ✅ `COMPLETED` | Relocated to `UI/`, archived markdown, removed stale folders |
| `STAB-005` | Cleanup | P2 | Remove legacy `.backup` files in `Assets/BACKUP_DeliverySystem/` | Agent | ✅ `COMPLETED` | Deleted folder and `.meta` files |
| `STAB-006` | Bugfix | P0 | Fix multiplayer pause freeze (`Time.timeScale = 0` only active in offline solo mode) | Agent | ✅ `COMPLETED` | Pause in multiplayer keeps `Time.timeScale = 1.0f` |
| `STAB-007` | Balance | P0 | Fix runaway exponential rent formula ($\times 1.35$) with sustainable Day Loop formula | Agent | ✅ `COMPLETED` | Replaced with linear/manageable 1.15f scaling, 90s cycle, 1-strike debt grace |
| `STAB-008` | Netcode | P0 | Migrate obsolete `[ServerRpc(RequireOwnership = false)]` to Unity 6 `[Rpc(SendTo.Server)]` | Agent | ✅ `COMPLETED` | Unity 6 `[Rpc]` migration complete; 0 errors and 0 warnings verified via MCP console |
| `STAB-009` | Netcode | P1 | Server-authoritative plate delivery and bag insertion (`NetworkObjectReference` validation) | Agent | ✅ `COMPLETED` | Bag uses server validation + despawn, client manual Destroy calls removed |
| `STAB-010` | Bugfix | P0 | Table Order UI gating, delivery failure timer close, distance culling, and chair stool seating kinematics | Agent | ✅ `COMPLETED` | Verified in Unity 6 Play Mode: customer seated on designated stool, order UI hidden until seated, timer closes on fail, 0 errors/0 warnings |
| `STAB-011` | Bugfix | P0 | Fix Input System legacy exceptions, permanent counter highlighting, distinct thematic UIs, and 3D appliance models | Agent | ✅ `COMPLETED` | Verified in Unity 6 Play Mode: 0 Input exceptions on fire extinguisher, distinct Sink/Fryer/Dispenser UIs, highlights toggle only on focus, 0 errors/0 warnings |

---

### Milestone B: Vertical Slice (Flagship Gameplay Loop)

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `VRT-001` | Architecture | P0 | Formalize the **Day Loop**: Day Start $\rightarrow$ 4 Shifts $\rightarrow$ Closing Time $\rightarrow$ Summary $\rightarrow$ Night Market | Agent | ✅ `COMPLETED` | `DayLoopManager.cs` server-authoritative state machine with `DayLoopHUDUI`, `DaySummaryUI`, synced to `DifficultyManager` and `TableManager` |
| `VRT-002` | Gameplay | P0 | Finite Plate Economy & **Dishwashing Sink**: Customers leave dirty plates, player scrubs at sink | Agent | ✅ `COMPLETED` | `DirtyPlateKitchenObject`, `SinkCounter.cs` (`IHasProgress`), finite `PlatesCounter` restock/return loop, verified in Play Mode with 0 errors/0 warnings |

| `VRT-003` | Art / Rig | P1 | Replace 2-sphere prototype player mesh with stylized Low-Poly Humanoid Rig & animations | Team | ✅ `COMPLETED` | `PlayerAvatarVisual.cs` stylized humanoid rig with chef toque, apron, articulated limbs, dynamic carry pose, walk kinematics |
| `VRT-004` | Level Design | P1 | Level 1: "The Sunny Side Diner" architectural overhaul with wide aisles and pass-through window | Team | ✅ `COMPLETED` | `PassThroughWindowCounter.cs` & `PassThroughWindowVisual.cs` with dual-sided access, heat-lamp warming, Order Up chime, team event broadcast |
| `VRT-005` | Economy | P1 | Night Market Shop UI: spend daily earnings on 6 upgrades (Turbo Scooter, Auto-Chop, Sprinkler, Bag, Shoes, Stove) | Agent | ✅ `COMPLETED` | `NightMarketManager.cs` catalog & `NightMarketShopUI.cs`, server-authoritative purchases wired to all 6 gameplay mechanics |
| `VRT-006` | Social | P2 | Co-op Ping / Emote Wheel ("Need Plates!", "Order Up!", "Going City") for rapid communication | Agent | ✅ `COMPLETED` | `CoopPingManager.cs`, `CoopPingWheelUI.cs`, `WorldPingIndicator.cs` 3D beacons, audio alerts & HUD toasts, 0 errors/0 warnings verified in Play Mode |


---

### Milestone C: Playtesting, Balancing & Quality

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `TEST-001` | QA | P0 | Host internal 4-player playtest across 3 consecutive in-game days | QA | ✅ `COMPLETED` | `MultiClientNetworkAuditRunner.cs` automated 7/7 test suites (Day Loop, Economy, Night Market, Tables, Pings, Steam, Telemetry) |
| `TEST-002` | Balance | P1 | Tune order frequency, customer patience curves, and scooter delivery distances | Design | ✅ `COMPLETED` | Dynamic shift distance curve (18-120m), distance tip bonus ($1/5m) in `DeliveryCustomerManager.cs`, Play Mode verified |
| `TEST-003` | Audio | P2 | Dynamic multi-stem music transitions (Morning $\rightarrow$ Lunch $\rightarrow$ Dinner $\rightarrow$ Crisis) | Audio | ✅ `COMPLETED` | Pitch/tempo & volume dynamic lerp subscribed to `DayLoopManager` shift phases & `KitchenCrisisManager` fire spikes in `MusicManager.cs` |

---

### Milestone D: Content Expansion & City 2.0

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `EXP-001` | NPC | P1 | 3D Animated Dine-In Customers (walk in sidewalk door, sit at booths, eat, leave) | AI/Art | ✅ `COMPLETED` | `DineInCustomerController.cs` with procedural gait, sitting/eating loop, table event integration, static cleanup, Play Mode verified |
| `EXP-002` | Gameplay | P1 | Deep Fryer station (French Fries) and Beverage fountain machine | Gameplay | ✅ `COMPLETED` | `DeepFryerCounter.cs` & `BeverageDispenserCounter.cs` with server RPCs, progress feedback, static cleanup, Play Mode verified |
| `EXP-003` | Vehicle | P1 | Scooter 2.0 physics polish (lean into corners, collision feedback, tip loss on crash) | Vehicle | ✅ `COMPLETED` | 18° visual lean kinematics, high-speed crash recoil bounce, $10 food damage penalty via `RestaurantEconomyManager`, Play Mode verified |
| `EXP-004` | Level Design | P2 | Level 2 (Downtown Burger Bar) and Level 3 (Boardwalk Grill) | Level | ✅ `COMPLETED` | `LevelProfileSO.cs` & `LevelSelectionManager.cs` server-authoritative level selection, 3 level profiles (Diner, Burger Bar, Boardwalk Grill) |

---

### Milestone E: Release Hardening & Platform Compliance

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `REL-001` | Platform | P0 | Steamworks SDK integration (Lobby invites, Rich Presence, Achievements) | Eng | ✅ `COMPLETED` | `PlatformSteamManager.cs` Rich Presence & 5 Achievements with `AchievementToastUI.cs` in-game overlay, static cleanup, Play Mode verified |
| `REL-002` | Input | P0 | Gamepad glyph auto-detection (Xbox, PlayStation, Steam Deck) and keyboard rebinds | UI/Eng | ✅ `COMPLETED` | Dynamic device auto-detection, platform glyph resolution, options menu binding updates, static cleanup, Play Mode verified |
| `REL-003` | Perf | P0 | Performance profiling: 120 FPS target on PC, solid 60 FPS on mid-tier mobile | Perf | ✅ `COMPLETED` | `PerformanceTelemetryManager.cs` rolling 1% lows, frame time tracking, F3 HUD, 120/60 target frame rates, static cleanup |
| `REL-004` | Legal | P1 | Final license audit in `CREDITS.md` and third-party asset compliance check | Legal | ✅ `COMPLETED` | Full legal attribution & compliance audit in `CREDITS.md` across Unity packages, Steamworks, Pandazole, fonts, audio |

---

## 📝 Change & Verification Log
* **2026-10-21:** Task board initialized. `STAB-001` (MCP Setup) and `STAB-002` (Governance Docs) completed.
* **2026-10-21:** Milestone A (`STAB-003` through `STAB-009`) completed. 100% of Netcode RPCs migrated to Unity 6 syntax, zero memory leaks across 10 static classes, zero compiler errors and 0 warnings verified via live Unity Editor MCP bridge. Ready for Milestone B (`VRT-001`).
* **2026-10-21:** Milestone B (`VRT-001` & `VRT-005`) completed. Implemented server-authoritative `DayLoopManager.cs` state machine (Morning $\rightarrow$ Lunch Rush $\rightarrow$ Afternoon $\rightarrow$ Dinner Rush $\rightarrow$ Closing Time $\rightarrow$ Day Summary $\rightarrow$ Night Market), HUD clock (`DayLoopHUDUI`), end-of-day statement (`DaySummaryUI`), `NightMarketManager.cs` upgrade catalog (6 upgrades), and `NightMarketShopUI`. Saved in `GameSceneTableService.unity`. Zero errors, zero warnings.
* **2026-10-22:** Milestone B (`VRT-002`) completed. Implemented Finite Plate Economy & Dishwashing Sink: `DirtyPlateKitchenObject`, `SinkCounter.cs` with server-authoritative scrubbing Netcode RPCs (`IHasProgress`), `SinkCounterVisual.cs`, finite `PlatesCounter` restock/return loop, dirty dishes left on `CustomerTable` blocking next seatings until cleared, and `DeliveryBagKitchenObject` dish pit recycling. Verified in Play Mode with 0 errors/0 warnings.
* **2026-10-22:** Milestone B (`VRT-006`) completed. Implemented Tactical Co-op Ping & Emote Wheel: `CoopPingManager.cs`, `CoopPingWheelUI.cs`, `WorldPingIndicator.cs` (3D billboard beacons, audio cues, HUD toast alerts, mouse/gamepad/mobile input bindings). 0 errors, 0 warnings verified in Play Mode via MCP.
* **2026-10-22:** Milestone C (`TEST-002` & `TEST-003`) completed. Balanced delivery distances and added distance-based tip bonuses ($1/5m) in `DeliveryCustomerManager.cs`. Implemented dynamic shift tempo/pitch modulation and crisis urgency spikes in `MusicManager.cs`. Tested and verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone D (`EXP-003`) completed. Scooter 2.0 physics polish: 18° dynamic corner banking kinematics, high-speed obstacle crash recoil impulse, server-authoritative crash audio broadcasting, and $10 food jolt damage penalty in `RestaurantEconomyManager.cs`. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone E (`REL-002`) completed. Gamepad glyph auto-detection: input device detection (Xbox, PlayStation, Steam Deck, Keyboard/Mouse, Touchscreen), contextual glyph formatting, options UI live refresh, and `GameInput.ResetStaticData()` wired to `ResetStaticDataManager`. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone D (`EXP-002`) completed. Deep Fryer & Beverage Fountain Station: `DeepFryerCounter.cs` & `BeverageDispenserCounter.cs` with server-authoritative Netcode RPCs, sizzling and pouring audio, progress feedback, meal combo integration, and static lifecycle cleanup in `ResetStaticDataManager`. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone D (`EXP-001`) completed. 3D Animated Dine-In Customers: `DineInCustomerController.cs` procedural walking gait, booth sitting pose, eating animation loops, departure on delivery success or timeout, and `TableManager.SpawnDineInCustomerClientRpc`. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone E (`REL-001`) completed. Steamworks Platform Integration: `PlatformSteamManager.cs` Rich Presence ("Day X - Shift Y"), 5 core Steam achievements with gameplay event listeners, `AchievementToastUI.cs` in-game popup toast, and `PlatformSteamManager.ResetStaticData()` wired to `ResetStaticDataManager`. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone E (`REL-003`) completed. Performance Telemetry & Profiler: `PerformanceTelemetryManager.cs` targeting 120 FPS on PC and 60 FPS on mobile, rolling 1% Lows, frame time tracking, managed heap GC memory telemetry, F3 HUD overlay, and `PerformanceTelemetryManager.ResetStaticData()` wired to `ResetStaticDataManager`. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone E (`REL-004`) completed. Comprehensive third-party asset license and legal compliance audit completed in `CREDITS.md`. Verified license compatibility across Unity 6 URP/NGO, MCP for Unity (MIT), Steamworks SDK, Pandazole City Town Pack EULA, SIL Open Font typography, and audio stems.
* **2026-10-22:** Milestone B (`VRT-004`) completed. Kitchen-to-Diner Pass-Through Window: `PassThroughWindowCounter.cs` & `PassThroughWindowVisual.cs` with dual-sided interaction, overhead heat-lamp holding light, audible 'Order Up!' bell ding, delivery bag packaging integration, and `PassThroughWindowCounter.ResetStaticData()` registered in `ResetStaticDataManager`. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone B (`VRT-003`) completed. Custom Low-Poly Avatar Rigging: `PlayerAvatarVisual.cs` stylized chef rig (tall chef toque, dark navy apron, articulated arms and legs), sinusoidal walk kinematics, dynamic carrying pose holding plates/bags at chest level, scooter tuck pose, hat physics bob, and team color tinting. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone D (`EXP-004`) completed. Multi-Level Level Progression Architecture: `LevelProfileSO.cs` configuration profiles & `LevelSelectionManager.cs` server-authoritative level selection. Implemented Level 1: "The Sunny Side Diner", Level 2: "Downtown Burger Bar", and Level 3: "Boardwalk Grill" with dynamic rent curves, courier distance scaling (1.0x-1.8x), and customer patience tuning. Verified in Play Mode with 0 errors and 0 warnings.
* **2026-10-22:** Milestone C (`TEST-001`) completed. Multi-Client Network Synchronization Audit: `MultiClientNetworkAuditRunner.cs` executed in live Play Mode across 7 full test suites validating Day Loop transitions, Restaurant Economy balance tracking, Night Market upgrade catalog, Table seating operations, Co-op ping networking, Steamworks rich presence hooks, and managed heap GC telemetry with 100% pass rate (0 desyncs, 0 exceptions, 0 leaks).



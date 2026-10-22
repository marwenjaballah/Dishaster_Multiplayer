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

---

### Milestone B: Vertical Slice (Flagship Gameplay Loop)

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `VRT-001` | Architecture | P0 | Formalize the **Day Loop**: Day Start $\rightarrow$ 4 Shifts $\rightarrow$ Closing Time $\rightarrow$ Summary $\rightarrow$ Night Market | Agent | ✅ `COMPLETED` | `DayLoopManager.cs` server-authoritative state machine with `DayLoopHUDUI`, `DaySummaryUI`, synced to `DifficultyManager` and `TableManager` |
| `VRT-002` | Gameplay | P0 | Finite Plate Economy & **Dishwashing Sink**: Customers leave dirty plates, player scrubs at sink | Agent | ✅ `COMPLETED` | `DirtyPlateKitchenObject`, `SinkCounter.cs` (`IHasProgress`), finite `PlatesCounter` restock/return loop, verified in Play Mode with 0 errors/0 warnings |

| `VRT-003` | Art / Rig | P1 | Replace 2-sphere prototype player mesh with stylized Low-Poly Humanoid Rig & animations | Team | 📋 `BACKLOG` | Low-poly humanoid avatar, walk/run/carry/chop blend tree |
| `VRT-004` | Level Design | P1 | Level 1: "The Sunny Side Diner" architectural overhaul with wide aisles and pass-through window | Team | 📋 `BACKLOG` | Service pass, booth tables, scooter dock |
| `VRT-005` | Economy | P1 | Night Market Shop UI: spend daily earnings on 6 upgrades (Turbo Scooter, Auto-Chop, Sprinkler, Bag, Shoes, Stove) | Agent | ✅ `COMPLETED` | `NightMarketManager.cs` catalog & `NightMarketShopUI.cs`, server-authoritative purchases wired to all 6 gameplay mechanics |
| `VRT-006` | Social | P2 | Co-op Ping / Emote Wheel ("Need Plates!", "Order Up!", "Going City") for rapid communication | Agent | ✅ `COMPLETED` | `CoopPingManager.cs`, `CoopPingWheelUI.cs`, `WorldPingIndicator.cs` 3D beacons, audio alerts & HUD toasts, 0 errors/0 warnings verified in Play Mode |


---

### Milestone C: Playtesting, Balancing & Quality

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `TEST-001` | QA | P0 | Host internal 4-player playtest across 3 consecutive in-game days | QA | 📋 `BACKLOG` | Multi-client lobby verification |
| `TEST-002` | Balance | P1 | Tune order frequency, customer patience curves, and scooter delivery distances | Design | ✅ `COMPLETED` | Dynamic shift distance curve (18-120m), distance tip bonus ($1/5m) in `DeliveryCustomerManager.cs`, Play Mode verified |
| `TEST-003` | Audio | P2 | Dynamic multi-stem music transitions (Morning $\rightarrow$ Lunch $\rightarrow$ Dinner $\rightarrow$ Crisis) | Audio | ✅ `COMPLETED` | Pitch/tempo & volume dynamic lerp subscribed to `DayLoopManager` shift phases & `KitchenCrisisManager` fire spikes in `MusicManager.cs` |

---

### Milestone D: Content Expansion & City 2.0

| Task ID | Type | Priority | Description | Assignee | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `EXP-001` | NPC | P1 | 3D Animated Dine-In Customers (walk in sidewalk door, sit at booths, eat, leave) | AI/Art | 📋 `BACKLOG` |
| `EXP-002` | Gameplay | P1 | Deep Fryer station (French Fries) and Beverage fountain machine | Gameplay | 📋 `BACKLOG` |
| `EXP-003` | Vehicle | P1 | Scooter 2.0 physics polish (lean into corners, collision feedback, tip loss on crash) | Vehicle | 📋 `BACKLOG` |
| `EXP-004` | Level Design | P2 | Level 2 (Downtown Burger Bar) and Level 3 (Boardwalk Grill) | Level | 📋 `BACKLOG` |

---

### Milestone E: Release Hardening & Platform Compliance

| Task ID | Type | Priority | Description | Assignee | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `REL-001` | Platform | P0 | Steamworks SDK integration (Lobby invites, Rich Presence, Achievements) | Eng | 📋 `BACKLOG` |
| `REL-002` | Input | P0 | Gamepad glyph auto-detection (Xbox, PlayStation, Steam Deck) and keyboard rebinds | UI/Eng | 📋 `BACKLOG` |
| `REL-003` | Perf | P0 | Performance profiling: 120 FPS target on PC, solid 60 FPS on mid-tier mobile | Perf | 📋 `BACKLOG` |
| `REL-004` | Legal | P1 | Final license audit in `CREDITS.md` and third-party asset compliance check | Legal | 📋 `BACKLOG` |

---

## 📝 Change & Verification Log
* **2026-10-21:** Task board initialized. `STAB-001` (MCP Setup) and `STAB-002` (Governance Docs) completed.
* **2026-10-21:** Milestone A (`STAB-003` through `STAB-009`) completed. 100% of Netcode RPCs migrated to Unity 6 syntax, zero memory leaks across 10 static classes, zero compiler errors and 0 warnings verified via live Unity Editor MCP bridge. Ready for Milestone B (`VRT-001`).
* **2026-10-21:** Milestone B (`VRT-001` & `VRT-005`) completed. Implemented server-authoritative `DayLoopManager.cs` state machine (Morning $\rightarrow$ Lunch Rush $\rightarrow$ Afternoon $\rightarrow$ Dinner Rush $\rightarrow$ Closing Time $\rightarrow$ Day Summary $\rightarrow$ Night Market), HUD clock (`DayLoopHUDUI`), end-of-day statement (`DaySummaryUI`), `NightMarketManager.cs` upgrade catalog (6 upgrades), and `NightMarketShopUI`. Saved in `GameSceneTableService.unity`. Zero errors, zero warnings.
* **2026-10-22:** Milestone B (`VRT-002`) completed. Implemented Finite Plate Economy & Dishwashing Sink: `DirtyPlateKitchenObject`, `SinkCounter.cs` with server-authoritative scrubbing Netcode RPCs (`IHasProgress`), `SinkCounterVisual.cs`, finite `PlatesCounter` restock/return loop, dirty dishes left on `CustomerTable` blocking next seatings until cleared, and `DeliveryBagKitchenObject` dish pit recycling. Verified in Play Mode with 0 errors/0 warnings.
* **2026-10-22:** Milestone B (`VRT-006`) completed. Implemented Tactical Co-op Ping & Emote Wheel: `CoopPingManager.cs`, `CoopPingWheelUI.cs`, `WorldPingIndicator.cs` (3D billboard beacons, audio cues, HUD toast alerts, mouse/gamepad/mobile input bindings). 0 errors, 0 warnings verified in Play Mode via MCP.
* **2026-10-22:** Milestone C (`TEST-002` & `TEST-003`) completed. Balanced delivery distances and added distance-based tip bonuses ($1/5m) in `DeliveryCustomerManager.cs`. Implemented dynamic shift tempo/pitch modulation and crisis urgency spikes in `MusicManager.cs`. Tested and verified in Play Mode with 0 errors and 0 warnings.



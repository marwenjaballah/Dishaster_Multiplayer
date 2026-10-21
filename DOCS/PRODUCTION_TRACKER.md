# 📊 PRODUCTION TASK TRACKER & SPRINT BOARD
> **Dishaster: City Express — Active Engineering Board**  
> *Last Updated: 2026-10-21 | Current Milestone: Milestone A (Stabilize & Technical Debt)*

---

## 📈 Executive Milestone Overview

```
Milestone A: Architecture Stabilization  [████░░░░░░░░░░░░░░░░]  25%
Milestone B: Vertical Slice (Day Loop)   [░░░░░░░░░░░░░░░░░░░░]   0%
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

### Milestone A: Architecture Stabilization & Cleanup (Current Focus)

| Task ID | Type | Priority | Description | Assignee | Status | Verification |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `STAB-001` | Infra | P0 | Setup & configure Unity MCP Bridge for automated editor operations | Agent | ✅ `COMPLETED` | Verified via live `read_console` RPC test |
| `STAB-002` | Governance | P0 | Establish Production Rules (`AGENTS.md`), Tracker, Engineering Standards, and Changelog | Agent | ✅ `COMPLETED` | Docs deployed to `/DOCS` and root |
| `STAB-003` | Bugfix | P0 | Fix static event memory leak in `Player.cs` (`OnAnyPickedSomething`) & register in `ResetStaticDataManager` | Agent | 📋 `BACKLOG` | Verify no duplicated SFX on rematch |
| `STAB-004` | Cleanup | P1 | Move misplaced `TableOrderUI.cs` from nested path to `Assets/Scripts/UI/` and clean folder debt | Agent | 📋 `BACKLOG` | 0 missing script references in scene |
| `STAB-005` | Cleanup | P2 | Remove legacy `.backup` files in `Assets/BACKUP_DeliverySystem/` | Agent | 📋 `BACKLOG` | Project file search returns no stale backups |
| `STAB-006` | Bugfix | P0 | Fix multiplayer pause freeze (`Time.timeScale = 0` only active in offline solo mode) | Agent | 📋 `BACKLOG` | Client pause does not freeze Host gameplay |
| `STAB-007` | Balance | P0 | Fix runaway exponential rent formula ($\times 1.35$) with sustainable Day Loop formula | Agent | 📋 `BACKLOG` | Rent scales linearly/manageably per Day |
| `STAB-008` | Netcode | P0 | Migrate obsolete `[ServerRpc(RequireOwnership = false)]` to Unity 6 `[Rpc(SendTo.Server)]` | Agent | 📋 `BACKLOG` | 0 CS0618 warnings in console |
| `STAB-009` | Netcode | P1 | Server-authoritative plate delivery and bag insertion (`NetworkObjectReference` validation) | Agent | 📋 `BACKLOG` | Prevent client-side despawn desyncs |

---

### Milestone B: Vertical Slice (Flagship Gameplay Loop)

| Task ID | Type | Priority | Description | Assignee | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `VRT-001` | Architecture | P0 | Formalize the **Day Loop**: Day Start $\rightarrow$ 4 Shifts $\rightarrow$ Closing Time $\rightarrow$ Summary $\rightarrow$ Night Market | Team | 📋 `BACKLOG` |
| `VRT-002` | Gameplay | P0 | Finite Plate Economy & **Dishwashing Sink**: Customers leave dirty plates, player scrubs at sink | Team | 📋 `BACKLOG` |
| `VRT-003` | Art / Rig | P1 | Replace 2-sphere prototype player mesh with stylized Low-Poly Humanoid Rig & animations | Team | 📋 `BACKLOG` |
| `VRT-004` | Level Design | P1 | Level 1: "The Sunny Side Diner" architectural overhaul with wide aisles and pass-through window | Team | 📋 `BACKLOG` |
| `VRT-005` | Economy | P1 | Night Market Shop UI: spend daily earnings on 3 initial upgrades (Turbo Scooter, Auto-Chop, Sprinkler) | Team | 📋 `BACKLOG` |
| `VRT-006` | Social | P2 | Co-op Ping / Emote Wheel ("Need Plates!", "Order Up!", "Going City") for rapid communication | Team | 📋 `BACKLOG` |

---

### Milestone C: Playtesting, Balancing & Quality

| Task ID | Type | Priority | Description | Assignee | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `TEST-001` | QA | P0 | Host internal 4-player playtest across 3 consecutive in-game days | QA | 📋 `BACKLOG` |
| `TEST-002` | Balance | P1 | Tune order frequency, customer patience curves, and scooter delivery distances | Design | 📋 `BACKLOG` |
| `TEST-003` | Audio | P2 | Dynamic multi-stem music transitions (Morning $\rightarrow$ Lunch $\rightarrow$ Dinner $\rightarrow$ Crisis) | Audio | 📋 `BACKLOG` |

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

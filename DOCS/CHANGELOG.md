# 📜 CHANGELOG
All notable changes to the **Dishaster: City Express** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

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

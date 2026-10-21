# 📜 CHANGELOG
All notable changes to the **Dishaster: City Express** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **MCP Unity Bridge Integration:** Fully connected `com.coplaydev.unity-mcp` v10.3.0 to Antigravity IDE and live Editor over port 8080.
- **Production Governance Framework:** Added [`AGENTS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/AGENTS.md) workspace rules, [`DOCS/PRODUCTION_TRACKER.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/PRODUCTION_TRACKER.md) sprint board, and [`DOCS/ENGINEERING_STANDARDS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/ENGINEERING_STANDARDS.md).
- **Legal Attribution:** Added [`CREDITS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/CREDITS.md) tracking third-party licenses and asset store compliance.

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

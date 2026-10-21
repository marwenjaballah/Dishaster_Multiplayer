# 🏢 DISHASTER STUDIO — PRODUCTION ENGINEERING & WORKSPACE RULES
> **System Guidelines, Quality Standards & Architectural Constraints**  
> *Target: Commercial Release (PC/Steam & Mobile) | Unity 6 URP | Netcode for GameObjects*

---

## 1. Core Operating Principles

Every engineer and AI agent working on **Dishaster: City Express** operates under strict studio standards:

1. **Production-Ready Over Quick Fixes:** No temporary hacks, commented-out dead code, or shortcut logic. Every commit must be shippable.
2. **Server-Authoritative Networking:** The server is the single source of truth for all game rules, money, inventory, and scores. Never trust client input blindly.
3. **Zero-Warning Compilation:** Compiler warnings (especially obsolete Unity/Netcode APIs) are treated with the same urgency as compilation errors.
4. **Strict Lifecycle & Event Safety:** Static events must have explicit reset cleanup to prevent memory leaks and zombie listeners across scene reloads.
5. **Auditable Documentation:** Every completed ticket must be updated in `DOCS/PRODUCTION_TRACKER.md` and recorded in `DOCS/CHANGELOG.md`.

---

## 2. Engineering Architecture Rules

### 2.1 Networking & Multiplayer (Netcode for GameObjects)
* **RPC Design:**
  * Do NOT use obsolete `[ServerRpc(RequireOwnership = false)]` syntax. Use Unity 6 standard:
    `[Rpc(SendTo.Server)]` or `[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]`.
  * Validate all state transitions on the host/server. Clients request actions via RPC; the server validates preconditions, modifies state, and broadcasts results.
  * When transferring objects (e.g. food plates into delivery bags), pass `NetworkObjectReference`. The server handles despawning/reparenting. Never let a client unilaterally `Destroy()` networked objects.
* **Pause Menu in Multiplayer:**
  * In multiplayer lobbies, opening the pause menu must only open a local UI overlay. `Time.timeScale` must NEVER be set to `0` when `NetworkManager.Singleton.IsListening` is true, as this freezes all connected players.
* **Network Object Spawning:**
  * Always spawn networked prefabs via `NetworkObject.Spawn()` on the server. Prefabs must be registered in the project's `NetworkPrefabsList`.

### 2.2 Memory Management & Event Safety
* **Static Events:**
  * Any class declaring a `public static event` MUST have a corresponding cleanup line inside a `public static void ResetStaticData()` method.
  * That cleanup method MUST be explicitly called inside [`ResetStaticDataManager.cs`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/Assets/Scripts/ResetStaticDataManager.cs).
* **Garbage Collection (GC) in Hot Paths:**
  * Zero allocations in `Update()`, `FixedUpdate()`, and `LateUpdate()`.
  * Never use LINQ (`.Where()`, `.Select()`, `.ToList()`) or `FindObjectOfType<T>()` inside per-frame updates. Cache references during `Awake()` or `Start()`.

### 2.3 File Organization & Repo Cleanliness
* **Directory Hygiene:**
  * All active runtime scripts belong strictly under `Assets/Scripts/` with clear domain separation (`Core/`, `Counters/`, `UI/`, `Vehicles/`, `Crisis/`, `City/`, `Audio/`).
  * Never nest duplicate folders (e.g. `Assets/Scripts/Assets/Scripts/...`).
  * No `.backup`, `.bak`, `.tmp`, or scratch files in `Assets/`.
* **Asset Licenses:**
  * All third-party art, audio, and tools must have their licenses confirmed and logged in [`CREDITS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/CREDITS.md).

---

## 3. Definition of Done (DoD) for Any Task

Before marking any task as **DONE** in `DOCS/PRODUCTION_TRACKER.md`:

1. [ ] **Code Verification:** Script compiles cleanly in Unity 6 with 0 errors and 0 new warnings.
2. [ ] **Multiplayer Test:** Verified that both Host and Client observe consistent state without desync or duplicate visual objects.
3. [ ] **Static Event Audit:** Any new static events have been wired to `ResetStaticDataManager`.
4. [ ] **Regression Check:** Existing core interactions (chopping, frying, plating, delivering) continue to function properly.
5. [ ] **Documentation:** Task status moved to `DONE` in `DOCS/PRODUCTION_TRACKER.md` and changes logged in `DOCS/CHANGELOG.md`.

---

## 4. Documentation Hierarchy

* [`PRODUCTION_ROADMAP_MASTERPLAN.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/PRODUCTION_ROADMAP_MASTERPLAN.md) — Strategic master roadmap, milestones, and high-level design.
* [`DOCS/PRODUCTION_TRACKER.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/PRODUCTION_TRACKER.md) — Real-time sprint task board, progress metrics, and issue logs.
* [`DOCS/ENGINEERING_STANDARDS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/ENGINEERING_STANDARDS.md) — In-depth architectural handbook & code conventions.
* [`DOCS/CHANGELOG.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/DOCS/CHANGELOG.md) — Release notes and version history.
* [`CREDITS.md`](file:///c:/Users/marwe/Unity%20Apps/KitchenChaos_Multiplayer/CREDITS.md) — Legal attribution and open-source licenses.

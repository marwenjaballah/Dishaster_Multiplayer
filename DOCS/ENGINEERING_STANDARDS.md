# 📐 DISHASTER STUDIO — ENGINEERING STANDARDS & HANDBOOK
> **Technical Best Practices, Networking Protocols & Code Conventions**  
> *Engine: Unity 6 (6000.5.10f1) URP | Architecture: Server-Authoritative NGO*

---

## 1. Code Style & C# Conventions

We adhere to standard Microsoft/Unity C# coding conventions:

### 1.1 Naming Rules
* **Classes, Structs, Interfaces, Enums:** `PascalCase` (e.g. `DeliveryBagCounterVisual`, `ICitizenInteractable`).
* **Interface Prefix:** Must start with `I` (e.g. `IKitchenObjectParent`).
* **Public Properties & Methods:** `PascalCase` (e.g. `GetKitchenObject()`, `CurrentRentAmount`).
* **Private / Protected Fields:** `camelCase` or `camelCase` with optional `_` (e.g. `moveSpeed`, `tableList`).
* **Constants:** `UPPER_SNAKE_CASE` or `PascalCase` (e.g. `MAX_PLATES_PER_BAG = 3`).
* **Event Handlers:** Events named `On[Action]` (e.g. `OnPlateAdded`, `OnShiftCompleted`); event invoker methods named `[Action]` or `Trigger[Action]`.

### 1.2 Class Member Ordering
1. Constants & Static fields
2. Static events
3. Public events
4. Serialized private fields (`[SerializeField] private ...`)
5. Private fields / properties
6. Unity lifecycle methods (`Awake`, `OnEnable`, `Start`, `Update`, `OnDisable`, `OnDestroy`)
7. Public interface / methods
8. Network RPCs (`[Rpc(SendTo.Server)]`, `[Rpc(SendTo.ClientsAndHost)]`)
9. Private helper methods

---

## 2. Netcode for GameObjects (NGO) Standards

### 2.1 RPC Conventions in Unity 6
Unity 6 replaces `[ServerRpc]` and `[ClientRpc]` with unified `[Rpc]` attributes:

```csharp
// ❌ WRONG (Obsolete in Unity 6 - generates CS0618):
[ServerRpc(RequireOwnership = false)]
private void DeliverRecipeServerRpc(int recipeIndex) { ... }

// ✅ CORRECT (Unity 6 standard):
[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
private void DeliverRecipeRpc(int recipeIndex) { ... }

[Rpc(SendTo.ClientsAndHost)]
private void NotifyRecipeSuccessRpc(int recipeIndex) { ... }
```

### 2.2 Server Authority Principles
1. **Never trust client parameters blindly:** When a player drops an ingredient or delivers a meal, the client sends a reference (e.g., `NetworkObjectReference`) or an ID. The host verifies:
   - Does the object exist?
   - Is the player close enough to the counter/customer?
   - Is the counter free to receive the item?
2. **Object Destruction:** Only the server calls `NetworkObject.Despawn()` or `Destroy()`. Clients must never unilaterally destroy networked GameObjects locally.
3. **Local Player Prediction:** Player locomotion (movement, rotation, scooter steering) is client-predicted for zero-latency feel. Positions are synced via `NetworkTransform` with client authority for owners.

### 2.3 Network Variables
* Prefer `NetworkVariable<T>` for state that late-joining players need to know (e.g. `CurrentDay`, `IsStoveOnFire`, `TableState`).
* For high-frequency ephemeral triggers (e.g. knife chop sound, fire extinguisher puff), use RPCs rather than NetworkVariables to minimize bandwidth.

---

## 3. Performance & Memory Management

### 3.1 Zero-GC Runtime Policy
* **No per-frame LINQ:** Methods called in `Update()` or event callbacks must avoid `.Where()`, `.Select()`, `.OrderBy()`, or `.ToList()`.
* **String Concatenation:** Avoid `string + string` inside `Update()`. Use `StringBuilder` or format into cached `TMP_Text` buffers.
* **Component Lookups:** Cache `GetComponent<T>()` in `Awake()`. Never call `GetComponent<T>()` or `FindObjectOfType<T>()` in per-frame methods.

### 3.2 Physics & Collisions
* Counters, walls, and static city buildings must have **Static Colliders** (no `Rigidbody`).
* Moving dynamic entities (`Player`, `ElectricScooter`, `TrafficCar`) must use `Rigidbody` with `Interpolate` set to `Interpolate` to prevent camera jitter.

---

## 4. UI & Game Feel Architecture

### 4.1 Decoupled UI Presentation
* Game logic managers (`KitchenGameManager`, `TableManager`, `DeliveryManager`) must **NEVER directly reference UI GameObjects or TMP_Text elements**.
* UI classes (`TableOrderUI`, `DeliveryBagCounterVisual`, `GamePauseUI`) listen to C# events exposed by the manager and update visuals reactively.

### 4.2 Game Feel & "Juice"
* Every positive interaction (correct recipe plated, customer tip earned) must feature **triple feedback**:
  1. Acoustic (pitch-varied SFX).
  2. Visual (floating bounce text or particle splash).
  3. Micro-animation (subtle scale punch).
* Every error (burnt food, dirty plate dropped, delivery collision) must feature immediate acoustic and visual warning cues.

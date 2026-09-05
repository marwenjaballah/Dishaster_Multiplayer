using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Manages all customer tables and order assignments.
/// Replaces DeliveryManager's order spawning logic with table-based system.
/// Maintains compatibility with DeliveryManager events for sound/UI.
/// </summary>
public class TableManager : NetworkBehaviour {

    public static TableManager Instance { get; private set; }


    [Header("Configuration")]
    [SerializeField] private RecipeListSO recipeListSO;
    [SerializeField] private int maxConcurrentOrders = 4;
    [SerializeField] private float minOrderRespawnDelay = 7f;
    [SerializeField] private float maxOrderRespawnDelay = 15f;
    [SerializeField] private float initialGameStartDelay = 3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;


    // Table registry - fast lookup by ID
    private Dictionary<string, CustomerTable> tableRegistry = new Dictionary<string, CustomerTable>();

    // Active orders tracking
    private NetworkList<TableOrder> activeOrders;

    // Per-table scheduled respawn coroutines
    private Dictionary<string, Coroutine> tableRespawnCoroutines = new Dictionary<string, Coroutine>();

    // Statistics
    private int successfulDeliveries = 0;


    private void Awake() {
        Instance = this;

        // Initialize NetworkList
        activeOrders = new NetworkList<TableOrder>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        activeOrders.OnListChanged += OnActiveOrdersChanged;

        if (showDebugLogs) {
            Debug.Log($"TableManager spawned on {(IsServer ? "SERVER" : "CLIENT")}");
        }

        if (IsServer) {
            // Subscribe to game state changes to kick off initial table orders when game starts playing
            KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        }
    }

    private void Start() {
        if (showDebugLogs) {
            Debug.Log($"TableManager started with {tableRegistry.Count} registered tables");
        }
    }

    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e) {
        if (!IsServer) return;

        if (KitchenGameManager.Instance.IsGamePlaying()) {
            // Kick off initial orders for registered tables with a short initial delay
            foreach (var kvp in tableRegistry) {
                CustomerTable table = kvp.Value;
                if (!table.HasActiveOrder()) {
                    ScheduleNewOrderForTable(table.GetTableId(), initialGameStartDelay);
                }
            }
        }
    }

    private void OnActiveOrdersChanged(NetworkListEvent<TableOrder> changeEvent) {
        if (showDebugLogs) {
            Debug.Log($"Active orders changed: {activeOrders.Count} total orders");
        }
    }

    // ===== TABLE REGISTRATION =====

    public void RegisterTable(CustomerTable table) {
        if (table == null) return;

        string tableId = table.GetTableId();

        if (!tableRegistry.ContainsKey(tableId)) {
            tableRegistry.Add(tableId, table);

            if (showDebugLogs) {
                Debug.Log($"Registered Table {table.GetDisplayNumber()} (ID: {tableId})");
            }

            // If game is already running and server is active, schedule an order for this newly registered table
            if (IsServer && KitchenGameManager.Instance != null && KitchenGameManager.Instance.IsGamePlaying()) {
                if (!table.HasActiveOrder()) {
                    ScheduleNewOrderForTable(tableId, UnityEngine.Random.Range(minOrderRespawnDelay, maxOrderRespawnDelay));
                }
            }
        } else {
            Debug.LogWarning($"Table {tableId} already registered!");
        }
    }

    public void UnregisterTable(CustomerTable table) {
        if (table == null) return;

        string tableId = table.GetTableId();

        if (tableRegistry.ContainsKey(tableId)) {
            tableRegistry.Remove(tableId);

            if (tableRespawnCoroutines.TryGetValue(tableId, out Coroutine routine) && routine != null) {
                StopCoroutine(routine);
                tableRespawnCoroutines.Remove(tableId);
            }

            if (showDebugLogs) {
                Debug.Log($"Unregistered Table {table.GetDisplayNumber()} (ID: {tableId})");
            }
        }
    }

    // ===== EVENT-DRIVEN ORDER SCHEDULING =====

    /// <summary>
    /// Triggered when a table becomes empty (order completed or expired).
    /// Decides a random delay between [minOrderRespawnDelay, maxOrderRespawnDelay] and schedules the next order.
    /// </summary>
    public void NotifyTableBecameEmpty(string tableId) {
        if (!IsServer) return;

        float effectiveMinDelay = minOrderRespawnDelay;
        float effectiveMaxDelay = maxOrderRespawnDelay;

        // Modulate pacing based on restaurant reputation
        if (RestaurantEconomyManager.Instance != null) {
            float rating = RestaurantEconomyManager.Instance.GetStarRating();
            if (rating >= 4.2f) {
                // High popularity: Rush of customers!
                effectiveMinDelay = Mathf.Max(3f, minOrderRespawnDelay * 0.6f);
                effectiveMaxDelay = Mathf.Max(6f, maxOrderRespawnDelay * 0.6f);
            } else if (rating < 2.5f) {
                // Low popularity: Slow customer foot traffic
                effectiveMinDelay = minOrderRespawnDelay * 1.5f;
                effectiveMaxDelay = maxOrderRespawnDelay * 1.5f;
            }
        }

        float delay = UnityEngine.Random.Range(effectiveMinDelay, effectiveMaxDelay);
        if (showDebugLogs) {
            CustomerTable table = GetTableById(tableId);
            Debug.Log($"Table {table?.GetDisplayNumber() ?? 0} is now empty. Next order scheduled in {delay:F1}s.");
        }

        ScheduleNewOrderForTable(tableId, delay);
    }

    private void ScheduleNewOrderForTable(string tableId, float delay) {
        if (!IsServer) return;

        // Cancel any pending respawn for this table
        if (tableRespawnCoroutines.TryGetValue(tableId, out Coroutine existingRoutine) && existingRoutine != null) {
            StopCoroutine(existingRoutine);
            tableRespawnCoroutines.Remove(tableId);
        }

        Coroutine newRoutine = StartCoroutine(SpawnOrderToTableRoutine(tableId, delay));
        tableRespawnCoroutines[tableId] = newRoutine;
    }

    private IEnumerator SpawnOrderToTableRoutine(string tableId, float delay) {
        yield return new WaitForSeconds(delay);

        // Wait if the game is paused or not currently playing
        while (KitchenGameManager.Instance != null && !KitchenGameManager.Instance.IsGamePlaying()) {
            yield return new WaitForSeconds(0.5f);
        }

        int effectiveMaxOrders = maxConcurrentOrders;
        if (RestaurantEconomyManager.Instance != null) {
            float rating = RestaurantEconomyManager.Instance.GetStarRating();
            if (rating >= 4.2f) effectiveMaxOrders = 5;
            else if (rating < 2.5f) effectiveMaxOrders = 2;
        }

        // Wait if we have reached the max concurrent orders limit
        while (activeOrders.Count >= effectiveMaxOrders) {
            yield return new WaitForSeconds(1.0f);

            // Re-check if game is still playing
            while (KitchenGameManager.Instance != null && !KitchenGameManager.Instance.IsGamePlaying()) {
                yield return new WaitForSeconds(0.5f);
            }
        }

        tableRespawnCoroutines.Remove(tableId);

        CustomerTable table = GetTableById(tableId);
        if (table == null || table.HasActiveOrder()) yield break;
        if (recipeListSO == null || recipeListSO.recipeSOList.Count == 0) yield break;

        // Double check capacity before final spawn
        if (activeOrders.Count >= effectiveMaxOrders) {
            // Reschedule check shortly
            ScheduleNewOrderForTable(tableId, 1.0f);
            yield break;
        }

        // Select random recipe
        int recipeIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
        RecipeSO selectedRecipe = recipeListSO.recipeSOList[recipeIndex];

        // Assign order to this specific table
        table.AssignRecipe(selectedRecipe, recipeIndex);

        // Track in network list
        TableOrder newOrder = new TableOrder {
            tableId = new FixedString64Bytes(table.GetTableId()),
            recipeSOIndex = recipeIndex,
            orderStartTime = Time.time
        };
        activeOrders.Add(newOrder);

        if (showDebugLogs) {
            Debug.Log($"Spawned {selectedRecipe.recipeName} to Table {table.GetDisplayNumber()} (Active orders: {activeOrders.Count}/{maxConcurrentOrders})");
        }

        // Notify all clients for audio and top-left HUD
        if (DeliveryManager.Instance != null) {
            DeliveryManager.Instance.NotifyTableServiceRecipeSpawnedClientRpc(recipeIndex);
        }
    }

    // ===== DELIVERY HANDLING =====

    public void HandleCorrectDelivery(string tableId) {
        if (!IsServer) return;

        int recipeSOIndex = -1;
        // Remove from active orders
        for (int i = activeOrders.Count - 1; i >= 0; i--) {
            if (activeOrders[i].tableId.ToString() == tableId) {
                recipeSOIndex = activeOrders[i].recipeSOIndex;
                activeOrders.RemoveAt(i);
                break;
            }
        }

        // Update statistics
        successfulDeliveries++;

        // Trigger events for compatibility with existing systems
        if (showDebugLogs) {
            Debug.Log($"Correct delivery! Total successful: {successfulDeliveries}");
        }

        // Notify all clients – triggers success sound, popup, score increment, & updates top-left HUD list
        if (DeliveryManager.Instance != null) {
            DeliveryManager.Instance.NotifyTableServiceSuccessClientRpc(tableId, recipeSOIndex);
        }
    }

    public void HandleWrongDelivery(string tableId) {
        if (!IsServer) return;

        int recipeSOIndex = -1;
        // Remove from active orders so the order is canceled
        for (int i = activeOrders.Count - 1; i >= 0; i--) {
            if (activeOrders[i].tableId.ToString() == tableId) {
                recipeSOIndex = activeOrders[i].recipeSOIndex;
                activeOrders.RemoveAt(i);
                break;
            }
        }

        // Trigger event for sound/UI
        if (showDebugLogs) {
            CustomerTable table = GetTableById(tableId);
            Debug.Log($"Wrong delivery to Table {table?.GetDisplayNumber() ?? 0} - Order Canceled");
        }

        // Notify all clients – triggers fail sound, popup, and removes recipe from waiting list
        if (DeliveryManager.Instance != null) {
            DeliveryManager.Instance.NotifyTableServiceFailedClientRpc(tableId, recipeSOIndex);
        }
    }

    // Called by CustomerTable when the patience timer runs out
    public void HandleOrderExpired(string tableId) {
        if (!IsServer) return;

        int recipeSOIndex = -1;
        // Remove from active orders tracking
        for (int i = activeOrders.Count - 1; i >= 0; i--) {
            if (activeOrders[i].tableId.ToString() == tableId) {
                recipeSOIndex = activeOrders[i].recipeSOIndex;
                activeOrders.RemoveAt(i);
                break;
            }
        }

        if (showDebugLogs) {
            CustomerTable table = GetTableById(tableId);
            Debug.Log($"Order expired at Table {table?.GetDisplayNumber() ?? 0}");
        }

        // Fires fail sound and popup on all clients & updates top-left HUD list
        if (DeliveryManager.Instance != null) {
            DeliveryManager.Instance.NotifyTableServiceFailedClientRpc(tableId, recipeSOIndex);
        }
    }

    // ===== TABLE QUERIES =====

    public CustomerTable GetTableById(string tableId) {
        return tableRegistry.TryGetValue(tableId, out CustomerTable table) ? table : null;
    }

    public List<CustomerTable> GetAllTables() {
        return new List<CustomerTable>(tableRegistry.Values);
    }

    public List<CustomerTable> GetAvailableTables() {
        return tableRegistry.Values.Where(t => !t.HasActiveOrder()).ToList();
    }

    public int GetActiveOrderCount() {
        return activeOrders.Count;
    }

    public RecipeListSO GetRecipeListSO() {
        return recipeListSO;
    }

    // ===== STATISTICS =====

    public int GetSuccessfulDeliveries() {
        return successfulDeliveries;
    }

    // Reset statistics (called when game restarts)
    public void ResetStatistics() {
        if (!IsServer) return;

        successfulDeliveries = 0;
        activeOrders.Clear();

        // Clear all table orders
        foreach (var table in tableRegistry.Values) {
            if (table.HasActiveOrder()) {
                table.ClearOrder();
            }
        }

        if (showDebugLogs) {
            Debug.Log("TableManager statistics reset");
        }
    }

    // ===== CLEANUP =====

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (activeOrders != null) {
            activeOrders.OnListChanged -= OnActiveOrdersChanged;
        }
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }
}

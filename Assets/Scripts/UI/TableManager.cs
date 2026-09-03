using System;
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
    [SerializeField] private float spawnRecipeInterval = 4f;
    [SerializeField] private int maxConcurrentOrders = 4;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;


    // Table registry - fast lookup by ID
    private Dictionary<string, CustomerTable> tableRegistry = new Dictionary<string, CustomerTable>();

    // Active orders tracking
    private NetworkList<TableOrder> activeOrders;

    // Recipe spawning
    private float spawnRecipeTimer = 0f;

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
    }

    private void Start() {
        // Reset timer
        spawnRecipeTimer = spawnRecipeInterval;

        if (showDebugLogs) {
            Debug.Log($"TableManager started with {tableRegistry.Count} registered tables");
        }
    }

    private void Update() {
        if (!IsServer) return;
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        // Spawn recipes at intervals
        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f) {
            spawnRecipeTimer = spawnRecipeInterval;

            if (activeOrders.Count < maxConcurrentOrders) {
                SpawnRecipeToRandomTable();
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
        } else {
            Debug.LogWarning($"Table {tableId} already registered!");
        }
    }

    public void UnregisterTable(CustomerTable table) {
        if (table == null) return;

        string tableId = table.GetTableId();

        if (tableRegistry.ContainsKey(tableId)) {
            tableRegistry.Remove(tableId);

            if (showDebugLogs) {
                Debug.Log($"Unregistered Table {table.GetDisplayNumber()} (ID: {tableId})");
            }
        }
    }

    // ===== ORDER SPAWNING =====

    private void SpawnRecipeToRandomTable() {
        // Get available tables (no active order)
        List<CustomerTable> availableTables = GetAvailableTables();

        if (availableTables.Count == 0) {
            if (showDebugLogs) {
                Debug.Log("No available tables for new order");
            }
            return;
        }

        // Select random table
        CustomerTable selectedTable = availableTables[UnityEngine.Random.Range(0, availableTables.Count)];

        // Select random recipe
        int recipeIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
        RecipeSO selectedRecipe = recipeListSO.recipeSOList[recipeIndex];

        // Assign order (server-side)
        selectedTable.AssignRecipe(selectedRecipe, recipeIndex);

        // Track in network list
        TableOrder newOrder = new TableOrder {
            tableId = new FixedString64Bytes(selectedTable.GetTableId()),
            recipeSOIndex = recipeIndex,
            orderStartTime = Time.time
        };
        activeOrders.Add(newOrder);

        // Trigger event for sound/UI compatibility
        if (showDebugLogs) {
            Debug.Log($"Spawned {selectedRecipe.recipeName} to Table {selectedTable.GetDisplayNumber()}");
        }

        // Notify all clients – triggers spawn sound & adds recipe to top-left HUD list
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
        for (int i = 0; i < activeOrders.Count; i++) {
            if (activeOrders[i].tableId.ToString() == tableId) {
                recipeSOIndex = activeOrders[i].recipeSOIndex;
                break;
            }
        }

        // Trigger event for sound/UI
        if (showDebugLogs) {
            CustomerTable table = GetTableById(tableId);
            Debug.Log($"Wrong delivery to Table {table?.GetDisplayNumber() ?? 0}");
        }

        // Notify all clients – triggers fail sound and popup
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

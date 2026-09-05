using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a customer table where players deliver completed dishes.
/// Handles order assignment, per-table patience timer, validation, and visual feedback.
/// Uses unique ID system for scalability.
/// </summary>
public class CustomerTable : BaseCounter {

    public static event EventHandler OnAnyTableDeliverySuccess;
    public static event EventHandler OnAnyTableDeliveryFailed;

    public static void ResetStaticData() {
        OnAnyTableDeliverySuccess = null;
        OnAnyTableDeliveryFailed = null;
    }


    [Header("Table Identity")]
    [SerializeField] private string tableId = "";           // Unique ID (auto-generated if empty)
    [SerializeField] private int displayNumber = 1;         // What players see (1, 2, 3, etc.)
    [SerializeField] private TextMeshProUGUI tableNumberText; // Optional TextMeshPro label
    [SerializeField] private Transform orderUIAnchor;       // For billboard positioning

    [Header("Visual Feedback")]
    [SerializeField] private GameObject orderActiveIndicator;  // Shows when table has order

    [Header("Table Service Timer")]
    [SerializeField] private float orderTimeoutDuration = 60f; // Seconds before order expires

    // ---- Network state ----
    private NetworkVariable<int>   assignedRecipeIndex = new NetworkVariable<int>(-1);
    private NetworkVariable<float> orderAssignedTime   = new NetworkVariable<float>(-1f);

    // ---- Local references ----
    private RecipeSO      currentRecipe;
    private TableOrderUI  tableOrderUI;
    private bool          orderExpiredHandled; // guard against double-firing on server


    private void Awake() {
        // Auto-generate ID if not set or invalid
        if (string.IsNullOrEmpty(tableId) || tableId == "0") {
            tableId = $"table_{displayNumber}";
        }

        // Find the billboard UI (child Canvas with TableOrderUI, or anywhere in children)
        tableOrderUI = GetComponentInChildren<TableOrderUI>(true);
        if (tableOrderUI == null) {
            Debug.LogWarning($"[CustomerTable] Table {displayNumber} (ID: {tableId}) does not have a TableOrderUI script in its children!");
        }
    }

    private void Start() {
        // Register with TableManager when it's ready
        if (TableManager.Instance != null) {
            TableManager.Instance.RegisterTable(this);
        }

        UpdateTableNumberText();

        if (orderActiveIndicator != null) {
            orderActiveIndicator.SetActive(false);
        }

        tableOrderUI?.HideOrder();
        UpdateTableNumberText();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        assignedRecipeIndex.OnValueChanged += OnAssignedRecipeIndexChanged;
        orderAssignedTime.OnValueChanged   += OnOrderAssignedTimeChanged;

        // Sync state for late-joining clients
        ApplyRecipeState(assignedRecipeIndex.Value);
    }

    // ---- NetworkVariable callbacks ----

    private void OnAssignedRecipeIndexChanged(int previousValue, int newValue) {
        ApplyRecipeState(newValue);
    }

    private void OnOrderAssignedTimeChanged(float previousValue, float newValue) {
        // Reset the expiry guard when a new order starts
        if (newValue > 0f) {
            orderExpiredHandled = false;
        }
    }

    private void ApplyRecipeState(int recipeIndex) {
        if (recipeIndex >= 0 && TableManager.Instance != null && TableManager.Instance.GetRecipeListSO() != null) {
            currentRecipe = TableManager.Instance.GetRecipeListSO().recipeSOList[recipeIndex];
            tableOrderUI?.ShowOrder(currentRecipe, displayNumber);
        } else {
            currentRecipe = null;
            tableOrderUI?.HideOrder();
        }

        UpdateVisualState();
    }

    // ---- Per-frame timer (runs on all clients for smooth UI; server also handles expiry) ----

    private void Update() {
        if (!HasActiveOrder()) return;
        if (orderAssignedTime.Value < 0f) return;

        float elapsed     = Time.time - orderAssignedTime.Value;
        float normalized  = 1f - Mathf.Clamp01(elapsed / orderTimeoutDuration);

        // Update the timer bar on every client
        tableOrderUI?.UpdateTimerBar(normalized);

        // Only the server expires the order
        if (IsServer && normalized <= 0f && !orderExpiredHandled) {
            orderExpiredHandled = true;
            HandleExpiredOrder();
        }
    }

    // Auto-trigger when player enters trigger zone
    private void OnTriggerEnter(Collider other) {
        // Only check on client for local player
        if (!IsClient) return;

        // Check if it's the local player
        Player player = other.GetComponent<Player>();
        if (player == null || !player.IsOwner) return;

        // Check if table is empty and player has a plate
        if (!HasKitchenObject() && player.HasKitchenObject()) {
            if (player.GetKitchenObject() is PlateKitchenObject) {
                // Auto-interact!
                Interact(player);
            }
        }
    }

    private void HandleExpiredOrder() {
        if (RestaurantEconomyManager.Instance != null) {
            RestaurantEconomyManager.Instance.ProcessExpiredOrderPenalty();
        }
        TableManager.Instance?.HandleOrderExpired(tableId);
        ClearOrder();
        ShowExpiredOrderFeedbackClientRpc();
    }

    // ---- Interaction ----

    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            if (player.HasKitchenObject()) {
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    if (HasActiveOrder()) {
                        DeliverToTableServerRpc(
                            NetworkObject.NetworkObjectId,
                            player.GetKitchenObject().NetworkObject
                        );
                    } else {
                        ShowNoOrderFeedback();
                    }
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverToTableServerRpc(ulong tableNetworkId, NetworkObjectReference plateRef) {
        if (!plateRef.TryGet(out NetworkObject plateNetworkObject)) return;

        PlateKitchenObject plate = plateNetworkObject.GetComponent<PlateKitchenObject>();
        if (plate == null) return;

        if (!HasActiveOrder()) {
            ShowNoOrderFeedbackClientRpc();
            return;
        }

        bool isCorrect = ValidateDelivery(currentRecipe, plate);

        if (isCorrect) {
            // Calculate patience and performance rating
            float elapsed = Time.time - orderAssignedTime.Value;
            float patience = 1f - Mathf.Clamp01(elapsed / orderTimeoutDuration);

            float stars;
            int tip;
            if (patience >= 0.75f) {
                stars = 5.0f;
                tip = 15;
            } else if (patience >= 0.25f) {
                stars = 4.0f;
                tip = 5;
            } else {
                stars = 3.0f;
                tip = 0;
            }

            int basePayout = 25 + (currentRecipe.kitchenObjectSOList.Count * 5);

            if (RestaurantEconomyManager.Instance != null) {
                RestaurantEconomyManager.Instance.ProcessDeliveryReward(basePayout, tip, stars);
            }

            TableManager.Instance?.HandleCorrectDelivery(tableId);
            ClearOrder();
            ShowCorrectDeliveryFeedbackClientRpc();
            KitchenObject.DestroyKitchenObject(plate);
        } else {
            // Wrong dish delivered -> Customer rejects & cancels order!
            if (RestaurantEconomyManager.Instance != null) {
                RestaurantEconomyManager.Instance.ProcessWrongDeliveryPenalty();
            }

            TableManager.Instance?.HandleWrongDelivery(tableId);
            ClearOrder(); // Order is canceled!
            ShowWrongDeliveryFeedbackClientRpc();
            // Note: KitchenObject.DestroyKitchenObject is NOT called -> player retains plate!
        }
    }

    private bool ValidateDelivery(RecipeSO recipe, PlateKitchenObject plate) {
        if (recipe == null || plate == null) return false;

        if (recipe.kitchenObjectSOList.Count != plate.GetKitchenObjectSOList().Count) {
            return false;
        }

        foreach (KitchenObjectSO recipeIngredient in recipe.kitchenObjectSOList) {
            bool ingredientFound = false;
            foreach (KitchenObjectSO plateIngredient in plate.GetKitchenObjectSOList()) {
                if (plateIngredient == recipeIngredient) {
                    ingredientFound = true;
                    break;
                }
            }
            if (!ingredientFound) return false;
        }

        return true;
    }

    // ---- Order assignment (server only) ----

    public void AssignRecipe(RecipeSO recipe, int recipeIndex) {
        if (!IsServer) return;

        orderExpiredHandled        = false;
        currentRecipe              = recipe;
        assignedRecipeIndex.Value  = recipeIndex;
        orderAssignedTime.Value    = Time.time; // synced to all clients

        Debug.Log($"Table {displayNumber} assigned: {recipe.recipeName} (timeout: {orderTimeoutDuration}s)");
    }

    public void ClearOrder() {
        if (!IsServer) return;

        currentRecipe             = null;
        assignedRecipeIndex.Value = -1;
        orderAssignedTime.Value   = -1f;

        Debug.Log($"Table {displayNumber} order cleared");

        // Notify TableManager to schedule a new order after a random delay
        TableManager.Instance?.NotifyTableBecameEmpty(tableId);
    }

    // ---- Visuals ----

    private void UpdateVisualState() {
        if (orderActiveIndicator != null) {
            orderActiveIndicator.SetActive(HasActiveOrder());
        }
    }

    public void ShowDeliveryResult(bool success) {
        if (tableOrderUI != null) {
            tableOrderUI.ShowDeliveryResult(success);
        }
    }

    [ClientRpc]
    private void ShowCorrectDeliveryFeedbackClientRpc() {
        OnAnyTableDeliverySuccess?.Invoke(this, EventArgs.Empty);
        ShowDeliveryResult(true);
        Debug.Log($"SUCCESS: Correct delivery to Table {displayNumber}!");
    }

    [ClientRpc]
    private void ShowWrongDeliveryFeedbackClientRpc() {
        OnAnyTableDeliveryFailed?.Invoke(this, EventArgs.Empty);
        ShowDeliveryResult(false);
        Debug.Log($"FAILED: Wrong delivery to Table {displayNumber}! Order canceled.");
    }

    [ClientRpc]
    private void ShowExpiredOrderFeedbackClientRpc() {
        OnAnyTableDeliveryFailed?.Invoke(this, EventArgs.Empty);
        ShowDeliveryResult(false);
        Debug.Log($"EXPIRED: Order expired at Table {displayNumber}!");
    }

    [ClientRpc]
    private void ShowNoOrderFeedbackClientRpc() {
        Debug.Log($"Table {displayNumber} has no active order");
    }

    private void ShowNoOrderFeedback() {
        ShowNoOrderFeedbackClientRpc();
    }

    // ---- Public getters ----

    public bool HasActiveOrder()    => assignedRecipeIndex.Value != -1;
    public string GetTableId()      => tableId;
    public int GetDisplayNumber()   => displayNumber;
    public RecipeSO GetAssignedRecipe() => currentRecipe;
    public Transform GetOrderUIAnchor() => orderUIAnchor;

    private void UpdateTableNumberText() {
        if (tableNumberText != null) {
            tableNumberText.text = displayNumber.ToString();
        }
        if (tableOrderUI != null) {
            tableOrderUI.UpdateTableNumber(displayNumber);
        }
    }

    public void SetDisplayNumber(int newNumber) {
        displayNumber = newNumber;
        UpdateTableNumberText();
    }
}

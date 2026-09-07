using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CityLife;

/// <summary>
/// Server-authoritative manager that:
///   - Picks random sidewalk waypoints from the CityLife waypoint graph.
///   - Spawns DeliveryCustomer NPCs there using the Pedestrian prefab.
///   - Validates deliveries when the player presses [E] near a customer.
///   - Awards score and fires DeliveryManager events (success/fail).
///
/// Place this on the same GameObject as DeliveryManager, or on its own.
/// Assign a reference to the CityLifeManager in the Inspector.
/// </summary>
public class DeliveryCustomerManager : NetworkBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static DeliveryCustomerManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("City Integration")]
    [Tooltip("Drag in the CityLifeManager GameObject that runs the CityLife system.")]
    [SerializeField] private CityLifeManager cityLifeManager;

    [Header("Customer Prefab")]
    [Tooltip("Prefab that has BOTH PedestrianController AND DeliveryCustomer scripts on it.")]
    [SerializeField] private GameObject deliveryCustomerPrefab;

    [Header("Orders")]
    [SerializeField] private RecipeListSO recipeListSO;
    [Tooltip("How many active sidewalk customers can exist at once.")]
    [SerializeField] private int maxActiveCustomers = 3;
    [Tooltip("How long each home delivery order lasts (seconds). Much longer than kitchen orders.")]
    [SerializeField] private float orderTimer = 100f;
    [Tooltip("Seconds between attempts to spawn a new sidewalk customer.")]
    [SerializeField] private float spawnInterval = 12f;

    [Header("Delivery Bag")]
    [Tooltip("If true, player must carry a DeliveryBag to complete the order. If false, any matching plate works.")]
    [SerializeField] private bool requireDeliveryBag = false;

    // ── Networked Data Struct ────────────────────────────────────────────────
    public struct SidewalkDeliveryData : INetworkSerializable, IEquatable<SidewalkDeliveryData>
    {
        public int deliveryId;
        public int recipeSOIndex;
        public Vector3 worldPosition;
        public float orderStartTime;
        public float orderDuration;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref deliveryId);
            serializer.SerializeValue(ref recipeSOIndex);
            serializer.SerializeValue(ref worldPosition);
            serializer.SerializeValue(ref orderStartTime);
            serializer.SerializeValue(ref orderDuration);
        }

        public bool Equals(SidewalkDeliveryData other) => deliveryId == other.deliveryId;
    }

    // ── Events ────────────────────────────────────────────────────────────────
    public event EventHandler OnDeliveryOrdersChanged;

    // ── Runtime State ─────────────────────────────────────────────────────────
    private NetworkList<SidewalkDeliveryData> _activeDeliveryDataList;
    private List<DeliveryCustomer> _activeCustomers  = new();
    private CityWaypointGraph      _waypointGraph;
    private System.Random          _rng;
    private float                  _spawnTimer;
    private int                    _nextDeliveryId = 1;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        _activeDeliveryDataList = new NetworkList<SidewalkDeliveryData>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _activeDeliveryDataList.OnListChanged += (changeEvent) =>
        {
            OnDeliveryOrdersChanged?.Invoke(this, EventArgs.Empty);
        };

        if (!IsServer) return;

        _rng        = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
        _spawnTimer = spawnInterval * 0.5f; // first spawn sooner

        // Try to grab the waypoint graph from the CityLifeManager
        StartCoroutine(WaitForCityAndInitialize());
    }

    private IEnumerator WaitForCityAndInitialize()
    {
        // Wait until CityLifeManager has built its graph (may take a frame or two)
        float waited = 0f;
        while (waited < 5f)
        {
            _waypointGraph = TryGetWaypointGraph();
            if (_waypointGraph != null && _waypointGraph.SidewalkWaypoints.Count > 0)
            {
                Debug.Log($"[DeliveryCustomerManager] Waypoint graph ready: {_waypointGraph.SidewalkWaypoints.Count} sidewalk points.");
                yield break;
            }
            yield return new WaitForSeconds(0.25f);
            waited += 0.25f;
        }

        Debug.LogWarning("[DeliveryCustomerManager] Could not obtain a CityWaypointGraph after 5 seconds. " +
                         "Customers will not spawn. Make sure CityLifeManager is initialized before this manager.");
    }

    private void Update()
    {
        if (!IsServer) return;
        if (_waypointGraph == null) return;
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;

        float currentInterval = spawnInterval;
        if (Difficulty.DifficultyManager.Instance != null)
        {
            currentInterval = Difficulty.DifficultyManager.Instance.CurrentSettings.deliverySpawnInterval;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = currentInterval;
            TrySpawnCustomer();
        }
    }

    // ── Spawn Logic ───────────────────────────────────────────────────────────

    private void TrySpawnCustomer()
    {
        // Purge any destroyed/deactivated customers
        _activeCustomers.RemoveAll(c => c == null || !c.gameObject.activeInHierarchy);

        int currentMax = maxActiveCustomers;
        if (Difficulty.DifficultyManager.Instance != null)
        {
            currentMax = Difficulty.DifficultyManager.Instance.CurrentSettings.maxDeliveryCustomers;
        }

        if (_activeCustomers.Count >= currentMax) return;
        if (deliveryCustomerPrefab == null || recipeListSO == null) return;

        // Pick a random sidewalk waypoint far enough from existing customers
        SidewalkWaypoint spot = GetFreeWaypointSpot();
        if (spot == null) return;

        // Pick a random recipe
        int recipeIndex = _rng.Next(recipeListSO.recipeSOList.Count);
        RecipeSO recipe = recipeListSO.recipeSOList[recipeIndex];

        // Spawn and activate the customer (server-side instantiation)
        SpawnCustomerAtSpot(spot.Position, recipe, recipeIndex);
    }

    private SidewalkWaypoint GetFreeWaypointSpot()
    {
        const float minCustomerSeparation = 6f;
        float sqrMin = minCustomerSeparation * minCustomerSeparation;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            SidewalkWaypoint wp = _waypointGraph.GetRandomSidewalkWaypoint(_rng);
            if (wp == null) continue;

            // Make sure it's not too close to an existing customer
            bool tooClose = false;
            foreach (var existing in _activeCustomers)
            {
                if (existing == null) continue;
                if ((existing.transform.position - wp.Position).sqrMagnitude < sqrMin)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) return wp;
        }
        return null;
    }

    private void SpawnCustomerAtSpot(Vector3 position, RecipeSO recipe, int recipeIndex)
    {
        // Instantiate locally on the server only (not a NetworkObject so no NetworkSpawn needed)
        var obj = Instantiate(deliveryCustomerPrefab, position, Quaternion.identity);

        var customer = obj.GetComponent<DeliveryCustomer>();
        if (customer == null)
        {
            Debug.LogError("[DeliveryCustomerManager] deliveryCustomerPrefab is missing a DeliveryCustomer component!");
            Destroy(obj);
            return;
        }

        float currentTimer = orderTimer;
        if (Difficulty.DifficultyManager.Instance != null)
        {
            currentTimer = Difficulty.DifficultyManager.Instance.CurrentSettings.deliveryOrderTimer;
        }

        int deliveryId = _nextDeliveryId++;
        customer.SetDeliveryId(deliveryId);
        customer.ActivateAsCustomer(recipe, position, currentTimer);
        customer.OnDeliverySucceeded += HandleDeliverySucceeded;
        customer.OnDeliveryExpired   += HandleDeliveryExpired;

        _activeCustomers.Add(customer);

        _activeDeliveryDataList.Add(new SidewalkDeliveryData
        {
            deliveryId = deliveryId,
            recipeSOIndex = recipeIndex,
            worldPosition = position,
            orderStartTime = Time.time,
            orderDuration = currentTimer
        });

        // Notify all clients that a new order has appeared on the sidewalk
        NotifyNewCustomerClientRpc(recipe.recipeName, position);

        Debug.Log($"[DeliveryCustomerManager] Spawned customer {deliveryId} at {position} waiting for '{recipe.recipeName}'.");
    }

    // ── Delivery Validation ───────────────────────────────────────────────────

    /// <summary>
    /// Called by DeliveryCustomer when the player presses [E] near it.
    /// Validates the plate/bag the player is holding against the customer's order.
    /// </summary>
    public void TryDeliverToCustomer(DeliveryCustomer customer, Player player)
    {
        if (!customer.IsActive()) return;

        RecipeSO required = customer.GetRecipe();
        if (!player.HasKitchenObject())
        {
            customer.FlashWrongDeliveryFeedback();
            NotifyWrongDeliveryClientRpc(player.GetComponent<NetworkObject>().OwnerClientId);
            DeliveryManager.Instance.TriggerDeliveryFailed();
            return;
        }

        KitchenObject held = player.GetKitchenObject();

        int ingredientCount = (required != null && required.kitchenObjectSOList != null) ? required.kitchenObjectSOList.Count : 3;
        float patience = customer.GetPatienceNormalized();
        int basePayout = 35 + (ingredientCount * 8);
        int tip = 10;
        float stars = 5f;
        if (Difficulty.DifficultyManager.Instance != null)
        {
            Difficulty.DifficultyManager.Instance.CalculateDeliveryPayout(ingredientCount, patience, out basePayout, out tip, out stars);
        }

        // 1. Direct Plate
        if (held is PlateKitchenObject plate && PlateMatchesRecipe(plate, required))
        {
            DestroyThePlate(player, plate);
            customer.CompleteDelivery(basePayout + tip);
            return;
        }

        // 2. Delivery Bag containing matching dish
        if (held is DeliveryBagKitchenObject bag && bag.HasMatchingDish(required))
        {
            bag.TryConsumeMatchingDish(required);
            customer.CompleteDelivery(basePayout + tip);
            return;
        }

        // FAIL (wrong food or empty) -> Flash ❌ visual feedback and play sound, but do NOT take item or charge money penalty
        customer.FlashWrongDeliveryFeedback();
        NotifyWrongDeliveryClientRpc(player.GetComponent<NetworkObject>().OwnerClientId);
        DeliveryManager.Instance.TriggerDeliveryFailed();
    }

    public List<DeliveryCustomer> GetActiveCustomers() => _activeCustomers;

    private bool PlateMatchesRecipe(PlateKitchenObject plate, RecipeSO recipe)
    {
        var plateIngredients  = plate.GetKitchenObjectSOList();
        var recipeIngredients = recipe.kitchenObjectSOList;

        if (plateIngredients.Count != recipeIngredients.Count) return false;

        foreach (var ingredient in recipeIngredients)
        {
            if (!plateIngredients.Contains(ingredient)) return false;
        }

        return true;
    }

    private void DestroyThePlate(Player player, PlateKitchenObject plate)
    {
        // Use the existing KitchenObject destroy pipeline
        KitchenObject.DestroyKitchenObject(plate);
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void HandleDeliverySucceeded(DeliveryCustomer customer)
    {
        customer.OnDeliverySucceeded -= HandleDeliverySucceeded;
        customer.OnDeliveryExpired   -= HandleDeliveryExpired;
        _activeCustomers.Remove(customer);
        RemoveDeliveryData(customer.GetDeliveryId());

        float currentInterval = spawnInterval;
        if (Difficulty.DifficultyManager.Instance != null)
        {
            currentInterval = Difficulty.DifficultyManager.Instance.CurrentSettings.deliverySpawnInterval;
        }
        _spawnTimer = Mathf.Max(_spawnTimer, currentInterval);

        // Calculate economy payout, tip & review rating based on patience
        float patience = customer.GetPatienceNormalized();
        RecipeSO recipe = customer.GetRecipe();
        int ingredientCount = (recipe != null && recipe.kitchenObjectSOList != null) ? recipe.kitchenObjectSOList.Count : 3;

        int basePayout;
        int tip;
        float stars;

        if (Difficulty.DifficultyManager.Instance != null)
        {
            Difficulty.DifficultyManager.Instance.CalculateDeliveryPayout(ingredientCount, patience, out basePayout, out tip, out stars);
        }
        else
        {
            // Fallback default
            basePayout = 35 + (ingredientCount * 8);
            tip = patience >= 0.75f ? 25 : (patience >= 0.4f ? 15 : (patience >= 0.2f ? 8 : 3));
            stars = patience >= 0.75f ? 5f : (patience >= 0.4f ? 4.5f : (patience >= 0.2f ? 4f : 3f));
        }

        if (RestaurantEconomyManager.Instance != null)
        {
            RestaurantEconomyManager.Instance.ProcessDeliveryReward(basePayout, tip, stars);
        }

        // Award score & audio via DeliveryManager
        DeliveryManager.Instance.TriggerDeliverySuccess();

        Debug.Log($"[DeliveryCustomerManager] Home delivery succeeded! Earned ${basePayout} + ${tip} tip ({stars:F1}★)");
    }

    private void HandleDeliveryExpired(DeliveryCustomer customer)
    {
        customer.OnDeliverySucceeded -= HandleDeliverySucceeded;
        customer.OnDeliveryExpired   -= HandleDeliveryExpired;
        _activeCustomers.Remove(customer);
        RemoveDeliveryData(customer.GetDeliveryId());

        float currentInterval = spawnInterval;
        if (Difficulty.DifficultyManager.Instance != null)
        {
            currentInterval = Difficulty.DifficultyManager.Instance.CurrentSettings.deliverySpawnInterval;
        }
        _spawnTimer = Mathf.Max(_spawnTimer, currentInterval);

        if (RestaurantEconomyManager.Instance != null)
        {
            int fine = Difficulty.DifficultyManager.Instance != null ? Difficulty.DifficultyManager.Instance.GetDeliveryWrongOrderFine() : 15;
            RestaurantEconomyManager.Instance.ProcessExpiredOrderPenalty(fine);
        }

        DeliveryManager.Instance.TriggerDeliveryFailed();

        Debug.Log("[DeliveryCustomerManager] Home delivery order expired! Rating penalty applied.");
    }

    private void RemoveDeliveryData(int deliveryId)
    {
        for (int i = 0; i < _activeDeliveryDataList.Count; i++)
        {
            if (_activeDeliveryDataList[i].deliveryId == deliveryId)
            {
                _activeDeliveryDataList.RemoveAt(i);
                break;
            }
        }
    }

    public NetworkList<SidewalkDeliveryData> GetActiveDeliveryDataList() => _activeDeliveryDataList;

    public RecipeSO GetRecipeSO(int index)
    {
        if (recipeListSO != null && index >= 0 && index < recipeListSO.recipeSOList.Count)
        {
            return recipeListSO.recipeSOList[index];
        }
        return null;
    }

    // ── Client RPCs (notifications) ───────────────────────────────────────────

    [ClientRpc]
    private void NotifyNewCustomerClientRpc(string recipeName, Vector3 position)
    {
        Debug.Log($"[DeliveryCustomerManager] New sidewalk order: '{recipeName}' at {position}");
        // UI toast / notification can be added here
    }

    [ClientRpc]
    private void NotifyWrongDeliveryClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.LocalClientId == playerId)
            Debug.Log("[DeliveryCustomerManager] Wrong food! This customer wants something else.");
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Tries to get the CityWaypointGraph from the CityLifeManager via reflection
    /// (the field is private in the original).  Falls back gracefully.
    /// </summary>
    private CityWaypointGraph TryGetWaypointGraph()
    {
        if (cityLifeManager == null)
        {
            cityLifeManager = FindFirstObjectByType<CityLifeManager>();
            if (cityLifeManager == null) return null;
        }

        // Use reflection to grab the private _graph field from CityLifeManager
        var field = typeof(CityLifeManager).GetField("_graph",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return null;

        return field.GetValue(cityLifeManager) as CityWaypointGraph;
    }
}

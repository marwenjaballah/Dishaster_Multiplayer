using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CityLife;

/// <summary>
/// Server-authoritative manager that:
///   - Picks random sidewalk waypoints from the CityLife waypoint graph.
///   - Spawns DeliveryCustomer NPCs using the Pedestrian pool across all multiplayer clients.
///   - Synchronizes active outdoor deliveries for joining players via NetworkList.
///   - Validates deliveries when any player (host or client) interacts near a customer.
///   - Awards score and fires DeliveryManager events (success/fail).
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

    [Header("Object Pooling")]
    [Tooltip("How many customer instances to pre-instantiate at start to eliminate runtime GC allocations.")]
    [SerializeField] private int prewarmPoolSize = 6;

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
    private readonly List<DeliveryCustomer> _activeCustomers = new();
    private readonly Queue<DeliveryCustomer> _customerPool = new();
    private Transform              _poolParent;
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
        InitializePool();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializePool();

        // Listen for list changes to synchronize customer NPCs & HUD indicators
        _activeDeliveryDataList.OnListChanged += (changeEvent) =>
        {
            SyncActiveCustomersWithNetworkList();
        };

        // Immediately sync any existing active orders upon spawning/joining
        SyncActiveCustomersWithNetworkList();

        if (!IsServer) return;

        _rng        = new System.Random(UnityEngine.Random.Range(0, int.MaxValue));
        _spawnTimer = spawnInterval * 0.5f; // first spawn sooner

        // Try to grab the waypoint graph from the CityLifeManager
        StartCoroutine(WaitForCityAndInitialize());
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _customerPool.Clear();
        _activeCustomers.Clear();
    }

    private void InitializePool()
    {
        if (deliveryCustomerPrefab == null) return;
        if (_poolParent == null)
        {
            var poolGo = new GameObject("DeliveryCustomerPool");
            _poolParent = poolGo.transform;
            _poolParent.SetParent(transform);
        }

        int targetSize = Mathf.Max(maxActiveCustomers * 2, prewarmPoolSize);
        while (_customerPool.Count < targetSize)
        {
            var obj = Instantiate(deliveryCustomerPrefab, _poolParent);
            obj.name = $"DeliveryCustomer_Pooled_{_customerPool.Count:00}";
            obj.SetActive(false);
            var customer = obj.GetComponent<DeliveryCustomer>();
            if (customer != null)
            {
                _customerPool.Enqueue(customer);
            }
        }
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

    // ── Synchronization for Server & Clients ──────────────────────────────────

    /// <summary>
    /// Synchronizes the local active DeliveryCustomer NPCs to match the current NetworkList.
    /// Runs seamlessly on both Server and joining Clients.
    /// </summary>
    private void SyncActiveCustomersWithNetworkList()
    {
        if (_activeDeliveryDataList == null) return;

        HashSet<int> activeIds = new HashSet<int>();

        for (int i = 0; i < _activeDeliveryDataList.Count; i++)
        {
            SidewalkDeliveryData data = _activeDeliveryDataList[i];
            activeIds.Add(data.deliveryId);

            DeliveryCustomer existing = _activeCustomers.Find(c => c != null && c.GetDeliveryId() == data.deliveryId);
            if (existing == null)
            {
                DeliveryCustomer customer = GetCustomerFromPool(data.worldPosition);
                if (customer != null)
                {
                    RecipeSO recipe = GetRecipeSO(data.recipeSOIndex);
                    float elapsed = Time.time - data.orderStartTime;
                    float remaining = Mathf.Max(0.1f, data.orderDuration - elapsed);

                    customer.SetDeliveryId(data.deliveryId);
                    customer.ActivateAsCustomer(recipe, data.worldPosition, remaining);

                    if (IsServer)
                    {
                        customer.OnDeliverySucceeded += HandleDeliverySucceeded;
                        customer.OnDeliveryExpired   += HandleDeliveryExpired;
                    }

                    _activeCustomers.Add(customer);
                }
            }
        }

        // Clean up customers no longer in the active network list
        for (int i = _activeCustomers.Count - 1; i >= 0; i--)
        {
            DeliveryCustomer customer = _activeCustomers[i];
            if (customer == null || !activeIds.Contains(customer.GetDeliveryId()))
            {
                _activeCustomers.RemoveAt(i);
                if (customer != null)
                {
                    if (IsServer)
                    {
                        customer.OnDeliverySucceeded -= HandleDeliverySucceeded;
                        customer.OnDeliveryExpired   -= HandleDeliveryExpired;
                    }
                    ReturnCustomerToPool(customer);
                }
            }
        }

        OnDeliveryOrdersChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Spawn Logic (Server Only) ─────────────────────────────────────────────

    private void TrySpawnCustomer()
    {
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
        float currentTimer = orderTimer;
        if (Difficulty.DifficultyManager.Instance != null)
        {
            currentTimer = Difficulty.DifficultyManager.Instance.CurrentSettings.deliveryOrderTimer;
        }

        int deliveryId = _nextDeliveryId++;

        // Add to networked list; OnListChanged will spawn visuals across all clients
        _activeDeliveryDataList.Add(new SidewalkDeliveryData
        {
            deliveryId = deliveryId,
            recipeSOIndex = recipeIndex,
            worldPosition = position,
            orderStartTime = Time.time,
            orderDuration = currentTimer
        });

        NotifyNewCustomerClientRpc(recipe.recipeName, position);
        Debug.Log($"[DeliveryCustomerManager] Spawned sidewalk customer {deliveryId} at {position} waiting for '{recipe.recipeName}'.");
    }

    private DeliveryCustomer GetCustomerFromPool(Vector3 position)
    {
        DeliveryCustomer customer = null;
        while (_customerPool.Count > 0 && customer == null)
        {
            customer = _customerPool.Dequeue();
        }

        if (customer == null)
        {
            if (deliveryCustomerPrefab == null) return null;
            var obj = Instantiate(deliveryCustomerPrefab, _poolParent);
            obj.name = $"DeliveryCustomer_Pooled_Auto";
            customer = obj.GetComponent<DeliveryCustomer>();
        }

        if (customer != null)
        {
            customer.transform.position = position;
            customer.gameObject.SetActive(true);
        }
        return customer;
    }

    public void ReturnCustomerToPool(DeliveryCustomer customer)
    {
        if (customer == null) return;

        customer.ResetCustomer();
        customer.gameObject.SetActive(false);
        if (_poolParent != null)
        {
            customer.transform.SetParent(_poolParent);
        }
        _customerPool.Enqueue(customer);
    }

    // ── Delivery Validation & Execution ───────────────────────────────────────

    /// <summary>
    /// Called by DeliveryCustomer when a player presses [E] near it.
    /// Handles both local host execution and client ServerRpc invocation.
    /// </summary>
    public void TryDeliverToCustomer(DeliveryCustomer customer, Player player)
    {
        if (customer == null || !customer.IsActive() || player == null) return;

        if (IsServer)
        {
            ProcessDelivery(customer.GetDeliveryId(), player);
        }
        else
        {
            if (player.TryGetComponent(out NetworkObject netObj))
            {
                RequestDeliverToCustomerServerRpc(customer.GetDeliveryId(), netObj);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDeliverToCustomerServerRpc(int deliveryId, NetworkObjectReference playerNetObjRef, ServerRpcParams rpcParams = default)
    {
        if (playerNetObjRef.TryGet(out NetworkObject playerNetObj))
        {
            Player player = playerNetObj.GetComponent<Player>();
            if (player != null)
            {
                ProcessDelivery(deliveryId, player);
            }
        }
    }

    private void ProcessDelivery(int deliveryId, Player player)
    {
        DeliveryCustomer customer = _activeCustomers.Find(c => c != null && c.GetDeliveryId() == deliveryId);
        if (customer == null || !customer.IsActive()) return;

        RecipeSO required = customer.GetRecipe();
        if (!player.HasKitchenObject())
        {
            customer.FlashWrongDeliveryFeedback();
            NotifyWrongDeliveryClientRpc(player.OwnerClientId);
            PlayDeliveryResultClientRpc(deliveryId, false);
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
            PlayDeliveryResultClientRpc(deliveryId, true);
            customer.CompleteDelivery(basePayout + tip);
            return;
        }

        // 2. Delivery Bag containing matching dish
        if (held is DeliveryBagKitchenObject bag && bag.HasMatchingDish(required))
        {
            bag.TryConsumeMatchingDish(required);
            PlayDeliveryResultClientRpc(deliveryId, true);
            customer.CompleteDelivery(basePayout + tip);
            return;
        }

        // FAIL (wrong food or empty)
        customer.FlashWrongDeliveryFeedback();
        NotifyWrongDeliveryClientRpc(player.OwnerClientId);
        PlayDeliveryResultClientRpc(deliveryId, false);
        DeliveryManager.Instance.TriggerDeliveryFailed();
    }

    [ClientRpc]
    private void PlayDeliveryResultClientRpc(int deliveryId, bool success)
    {
        if (IsServer) return; // Server already processed locally

        DeliveryCustomer cust = _activeCustomers.Find(c => c != null && c.GetDeliveryId() == deliveryId);
        if (cust != null)
        {
            if (success)
            {
                cust.CompleteDelivery();
            }
            else
            {
                cust.FlashWrongDeliveryFeedback();
            }
        }
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
    }

    [ClientRpc]
    private void NotifyWrongDeliveryClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == playerId)
            Debug.Log("[DeliveryCustomerManager] Wrong food! This customer wants something else.");
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private CityWaypointGraph TryGetWaypointGraph()
    {
        if (cityLifeManager == null)
        {
            cityLifeManager = FindFirstObjectByType<CityLifeManager>();
            if (cityLifeManager == null) return null;
        }

        var field = typeof(CityLifeManager).GetField("_graph",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field == null) return null;

        return field.GetValue(cityLifeManager) as CityWaypointGraph;
    }
}

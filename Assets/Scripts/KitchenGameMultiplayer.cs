using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameMultiplayer : NetworkBehaviour {


    public const int MAX_PLAYER_AMOUNT = 4;
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";


    public static KitchenGameMultiplayer Instance { get; private set; }


    public static bool playMultiplayer = true;

    // When true, lobby routes to GameSceneTableService with per-table orders
    public static bool tableServiceMode = false;


    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;


    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;
    [SerializeField] private List<Color> playerColorList;


    private NetworkList<PlayerData> playerDataNetworkList;
    private string playerName;



    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(100, 1000));

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;

        RegisterKitchenObjectPrefabs();
    }

    private void RegisterKitchenObjectPrefabs() {
        if (NetworkManager.Singleton == null || kitchenObjectListSO == null) return;

        foreach (KitchenObjectSO koSO in kitchenObjectListSO.kitchenObjectSOList) {
            if (koSO != null && koSO.prefab != null) {
                GameObject prefabGO = koSO.prefab.gameObject;
                if (prefabGO.GetComponent<NetworkObject>() != null) {
                    if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(prefabGO)) {
                        NetworkManager.Singleton.AddNetworkPrefab(prefabGO);
                    }
                }
            }
        }
    }

    private void Start() {
        if (!playMultiplayer) {
            // Singleplayer
            StartHost();
            Loader.Scene targetScene = tableServiceMode 
                ? Loader.Scene.GameSceneTableService 
                : Loader.Scene.GameScene;
            Loader.LoadNetwork(targetScene);
        }
    }

    public string GetPlayerName() {
        return playerName;
    }

    public void SetPlayerName(string playerName) {
        this.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }

    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent) {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void StartHost() {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Clear any stale data when first spawning
        if (IsServer && playerDataNetworkList.Count == 0) {
            Debug.Log("KitchenGameMultiplayer spawned on server - initializing clean player list");
        }

        // Wait for player to be added to the list before setting name/ID
        if (IsClient) {
            StartCoroutine(SetPlayerInfoWhenReady());
        }
    }

    private System.Collections.IEnumerator SetPlayerInfoWhenReady() {
        // Wait until this client's data exists in the list
        while (GetPlayerDataIndexFromClientId(NetworkManager.Singleton.LocalClientId) == -1) {
            yield return null;
        }

        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId) {
        for (int i = 0; i < playerDataNetworkList.Count; i++) {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId) {
                // Disconnected!
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientId) {
        playerDataNetworkList.Add(new PlayerData {
            clientId = clientId,
            colorId = GetFirstUnusedColorId(),
        });
        // Player name and ID will be set in OnNetworkSpawn() when the NetworkBehaviour is ready
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse) {
        // The host/server is always approved
        if (NetworkManager.Singleton != null && 
            (connectionApprovalRequest.ClientNetworkId == NetworkManager.Singleton.LocalClientId || connectionApprovalRequest.ClientNetworkId == NetworkManager.ServerClientId)) {
            connectionApprovalResponse.Approved = true;
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        bool isJoinableScene = currentScene == Loader.Scene.CharacterSelectScene.ToString() ||
                               currentScene == Loader.Scene.LobbyScene.ToString();

        if (!isJoinableScene) {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started";
            return;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT) {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        connectionApprovalResponse.Approved = true;
    }

    public void StartClient() {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId) {
        // Player name and ID will be set in OnNetworkSpawn() when the NetworkBehaviour is ready
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPlayerNameServerRpc(string playerName, RpcParams rpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(rpcParams.Receive.SenderClientId);

        if (playerDataIndex == -1) {
            Debug.LogError($"Player data not found for client {rpcParams.Receive.SenderClientId}");
            return;
        }

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPlayerIdServerRpc(string playerId, RpcParams rpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(rpcParams.Receive.SenderClientId);

        if (playerDataIndex == -1) {
            Debug.LogError($"Player data not found for client {rpcParams.Receive.SenderClientId}");
            return;
        }

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId) {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent) {
        if (!IsSpawned) {
            Debug.LogError("KitchenGameMultiplayer is not spawned on the network yet!");
            return;
        }
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference) {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        if (kitchenObjectParent.HasKitchenObject()) {
            // Parent already spawned an object
            return;
        }

        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenObjectNetworkObject.Spawn(true);

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();


        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
    }

    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO) {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex) {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }

    public KitchenObjectSO GetKitchenObjectSOByName(string objectName) {
        if (kitchenObjectListSO == null || kitchenObjectListSO.kitchenObjectSOList == null) return null;
        foreach (var so in kitchenObjectListSO.kitchenObjectSOList) {
            if (so != null && (so.objectName == objectName || so.name == objectName)) return so;
        }
        return null;
    }




    public void DestroyKitchenObject(KitchenObject kitchenObject) {
        if (!IsSpawned) {
            Debug.LogError("KitchenGameMultiplayer is not spawned on the network yet!");
            return;
        }
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);

        if (kitchenObjectNetworkObject == null) {
            // This object is already destroyed
            return;
        }

        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        ClearKitchenObjectOnParentClientRpc(kitchenObjectNetworkObjectReference);

        kitchenObject.DestroySelf();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClearKitchenObjectOnParentClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParent();
    }



    public bool IsPlayerIndexConnected(int playerIndex) {
        return playerIndex < playerDataNetworkList.Count;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId) {
        for (int i=0; i< playerDataNetworkList.Count; i++) {
            if (playerDataNetworkList[i].clientId == clientId) {
                return i;
            }
        }
        return -1;
    }

    public PlayerData GetPlayerDataFromClientId(ulong clientId) {
        foreach (PlayerData playerData in playerDataNetworkList) {
            if (playerData.clientId == clientId) {
                return playerData;
            }
        }
        return default;
    }

    public PlayerData GetPlayerData() {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex) {
        return playerDataNetworkList[playerIndex];
    }

    public Color GetPlayerColor(int colorId) {
        return playerColorList[colorId];
    }

    public void ChangePlayerColor(int colorId) {
        if (!IsSpawned) {
            Debug.LogError("KitchenGameMultiplayer is not spawned on the network yet! Cannot change player color.");
            return;
        }
        ChangePlayerColorServerRpc(colorId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ChangePlayerColorServerRpc(int colorId, RpcParams rpcParams = default) {
        if (!IsColorAvailable(colorId)) {
            // Color not available
            return;
        }

        int playerDataIndex = GetPlayerDataIndexFromClientId(rpcParams.Receive.SenderClientId);

        if (playerDataIndex == -1) {
            Debug.LogError($"Player data not found for client {rpcParams.Receive.SenderClientId}");
            return;
        }

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.colorId = colorId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private bool IsColorAvailable(int colorId) {
        foreach (PlayerData playerData in playerDataNetworkList) {
            if (playerData.colorId == colorId) {
                // Already in use
                return false;
            }
        }
        return true;
    }

    private int GetFirstUnusedColorId() {
        for (int i = 0; i<playerColorList.Count; i++) {
            if (IsColorAvailable(i)) {
                return i;
            }
        }
        return -1;
    }

    public void KickPlayer(ulong clientId) {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }

}
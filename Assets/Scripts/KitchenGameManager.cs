using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class KitchenGameManager : NetworkBehaviour {


    public static KitchenGameManager Instance { get; private set; }



    public event EventHandler OnStateChanged;
    public event EventHandler OnLocalGamePaused;
    public event EventHandler OnLocalGameUnpaused;
    public event EventHandler OnMultiplayerGamePaused;
    public event EventHandler OnMultiplayerGameUnpaused;
    public event EventHandler OnLocalPlayerReadyChanged;
    public event EventHandler OnRematchVotesChanged;


    private enum State {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }


    [SerializeField] private Transform playerPrefab;


    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);
    private bool isLocalPlayerReady;
    private NetworkVariable<float> countdownToStartTimer = new NetworkVariable<float>(3f);
    private NetworkVariable<float> gamePlayingTimer = new NetworkVariable<float>(0f);
    private float gamePlayingTimerMax = 900f;
    private bool isLocalGamePaused = false;
    private NetworkVariable<bool> isGamePaused = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isBankruptcyGameOver = new NetworkVariable<bool>(false);
    private NetworkVariable<int> rematchVotesCount = new NetworkVariable<int>(0);
    private NetworkVariable<int> totalPlayersCount = new NetworkVariable<int>(1);

    private Dictionary<ulong, bool> playerReadyDictionary;
    private Dictionary<ulong, bool> playerPausedDictionary;
    private Dictionary<ulong, bool> playerRematchDictionary;
    private bool autoTestGamePausedState;
    private bool localPlayerVotedRematch = false;


    private void Awake() {
        Instance = this;

        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerPausedDictionary = new Dictionary<ulong, bool>();
        playerRematchDictionary = new Dictionary<ulong, bool>();
    }

    private void Start() {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    public override void OnNetworkSpawn() {
        state.OnValueChanged += State_OnValueChanged;
        isGamePaused.OnValueChanged += IsGamePaused_OnValueChanged;
        rematchVotesCount.OnValueChanged += RematchVotes_OnValueChanged;
        totalPlayersCount.OnValueChanged += RematchVotes_OnValueChanged;

        if (IsServer) {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }

    private void RematchVotes_OnValueChanged(int previousValue, int newValue) {
        OnRematchVotesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId) {
        autoTestGamePausedState = true;
    }

    private void IsGamePaused_OnValueChanged(bool previousValue, bool newValue) {
        if (isGamePaused.Value) {
            Time.timeScale = 0f;

            OnMultiplayerGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            Time.timeScale = 1f;

            OnMultiplayerGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    private void State_OnValueChanged(State previousValue, State newValue) {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e) {
        if (state.Value == State.WaitingToStart) {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);

            SetPlayerReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default) {
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId]) {
                // This player is NOT ready
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady) {
            state.Value = State.CountdownToStart;
        }
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e) {
        TogglePauseGame();
    }

    private void Update() {
        if (!IsServer) {
            return;
        }

        switch (state.Value) {
            case State.WaitingToStart:
                break;
            case State.CountdownToStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f) {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = gamePlayingTimerMax;
                }
                break;
            case State.GamePlaying:
                // Classic Mode: Count down timer to end match.
                // Table Service Mode: Endless survival (ends strictly on Bankruptcy).
                if (!KitchenGameMultiplayer.tableServiceMode) {
                    gamePlayingTimer.Value -= Time.deltaTime;
                    if (gamePlayingTimer.Value < 0f) {
                        state.Value = State.GameOver;
                    }
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void LateUpdate() {
        if (autoTestGamePausedState) {
            autoTestGamePausedState = false;
            TestGamePausedState();
        }
    }

    public bool IsGamePlaying() {
        return state.Value == State.GamePlaying;
    }

    public bool IsCountdownToStartActive() {
        return state.Value == State.CountdownToStart;
    }

    public float GetCountdownToStartTimer() {
        return countdownToStartTimer.Value;
    }

    public bool IsGameOver() {
        return state.Value == State.GameOver;
    }

    public bool IsBankruptcyGameOver() {
        return isBankruptcyGameOver.Value;
    }

    public bool IsWaitingToStart() {
        return state.Value == State.WaitingToStart;
    }

    public bool IsLocalPlayerReady() {
        return isLocalPlayerReady;
    }

    public float GetGamePlayingTimerNormalized() {
        return 1 - (gamePlayingTimer.Value / gamePlayingTimerMax);
    }

    // ===== BANKRUPTCY GAME OVER TRIGGER =====
    public void TriggerBankruptcyGameOver() {
        if (IsServer) {
            isBankruptcyGameOver.Value = true;
            state.Value = State.GameOver;
        } else {
            TriggerBankruptcyGameOverServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBankruptcyGameOverServerRpc(ServerRpcParams serverRpcParams = default) {
        isBankruptcyGameOver.Value = true;
        state.Value = State.GameOver;
    }

    // ===== MULTIPLAYER REMATCH / REPLAY VOTING SYSTEM =====
    public bool HasLocalPlayerVotedRematch() {
        return localPlayerVotedRematch;
    }

    public int GetRematchVotesCount() {
        return rematchVotesCount.Value;
    }

    public int GetTotalPlayersCount() {
        return totalPlayersCount.Value;
    }

    public void ToggleLocalPlayerRematchVote() {
        localPlayerVotedRematch = !localPlayerVotedRematch;
        VoteRematchServerRpc(localPlayerVotedRematch);
        OnRematchVotesChanged?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void VoteRematchServerRpc(bool vote, ServerRpcParams serverRpcParams = default) {
        ulong senderId = serverRpcParams.Receive.SenderClientId;
        playerRematchDictionary[senderId] = vote;

        int votes = 0;
        int total = NetworkManager.Singleton.ConnectedClientsIds.Count;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (playerRematchDictionary.ContainsKey(clientId) && playerRematchDictionary[clientId]) {
                votes++;
            }
        }

        rematchVotesCount.Value = votes;
        totalPlayersCount.Value = total;

        // If all connected players have voted for rematch, restart the match!
        if (votes >= total && total > 0) {
            RestartGameSession();
        }
    }

    private void RestartGameSession() {
        if (!IsServer) return;
        Loader.Scene targetScene = KitchenGameMultiplayer.tableServiceMode
            ? Loader.Scene.GameSceneTableService
            : Loader.Scene.GameScene;
        Loader.LoadNetwork(targetScene);
    }

    public void LeaveGameSession() {
        if (KitchenGameLobby.Instance != null) {
            KitchenGameLobby.Instance.LeaveLobby();
        }
        if (NetworkManager.Singleton != null) {
            NetworkManager.Singleton.Shutdown();
        }
        Loader.Load(Loader.Scene.MainMenuScene);
    }

    public bool IsLocalGamePaused() {
        return isLocalGamePaused;
    }

    public void TogglePauseGame() {
        isLocalGamePaused = !isLocalGamePaused;
        if (isLocalGamePaused) {
            PauseGameServerRpc();

            OnLocalGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            UnpauseGameServerRpc();

            OnLocalGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PauseGameServerRpc(ServerRpcParams serverRpcParams = default) {
        playerPausedDictionary[serverRpcParams.Receive.SenderClientId] = true;

        TestGamePausedState();
    }

    [ServerRpc(RequireOwnership = false)]
    private void UnpauseGameServerRpc(ServerRpcParams serverRpcParams = default) {
        playerPausedDictionary[serverRpcParams.Receive.SenderClientId] = false;

        TestGamePausedState();
    }

    private void TestGamePausedState() {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (playerPausedDictionary.ContainsKey(clientId) && playerPausedDictionary[clientId]) {
                // This player is paused
                isGamePaused.Value = true;
                return;
            }
        }

        // All players are unpaused
        isGamePaused.Value = false;
    }

}
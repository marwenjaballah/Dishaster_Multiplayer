using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative and solo-mode assist system that enables dual-chef hot swapping.
/// In single-player offline sessions, spawns a second chef and allows the player
/// to instantly toggle active control between Chef A (Kitchen) and Chef B (Courier/Front) via [Tab] or Gamepad [Y].
/// </summary>
public class SoloChefHotSwapManager : MonoBehaviour
{
    public static SoloChefHotSwapManager Instance { get; private set; }

    public static void ResetStaticData()
    {
        Instance = null;
    }

    public static event EventHandler<int> OnActiveChefChanged;

    [Header("Solo Setup")]
    [SerializeField] private Vector3 chefBSpawnPosition = new Vector3(110.64f, 0f, -23.0f); // Service window / Scooter side
    [SerializeField] private float swapCooldown = 0.25f;

    private Player _chefA;
    private Player _chefB;
    private int _activeChefIndex = 0; // 0 = Chef A, 1 = Chef B
    private float _lastSwapTime = -10f;
    private bool _isSoloModeActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnSwapChefAction += GameInput_OnSwapChefAction;
        }

        Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnSwapChefAction -= GameInput_OnSwapChefAction;
        }

        Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
    }

    private void Player_OnAnyPlayerSpawned(object sender, EventArgs e)
    {
        CheckAndInitializeSoloAssist();
    }

    private void CheckAndInitializeSoloAssist()
    {
        if (_chefB != null) return; // Already initialized

        // Check if session is solo (1 player)
        bool isMultiplayer = NetworkManager.Singleton != null && 
                             NetworkManager.Singleton.IsListening && 
                             NetworkManager.Singleton.ConnectedClients.Count > 1;

        if (isMultiplayer)
        {
            _isSoloModeActive = false;
            return;
        }

        if (Player.LocalInstance == null) return;

        _chefA = Player.LocalInstance;
        _isSoloModeActive = true;

        SpawnChefB();
    }

    private void SpawnChefB()
    {
        if (_chefA == null) return;

        // Instantiate Chef B from Chef A's prefab structure
        GameObject chefBGo = Instantiate(_chefA.gameObject, chefBSpawnPosition, Quaternion.Euler(0f, 90f, 0f));
        chefBGo.name = "Chef_B_SoloAssistant";

        _chefB = chefBGo.GetComponent<Player>();
        if (_chefB == null) return;

        // If Netcode is running in local host mode, spawn the network object
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = chefBGo.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);
            }
        }

        // Tint Chef B with alternate visual identity (e.g. Yellow color)
        PlayerVisual visual = chefBGo.GetComponentInChildren<PlayerVisual>();
        if (visual != null && KitchenGameMultiplayer.Instance != null)
        {
            visual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(1)); // Color Index 1
        }

        // Start Chef B in idle stationary mode
        _chefB.SetInputActive(false);

        Debug.Log("[SoloChefHotSwapManager] Solo Assist Active: Spawned Chef B at " + chefBSpawnPosition);
    }

    private void GameInput_OnSwapChefAction(object sender, EventArgs e)
    {
        if (!_isSoloModeActive || _chefA == null || _chefB == null) return;
        if (Time.time - _lastSwapTime < swapCooldown) return;

        _lastSwapTime = Time.time;
        SwapActiveChef();
    }

    public void SwapActiveChef()
    {
        if (_chefA == null || _chefB == null) return;

        _activeChefIndex = 1 - _activeChefIndex;
        Player newActive = _activeChefIndex == 0 ? _chefA : _chefB;
        Player oldActive = _activeChefIndex == 0 ? _chefB : _chefA;

        // 1. Deactivate old chef input
        oldActive.SetInputActive(false);

        // 2. Switch LocalInstance so camera and inputs attach to the new chef
        Player.SetLocalInstance(newActive);
        newActive.SetInputActive(true);

        OnActiveChefChanged?.Invoke(this, _activeChefIndex);

        // Audio and logging feedback
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayWarningSound(newActive.transform.position);
        }

        Debug.Log($"[SoloChefHotSwapManager] Swapped active control to {(_activeChefIndex == 0 ? "Chef A (Kitchen)" : "Chef B (Courier)")}");
    }

    public int GetActiveChefIndex() => _activeChefIndex;
    public bool IsSoloMode() => _isSoloModeActive;
}

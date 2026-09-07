using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Central manager for sudden emergency kitchen events (Blackout, Fire, Water Leak, etc.).
/// Fully synchronized via Netcode with clean C# event architecture.
/// </summary>
public class KitchenCrisisManager : NetworkBehaviour {

    public static KitchenCrisisManager Instance { get; private set; }

    // ===== CRISIS EVENTS =====
    public event EventHandler OnBlackoutStarted;
    public event EventHandler OnBlackoutEnded;
    public event EventHandler<OnFireSpawnedEventArgs> OnFireSpawned;
    public event EventHandler<OnFireExtinguishedEventArgs> OnFireExtinguished;
    public event EventHandler OnWaterLeakStarted;
    public event EventHandler OnWaterLeakEnded;
    public event EventHandler OnCrisisStateChanged;

    public class OnFireSpawnedEventArgs : EventArgs {
        public BaseCounter targetCounter;
    }

    public class OnFireExtinguishedEventArgs : EventArgs {
        public BaseCounter targetCounter;
    }

    [Header("Auto Crisis Settings")]
    [SerializeField] private bool autoCrisisEnabled = false;
    [SerializeField] private float minCrisisInterval = 40f;
    [SerializeField] private float maxCrisisInterval = 80f;

    [Header("References")]
    [SerializeField] private List<BaseCounter> potentialFireCounters = new List<BaseCounter>();
    [SerializeField] private WaterLeakValveCounter waterLeakValve;

    // Networked States
    private NetworkVariable<bool> isBlackoutActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isAutoCrisisActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isWaterLeakActive = new NetworkVariable<bool>(false);

    private float crisisTimer;
    private List<FireHazard> activeFires = new List<FireHazard>();

    private void Awake() {
        Instance = this;
    }

    public override void OnNetworkSpawn() {
        isBlackoutActive.OnValueChanged += IsBlackoutActive_OnValueChanged;
        isAutoCrisisActive.OnValueChanged += IsAutoCrisisActive_OnValueChanged;
        isWaterLeakActive.OnValueChanged += IsWaterLeakActive_OnValueChanged;

        if (IsServer) {
            isAutoCrisisActive.Value = autoCrisisEnabled;
            ResetCrisisTimer();
        }
    }

    public override void OnNetworkDespawn() {
        isBlackoutActive.OnValueChanged -= IsBlackoutActive_OnValueChanged;
        isAutoCrisisActive.OnValueChanged -= IsAutoCrisisActive_OnValueChanged;
        isWaterLeakActive.OnValueChanged -= IsWaterLeakActive_OnValueChanged;
    }

    private void Update() {
        if (!IsServer) return;

        // Check initial crisis immunity (calm start)
        if (Difficulty.DifficultyManager.Instance != null && Difficulty.DifficultyManager.Instance.IsInInitialCrisisImmunity()) {
            return;
        }

        if (isAutoCrisisActive.Value && KitchenGameManager.Instance != null && KitchenGameManager.Instance.IsGamePlaying()) {
            crisisTimer -= Time.deltaTime;
            if (crisisTimer <= 0f) {
                TriggerRandomCrisis();
                ResetCrisisTimer();
            }
        }
    }

    private void ResetCrisisTimer() {
        float minInterval = minCrisisInterval;
        float maxInterval = maxCrisisInterval;

        if (Difficulty.DifficultyManager.Instance != null) {
            var settings = Difficulty.DifficultyManager.Instance.CurrentSettings;
            minInterval = settings.crisisMinInterval;
            maxInterval = settings.crisisMaxInterval;
        }

        crisisTimer = UnityEngine.Random.Range(minInterval, maxInterval);
    }

    private void TriggerRandomCrisis() {
        float rand = UnityEngine.Random.value;
        if (rand < 0.33f && !isBlackoutActive.Value) {
            TriggerBlackoutServerRpc();
        } else if (rand < 0.66f) {
            TriggerFireServerRpc();
        } else {
            TriggerWaterLeakServerRpc();
        }
    }

    // ===== NETWORK EVENT CALLBACKS =====

    private void IsBlackoutActive_OnValueChanged(bool previousValue, bool newValue) {
        if (newValue) {
            OnBlackoutStarted?.Invoke(this, EventArgs.Empty);
        } else {
            OnBlackoutEnded?.Invoke(this, EventArgs.Empty);
        }
        OnCrisisStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void IsAutoCrisisActive_OnValueChanged(bool previousValue, bool newValue) {
        OnCrisisStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void IsWaterLeakActive_OnValueChanged(bool previousValue, bool newValue) {
        if (newValue) {
            OnWaterLeakStarted?.Invoke(this, EventArgs.Empty);
        } else {
            OnWaterLeakEnded?.Invoke(this, EventArgs.Empty);
        }
        OnCrisisStateChanged?.Invoke(this, EventArgs.Empty);
    }

    // ===== SERVER RPCS / CRISIS TRIGGERS =====

    [ServerRpc(RequireOwnership = false)]
    public void TriggerBlackoutServerRpc() {
        if (isBlackoutActive.Value) return;
        isBlackoutActive.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndBlackoutServerRpc() {
        if (!isBlackoutActive.Value) return;
        isBlackoutActive.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerWaterLeakServerRpc() {
        if (isWaterLeakActive.Value) return;
        isWaterLeakActive.Value = true;

        if (waterLeakValve == null) {
            waterLeakValve = FindFirstObjectByType<WaterLeakValveCounter>();
        }

        if (waterLeakValve != null) {
            waterLeakValve.TriggerBurst();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndWaterLeakServerRpc() {
        if (!isWaterLeakActive.Value) return;
        isWaterLeakActive.Value = false;

        if (waterLeakValve == null) {
            waterLeakValve = FindFirstObjectByType<WaterLeakValveCounter>();
        }

        if (waterLeakValve != null) {
            waterLeakValve.RepairImmediately();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerFireServerRpc() {
        BaseCounter targetCounter = GetRandomEligibleCounter();
        if (targetCounter == null) return;

        IgniteCounterClientRpc(targetCounter.NetworkObject);
    }

    [ClientRpc]
    private void IgniteCounterClientRpc(NetworkObjectReference counterRef) {
        if (counterRef.TryGet(out NetworkObject counterNetObj)) {
            BaseCounter counter = counterNetObj.GetComponent<BaseCounter>();
            if (counter != null) {
                FireHazard fireHazard = counter.GetComponent<FireHazard>();
                if (fireHazard == null) {
                    fireHazard = counter.gameObject.AddComponent<FireHazard>();
                }
                fireHazard.Ignite();

                if (!activeFires.Contains(fireHazard)) {
                    activeFires.Add(fireHazard);
                }

                OnFireSpawned?.Invoke(this, new OnFireSpawnedEventArgs {
                    targetCounter = counter
                });
                OnCrisisStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestExtinguishCounterServerRpc(NetworkObjectReference counterRef) {
        ExtinguishCounterClientRpc(counterRef);
    }

    [ClientRpc]
    private void ExtinguishCounterClientRpc(NetworkObjectReference counterRef) {
        if (counterRef.TryGet(out NetworkObject counterNetObj)) {
            FireHazard fireHazard = counterNetObj.GetComponent<FireHazard>();
            if (fireHazard != null) {
                fireHazard.ExtinguishImmediately();
                if (activeFires.Contains(fireHazard)) {
                    activeFires.Remove(fireHazard);
                }
                BaseCounter counter = counterNetObj.GetComponent<BaseCounter>();
                OnFireExtinguished?.Invoke(this, new OnFireExtinguishedEventArgs {
                    targetCounter = counter
                });
                OnCrisisStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExtinguishAllFiresServerRpc() {
        ExtinguishAllFiresClientRpc();
    }

    [ClientRpc]
    private void ExtinguishAllFiresClientRpc() {
        foreach (FireHazard fire in activeFires.ToArray()) {
            if (fire != null) {
                fire.ExtinguishImmediately();
            }
        }
        activeFires.Clear();
        OnCrisisStateChanged?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetAllCrisesServerRpc() {
        isBlackoutActive.Value = false;
        isWaterLeakActive.Value = false;
        if (waterLeakValve != null) {
            waterLeakValve.RepairImmediately();
        }
        ExtinguishAllFiresClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleAutoCrisisServerRpc() {
        isAutoCrisisActive.Value = !isAutoCrisisActive.Value;
        if (isAutoCrisisActive.Value) {
            ResetCrisisTimer();
        }
    }

    private BaseCounter GetRandomEligibleCounter() {
        if (potentialFireCounters.Count == 0) {
            StoveCounter[] stoves = FindObjectsByType<StoveCounter>(FindObjectsSortMode.None);
            if (stoves.Length > 0) {
                return stoves[UnityEngine.Random.Range(0, stoves.Length)];
            }
            ClearCounter[] clearCounters = FindObjectsByType<ClearCounter>(FindObjectsSortMode.None);
            if (clearCounters.Length > 0) {
                return clearCounters[UnityEngine.Random.Range(0, clearCounters.Length)];
            }
            return null;
        }

        return potentialFireCounters[UnityEngine.Random.Range(0, potentialFireCounters.Count)];
    }

    // ===== PUBLIC GETTERS =====
    public bool IsBlackout() => isBlackoutActive.Value;
    public bool IsWaterLeakActive() => isWaterLeakActive.Value;
    public bool IsAutoCrisisEnabled() => isAutoCrisisActive.Value;
    public int ActiveFireCount => activeFires.Count;
    public bool HasActiveCrisis => isBlackoutActive.Value || activeFires.Count > 0 || isWaterLeakActive.Value;
}

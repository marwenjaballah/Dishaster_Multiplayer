using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlatesCounter : BaseCounter {

    public event EventHandler OnPlateSpawned;
    public event EventHandler OnPlateRemoved;

    [SerializeField] private KitchenObjectSO plateKitchenObjectSO;
    [SerializeField] private bool infinitePlates = false;
    [SerializeField] private int initialPlatesAmount = 5;
    [SerializeField] private int platesSpawnedAmountMax = 5;

    private float spawnPlateTimer;
    private float spawnPlateTimerMax = 4f;
    private int platesSpawnedAmount;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (IsServer) {
            if (!infinitePlates) {
                SetInitialPlatesClientRpc(initialPlatesAmount);
            }
            if (DayLoopManager.Instance != null) {
                DayLoopManager.OnAnyDayPhaseChanged += DayLoopManager_OnAnyDayPhaseChanged;
            }
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (IsServer) {
            DayLoopManager.OnAnyDayPhaseChanged -= DayLoopManager_OnAnyDayPhaseChanged;
        }
    }

    private void DayLoopManager_OnAnyDayPhaseChanged(object sender, DayLoopManager.OnDayPhaseChangedEventArgs e) {
        if (!IsServer || infinitePlates) return;
        if (e.newPhase == DayLoopManager.DayPhase.Shift1_MorningPrep) {
            int needed = initialPlatesAmount - platesSpawnedAmount;
            if (needed > 0) {
                RestockPlatesClientRpc(needed);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetInitialPlatesClientRpc(int amount) {
        platesSpawnedAmount = amount;
        for (int i = 0; i < amount; i++) {
            OnPlateSpawned?.Invoke(this, EventArgs.Empty);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RestockPlatesClientRpc(int countToAdd) {
        platesSpawnedAmount += countToAdd;
        for (int i = 0; i < countToAdd; i++) {
            OnPlateSpawned?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Update() {
        if (!IsServer) {
            return;
        }

        // In finite plate mode, plates are not generated out of thin air
        if (!infinitePlates) {
            return;
        }

        spawnPlateTimer += Time.deltaTime;
        if (spawnPlateTimer > spawnPlateTimerMax) {
            spawnPlateTimer = 0f;

            if (KitchenGameManager.Instance.IsGamePlaying() && platesSpawnedAmount < platesSpawnedAmountMax) {
                SpawnPlateServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlateServerRpc() {
        SpawnPlateClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnPlateClientRpc() {
        platesSpawnedAmount++;

        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }

    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            // Player is empty handed -> pick up clean plate if available
            if (platesSpawnedAmount > 0) {
                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);

                InteractLogicServerRpc();
            }
        } else {
            // Player is carrying something -> check if returning a clean plate to rack
            if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                // Must be an empty plate with zero ingredients
                if (plateKitchenObject.GetKitchenObjectSOList().Count == 0 && platesSpawnedAmount < platesSpawnedAmountMax) {
                    ReturnPlateServerRpc(player.GetKitchenObject().NetworkObject);
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReturnPlateServerRpc(NetworkObjectReference plateNetworkObjectReference) {
        if (platesSpawnedAmount >= platesSpawnedAmountMax) return;

        if (plateNetworkObjectReference.TryGet(out NetworkObject plateNetworkObject)) {
            KitchenObject plateKitchenObject = plateNetworkObject.GetComponent<KitchenObject>();
            if (plateKitchenObject != null) {
                KitchenObject.DestroyKitchenObject(plateKitchenObject);
                ReturnPlateClientRpc();
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReturnPlateClientRpc() {
        platesSpawnedAmount++;
        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicServerRpc() {
        InteractLogicClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InteractLogicClientRpc() {
        platesSpawnedAmount--;

        OnPlateRemoved?.Invoke(this, EventArgs.Empty);
    }

}
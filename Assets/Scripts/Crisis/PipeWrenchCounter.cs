using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tool rack / stand for the Pipe Wrench.
/// Player interacts with E to pick up the Pipe Wrench with empty hands.
/// Player interacts with E while holding the Pipe Wrench to return it to the rack.
/// Fully synchronized over Unity Netcode.
/// </summary>
public class PipeWrenchCounter : BaseCounter {

    public static event EventHandler OnAnyWrenchPickedUp;
    public static event EventHandler OnAnyWrenchReturned;

    public event EventHandler OnWrenchPickedUp;
    public event EventHandler OnWrenchReturned;

    [Header("Configuration")]
    [SerializeField] private KitchenObjectSO pipeWrenchSO;
    [SerializeField] private GameObject visualWrenchModel;

    private NetworkVariable<bool> hasWrenchOnStand = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        hasWrenchOnStand.OnValueChanged += HasWrenchOnStand_OnValueChanged;
        UpdateVisuals(hasWrenchOnStand.Value);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        hasWrenchOnStand.OnValueChanged -= HasWrenchOnStand_OnValueChanged;
    }

    private void HasWrenchOnStand_OnValueChanged(bool previousValue, bool newValue) {
        UpdateVisuals(newValue);
    }

    private void UpdateVisuals(bool hasWrench) {
        if (visualWrenchModel != null) {
            visualWrenchModel.SetActive(hasWrench);
        }
    }

    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            // Player is empty-handed: request pick up from stand
            if (hasWrenchOnStand.Value) {
                InteractPickupServerRpc(player.GetNetworkObject());
            }
        } else {
            // Player is carrying something
            if (player.GetKitchenObject().GetKitchenObjectSO() == pipeWrenchSO) {
                // Request return wrench to the stand
                InteractReturnServerRpc(player.GetKitchenObject().NetworkObject);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractPickupServerRpc(NetworkObjectReference playerNetworkObjectReference) {
        if (!hasWrenchOnStand.Value) return;

        if (playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject)) {
            Player player = playerNetworkObject.GetComponent<Player>();
            if (player != null && !player.HasKitchenObject()) {
                hasWrenchOnStand.Value = false;
                KitchenObject.SpawnKitchenObject(pipeWrenchSO, player);
                InteractPickupClientRpc();
            }
        }
    }

    [ClientRpc]
    private void InteractPickupClientRpc() {
        OnWrenchPickedUp?.Invoke(this, EventArgs.Empty);
        OnAnyWrenchPickedUp?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractReturnServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        if (hasWrenchOnStand.Value) return;

        if (kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject)) {
            KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
            if (kitchenObject != null && kitchenObject.GetKitchenObjectSO() == pipeWrenchSO) {
                KitchenObject.DestroyKitchenObject(kitchenObject);
                hasWrenchOnStand.Value = true;
                InteractReturnClientRpc();
            }
        }
    }

    [ClientRpc]
    private void InteractReturnClientRpc() {
        OnWrenchReturned?.Invoke(this, EventArgs.Empty);
        OnAnyWrenchReturned?.Invoke(this, EventArgs.Empty);
    }

    public void RestoreWrench() {
        if (!hasWrenchOnStand.Value) {
            hasWrenchOnStand.Value = true;
            InteractReturnClientRpc();
        }
    }
}

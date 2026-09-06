using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Fire Extinguisher Counter stand.
/// Player interacts with E to pick up the portable Fire Extinguisher into their hands.
/// Player sprays with F (Interact Alternate).
/// Player places/drops it back with E.
/// Fully synchronized over Unity Netcode.
/// </summary>
public class FireExtinguisherCounter : BaseCounter {

    public static event EventHandler OnAnyExtinguisherPickedUp;
    public static event EventHandler OnAnyExtinguisherReturned;

    // ===== EVENTS =====
    public event EventHandler OnExtinguisherPickedUp;
    public event EventHandler OnExtinguisherReturned;

    [Header("Configuration")]
    [SerializeField] private KitchenObjectSO fireExtinguisherSO;
    [SerializeField] private GameObject visualExtinguisherModel;

    private NetworkVariable<bool> hasExtinguisherOnStand = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        hasExtinguisherOnStand.OnValueChanged += HasExtinguisherOnStand_OnValueChanged;
        UpdateVisuals(hasExtinguisherOnStand.Value);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        hasExtinguisherOnStand.OnValueChanged -= HasExtinguisherOnStand_OnValueChanged;
    }

    private void HasExtinguisherOnStand_OnValueChanged(bool previousValue, bool newValue) {
        UpdateVisuals(newValue);
    }

    private void UpdateVisuals(bool hasExtinguisher) {
        if (visualExtinguisherModel != null) {
            visualExtinguisherModel.SetActive(hasExtinguisher);
        }
    }

    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            // Player is empty-handed: request pick up from stand
            if (hasExtinguisherOnStand.Value) {
                InteractPickupServerRpc(player.GetNetworkObject());
            }
        } else {
            // Player is carrying something
            if (player.GetKitchenObject().GetKitchenObjectSO() == fireExtinguisherSO) {
                // Request return extinguisher to the stand
                InteractReturnServerRpc(player.GetKitchenObject().NetworkObject);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractPickupServerRpc(NetworkObjectReference playerNetworkObjectReference) {
        if (!hasExtinguisherOnStand.Value) return;

        if (playerNetworkObjectReference.TryGet(out NetworkObject playerNetworkObject)) {
            Player player = playerNetworkObject.GetComponent<Player>();
            if (player != null && !player.HasKitchenObject()) {
                hasExtinguisherOnStand.Value = false;
                KitchenObject.SpawnKitchenObject(fireExtinguisherSO, player);
                InteractPickupClientRpc();
            }
        }
    }

    [ClientRpc]
    private void InteractPickupClientRpc() {
        OnExtinguisherPickedUp?.Invoke(this, EventArgs.Empty);
        OnAnyExtinguisherPickedUp?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractReturnServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        if (hasExtinguisherOnStand.Value) return;

        if (kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject)) {
            KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
            if (kitchenObject != null && kitchenObject.GetKitchenObjectSO() == fireExtinguisherSO) {
                KitchenObject.DestroyKitchenObject(kitchenObject);
                hasExtinguisherOnStand.Value = true;
                InteractReturnClientRpc();
            }
        }
    }

    [ClientRpc]
    private void InteractReturnClientRpc() {
        OnExtinguisherReturned?.Invoke(this, EventArgs.Empty);
        OnAnyExtinguisherReturned?.Invoke(this, EventArgs.Empty);
    }

    public void RestoreExtinguisher() {
        if (!hasExtinguisherOnStand.Value) {
            hasExtinguisherOnStand.Value = true;
            InteractReturnClientRpc();
        }
    }
}

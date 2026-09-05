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
            // Player is empty-handed: pick up wrench from the stand
            if (hasWrenchOnStand.Value) {
                InteractPickupServerRpc();
                KitchenObject.SpawnKitchenObject(pipeWrenchSO, player);
            }
        } else {
            // Player is carrying something
            if (player.GetKitchenObject().GetKitchenObjectSO() == pipeWrenchSO) {
                // Return wrench to the stand
                KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
                InteractReturnServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractPickupServerRpc() {
        hasWrenchOnStand.Value = false;
        InteractPickupClientRpc();
    }

    [ClientRpc]
    private void InteractPickupClientRpc() {
        OnWrenchPickedUp?.Invoke(this, EventArgs.Empty);
        OnAnyWrenchPickedUp?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractReturnServerRpc() {
        hasWrenchOnStand.Value = true;
        InteractReturnClientRpc();
    }

    [ClientRpc]
    private void InteractReturnClientRpc() {
        OnWrenchReturned?.Invoke(this, EventArgs.Empty);
        OnAnyWrenchReturned?.Invoke(this, EventArgs.Empty);
    }
}

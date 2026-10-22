using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative Dishwashing Station counter.
/// Players deposit dirty plates and scrub them clean via hold/tap InteractAlternate ('F').
/// Once fully scrubbed, the dirty plate transforms into a sparkling clean PlateKitchenObject.
/// </summary>
public class SinkCounter : BaseCounter, IHasProgress {

    public static event EventHandler OnAnyWashed;

    new public static void ResetStaticData() {
        OnAnyWashed = null;
    }

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnWash;
    public event EventHandler OnWashComplete;

    [Header("Dishwashing Settings")]
    [SerializeField] private KitchenObjectSO cleanPlateKitchenObjectSO;
    [SerializeField] private int washProgressMax = 4;

    private int washProgress;

    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            // Sink is empty
            if (player.HasKitchenObject()) {
                // Only accept dirty plates into the sink basin
                if (player.GetKitchenObject() is DirtyPlateKitchenObject || player.GetKitchenObject().TryGetDirtyPlate(out _)) {
                    KitchenObject dirtyPlate = player.GetKitchenObject();
                    dirtyPlate.SetKitchenObjectParent(this);
                    ResetWashProgressServerRpc();
                }
            }
        } else {
            // Sink contains an object
            if (!player.HasKitchenObject()) {
                // Empty-handed player can pick up whatever is in the sink (clean plate or dirty plate)
                GetKitchenObject().SetKitchenObjectParent(player);
                ResetWashProgressServerRpc();
            }
        }
    }

    public override void InteractAlternate(Player player) {
        if (HasKitchenObject() && GetKitchenObject() is DirtyPlateKitchenObject) {
            WashPlateServerRpc();
            TestWashProgressDoneServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ResetWashProgressServerRpc() {
        ResetWashProgressClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ResetWashProgressClientRpc() {
        washProgress = 0;
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void WashPlateServerRpc() {
        if (HasKitchenObject() && GetKitchenObject() is DirtyPlateKitchenObject) {
            WashPlateClientRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void WashPlateClientRpc() {
        washProgress++;

        OnWash?.Invoke(this, EventArgs.Empty);
        OnAnyWashed?.Invoke(this, EventArgs.Empty);

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = Mathf.Clamp01((float)washProgress / washProgressMax)
        });
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TestWashProgressDoneServerRpc() {
        if (HasKitchenObject() && GetKitchenObject() is DirtyPlateKitchenObject) {
            if (washProgress >= washProgressMax) {
                KitchenObject dirtyPlate = GetKitchenObject();
                KitchenObject.DestroyKitchenObject(dirtyPlate);

                if (cleanPlateKitchenObjectSO == null && KitchenGameMultiplayer.Instance != null) {
                    cleanPlateKitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOByName("Plate");
                }

                if (cleanPlateKitchenObjectSO != null) {
                    KitchenObject.SpawnKitchenObject(cleanPlateKitchenObjectSO, this);
                }
                WashCompleteClientRpc();
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void WashCompleteClientRpc() {
        washProgress = 0;
        OnWashComplete?.Invoke(this, EventArgs.Empty);
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });
    }
}

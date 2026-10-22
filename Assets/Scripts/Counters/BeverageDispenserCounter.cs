using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative Beverage Dispenser / Soda Fountain Station.
/// Players interact to fill cold drinks with live pouring progress feedback (IHasProgress).
/// Supports meal combo packaging into Plates and Delivery Bags.
/// </summary>
public class BeverageDispenserCounter : BaseCounter, IHasProgress {

    public static event EventHandler OnAnyDrinkDispensed;
    public static event EventHandler OnAnyDispenseStarted;

    new public static void ResetStaticData() {
        OnAnyDrinkDispensed = null;
        OnAnyDispenseStarted = null;
    }

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnPourStarted;
    public event EventHandler OnPourCompleted;

    [Header("Beverage Settings")]
    [SerializeField] private float dispenseDuration = 2.0f;
    [SerializeField] private KitchenObjectSO beverageKitchenObjectSO;
    [SerializeField] private AudioSource pourAudioSource;

    private readonly NetworkVariable<float> dispenseTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<bool> isDispensing = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        dispenseTimer.OnValueChanged += DispenseTimer_OnValueChanged;
        isDispensing.OnValueChanged += IsDispensing_OnValueChanged;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        dispenseTimer.OnValueChanged -= DispenseTimer_OnValueChanged;
        isDispensing.OnValueChanged -= IsDispensing_OnValueChanged;
    }

    private void DispenseTimer_OnValueChanged(float prev, float curr) {
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = Mathf.Clamp01(curr / dispenseDuration)
        });
    }

    private void IsDispensing_OnValueChanged(bool prev, bool curr) {
        if (curr) {
            OnPourStarted?.Invoke(this, EventArgs.Empty);
            if (pourAudioSource != null && !pourAudioSource.isPlaying) {
                pourAudioSource.Play();
            }
        } else {
            OnPourCompleted?.Invoke(this, EventArgs.Empty);
            if (pourAudioSource != null && pourAudioSource.isPlaying) {
                pourAudioSource.Stop();
            }
            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
                progressNormalized = 0f
            });
        }
    }

    private void Update() {
        if (!IsServer) return;

        // Blackout pauses the electric soda fountain
        if (KitchenCrisisManager.Instance != null && KitchenCrisisManager.Instance.IsBlackout()) {
            return;
        }

        if (isDispensing.Value) {
            dispenseTimer.Value += Time.deltaTime;

            if (dispenseTimer.Value >= dispenseDuration) {
                isDispensing.Value = false;
                dispenseTimer.Value = 0f;

                // Spawn the dispensed drink onto the counter if not already occupied
                if (!HasKitchenObject()) {
                    KitchenObjectSO drinkSO = beverageKitchenObjectSO;
                    if (drinkSO == null && KitchenGameMultiplayer.Instance != null) {
                        drinkSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOByName("SodaCup");
                    }
                    if (drinkSO != null) {
                        KitchenObject.SpawnKitchenObject(drinkSO, this);
                    }
                }

                NotifyDrinkCompleteClientRpc();
            }
        }
    }

    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            if (!isDispensing.Value) {
                // Trigger dispensing
                StartDispensingServerRpc();
            }
        } else {
            // Counter already has a dispensed drink
            if (!player.HasKitchenObject()) {
                // Empty handed player picks up the drink
                GetKitchenObject().SetKitchenObjectParent(player);
            } else {
                // If player has a Plate, try to add the drink as a combo side
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plate)) {
                    if (plate.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    }
                }
            }
        }
    }

    public override void InteractAlternate(Player player) {
        // Quick dispense shortcut with Alternate Interact
        if (!HasKitchenObject() && !isDispensing.Value) {
            StartDispensingServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartDispensingServerRpc() {
        if (isDispensing.Value) return;

        dispenseTimer.Value = 0f;
        isDispensing.Value = true;
        OnAnyDispenseStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyDrinkCompleteClientRpc() {
        OnAnyDrinkDispensed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsDispensing() => isDispensing.Value;
}

using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A wall-mounted or counter-based Water Shutoff Valve / Pipe that bursts during a Water Leak crisis.
/// Players hold the 'F' key for 3 seconds while holding the Pipe Wrench to turn the valve and stop the flood.
/// When fixed, the valve cannot be interacted with and shows no prompts.
/// </summary>
public class WaterLeakValveCounter : BaseCounter, IHasProgress {

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnValveRepaired;
    public event EventHandler OnValveBurst;

    [Header("Valve Settings")]
    [SerializeField] private int clicksRequired = 3;
    [SerializeField] private Transform valveWheelTransform;
    [SerializeField] private KitchenObjectSO requiredPipeWrenchSO;

    private NetworkVariable<int> currentClicks = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isBurst = new NetworkVariable<bool>(false);

    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 3.0f;
    private InteractKeyPromptUI keyPromptUI;
    private WaterLeakHazard waterLeakHazard;

    private void Awake() {
        waterLeakHazard = GetComponent<WaterLeakHazard>();
        if (waterLeakHazard == null) {
            waterLeakHazard = gameObject.AddComponent<WaterLeakHazard>();
        }

        keyPromptUI = GetComponentInChildren<InteractKeyPromptUI>(true);
        if (keyPromptUI != null) {
            keyPromptUI.ConfigureHold(true, holdDuration, "F");
            keyPromptUI.SetCanShowPredicate(CanPlayerInteractWithValve);
            keyPromptUI.OnHoldCompleted += KeyPromptUI_OnHoldCompleted;
        }
    }

    private void Start() {
        if (keyPromptUI != null) {
            keyPromptUI.ConfigureHold(true, holdDuration, "F");
            keyPromptUI.SetCanShowPredicate(CanPlayerInteractWithValve);
        }
    }

    private bool CanPlayerInteractWithValve() {
        if (!isBurst.Value) return false;
        if (Player.LocalInstance == null || !Player.LocalInstance.HasKitchenObject()) return false;

        KitchenObject heldObject = Player.LocalInstance.GetKitchenObject();
        return (heldObject is PortablePipeWrench) || 
               (requiredPipeWrenchSO != null && heldObject.GetKitchenObjectSO() == requiredPipeWrenchSO);
    }

    private void KeyPromptUI_OnHoldCompleted() {
        if (!isBurst.Value) return;
        if (Player.LocalInstance == null || !Player.LocalInstance.HasKitchenObject()) return;

        KitchenObject heldObject = Player.LocalInstance.GetKitchenObject();
        bool isWrench = (heldObject is PortablePipeWrench) || 
                        (requiredPipeWrenchSO != null && heldObject.GetKitchenObjectSO() == requiredPipeWrenchSO);

        if (!isWrench) return;

        if (heldObject is PortablePipeWrench wrench) {
            wrench.Use(Player.LocalInstance);
        }

        CompleteRepairServerRpc();
    }

    private void Update() {
        // While holding 'F' facing the burst valve with the wrench, rotate the wheel continuously
        if (isBurst.Value && Player.LocalInstance != null && Player.LocalInstance.HasKitchenObject()) {
            KitchenObject heldObject = Player.LocalInstance.GetKitchenObject();
            bool isWrench = (heldObject is PortablePipeWrench) || 
                            (requiredPipeWrenchSO != null && heldObject.GetKitchenObjectSO() == requiredPipeWrenchSO);

            bool isFPressed = false;
            if (GameInput.Instance != null) {
                isFPressed = GameInput.Instance.IsInteractAlternatePressed();
            }
            if (!isFPressed) {
                isFPressed = Input.GetKey(KeyCode.F);
            }

            if (isWrench && isFPressed) {
                if (valveWheelTransform != null) {
                    valveWheelTransform.Rotate(Vector3.up, 180f * Time.deltaTime, Space.Self);
                }
            }
        }
    }

    public override void Interact(Player player) {
        // 'E' key does not interact with the valve. Fixing the valve strictly requires holding 'F' with the Pipe Wrench.
    }

    public override void InteractAlternate(Player player) {
        if (!isBurst.Value) return;
        // Holding 'F' is managed by InteractKeyPromptUI and KeyPromptUI_OnHoldCompleted
    }

    [ServerRpc(RequireOwnership = false)]
    private void CompleteRepairServerRpc() {
        if (!isBurst.Value) return;

        isBurst.Value = false;
        currentClicks.Value = 0;

        if (waterLeakHazard != null) {
            waterLeakHazard.StopLeakImmediately();
        }

        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.EndWaterLeakServerRpc();
        }

        OnValveRepaired?.Invoke(this, EventArgs.Empty);
    }

    public void TriggerBurst() {
        if (!IsServer) return;

        currentClicks.Value = 0;
        isBurst.Value = true;

        if (waterLeakHazard != null) {
            waterLeakHazard.StartLeak();
        }

        OnValveBurst?.Invoke(this, EventArgs.Empty);
    }

    public void RepairImmediately() {
        if (!IsServer) return;

        isBurst.Value = false;
        currentClicks.Value = 0;

        if (waterLeakHazard != null) {
            waterLeakHazard.StopLeakImmediately();
        }

        OnValveRepaired?.Invoke(this, EventArgs.Empty);
    }

    public bool IsBurst() => isBurst.Value;
}

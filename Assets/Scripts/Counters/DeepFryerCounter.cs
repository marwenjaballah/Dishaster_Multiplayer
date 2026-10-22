using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative Deep Fryer station for fast-food frying (French Fries, Crispy Patties).
/// Features oil sizzle audio, progress feedback (IHasProgress), and state synchronization.
/// Supports Night Market SpeedyStove upgrade and pauses during blackout crises.
/// </summary>
public class DeepFryerCounter : BaseCounter, IHasProgress {

    public static event EventHandler OnAnyFried;
    public static event EventHandler OnAnyFryStarted;

    new public static void ResetStaticData() {
        OnAnyFried = null;
        OnAnyFryStarted = null;
    }

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public class OnStateChangedEventArgs : EventArgs {
        public State state;
    }

    public enum State {
        Idle,
        Frying,
        Fried,
        Burned,
    }

    [Header("Fryer Settings")]
    [SerializeField] private float fryTimerMax = 4.0f;
    [SerializeField] private float burnTimerMax = 6.0f;
    [SerializeField] private KitchenObjectSO friedOutputSO;
    [SerializeField] private KitchenObjectSO burnedOutputSO;
    [SerializeField] private AudioSource sizzleAudioSource;

    private readonly NetworkVariable<State> state = new NetworkVariable<State>(State.Idle);
    private readonly NetworkVariable<float> fryingTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> burningTimer = new NetworkVariable<float>(0f);

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        fryingTimer.OnValueChanged += FryingTimer_OnValueChanged;
        burningTimer.OnValueChanged += BurningTimer_OnValueChanged;
        state.OnValueChanged += State_OnValueChanged;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        fryingTimer.OnValueChanged -= FryingTimer_OnValueChanged;
        burningTimer.OnValueChanged -= BurningTimer_OnValueChanged;
        state.OnValueChanged -= State_OnValueChanged;
    }

    private void FryingTimer_OnValueChanged(float previousValue, float newValue) {
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = fryingTimer.Value / fryTimerMax
        });
    }

    private void BurningTimer_OnValueChanged(float previousValue, float newValue) {
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = burningTimer.Value / burnTimerMax
        });
    }

    private void State_OnValueChanged(State previousState, State newState) {
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
            state = state.Value
        });

        if (sizzleAudioSource != null) {
            if (state.Value == State.Frying) {
                if (!sizzleAudioSource.isPlaying) sizzleAudioSource.Play();
            } else {
                if (sizzleAudioSource.isPlaying) sizzleAudioSource.Stop();
            }
        }

        if (state.Value == State.Burned || state.Value == State.Idle) {
            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
                progressNormalized = 0f
            });
        }
    }

    private void Update() {
        if (!IsServer) return;

        // Crisis event: pause cooking when blackout cuts power
        if (KitchenCrisisManager.Instance != null && KitchenCrisisManager.Instance.IsBlackout()) {
            return;
        }

        if (HasKitchenObject()) {
            switch (state.Value) {
                case State.Idle:
                    break;

                case State.Frying:
                    float cookSpeed = 1f;
                    if (NightMarketManager.Instance != null && NightMarketManager.Instance.HasUpgrade(NightMarketManager.UpgradeType.SpeedyStove)) {
                        cookSpeed = 1.30f;
                    }
                    fryingTimer.Value += Time.deltaTime * cookSpeed;

                    if (fryingTimer.Value >= fryTimerMax) {
                        KitchenObject currentObj = GetKitchenObject();
                        KitchenObjectSO outputSO = friedOutputSO;
                        if (outputSO == null && currentObj != null) {
                            outputSO = currentObj.GetKitchenObjectSO();
                        }

                        KitchenObject.DestroyKitchenObject(currentObj);

                        if (outputSO != null) {
                            KitchenObject.SpawnKitchenObject(outputSO, this);
                        }

                        state.Value = State.Fried;
                        burningTimer.Value = 0f;
                        OnAnyFried?.Invoke(this, EventArgs.Empty);
                    }
                    break;

                case State.Fried:
                    burningTimer.Value += Time.deltaTime;

                    if (burningTimer.Value >= burnTimerMax) {
                        KitchenObject currentObj = GetKitchenObject();
                        KitchenObject.DestroyKitchenObject(currentObj);

                        if (burnedOutputSO != null) {
                            KitchenObject.SpawnKitchenObject(burnedOutputSO, this);
                        }

                        state.Value = State.Burned;
                    }
                    break;

                case State.Burned:
                    break;
            }
        }
    }

    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            // Fryer is empty
            if (player.HasKitchenObject()) {
                KitchenObject kitchenObject = player.GetKitchenObject();
                // Transfer ingredient into fryer
                kitchenObject.SetKitchenObjectParent(this);
                StartFryingServerRpc();
            }
        } else {
            // Fryer contains food
            if (player.HasKitchenObject()) {
                // If player holds plate, try to plate the fried food
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        SetStateIdleServerRpc();
                    }
                }
            } else {
                // Empty handed player picks up the food from fryer
                GetKitchenObject().SetKitchenObjectParent(player);
                SetStateIdleServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartFryingServerRpc() {
        fryingTimer.Value = 0f;
        burningTimer.Value = 0f;
        state.Value = State.Frying;
        OnAnyFryStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetStateIdleServerRpc() {
        state.Value = State.Idle;
        fryingTimer.Value = 0f;
        burningTimer.Value = 0f;
    }

    public bool IsFried() => state.Value == State.Fried;
    public bool IsFrying() => state.Value == State.Frying;
}

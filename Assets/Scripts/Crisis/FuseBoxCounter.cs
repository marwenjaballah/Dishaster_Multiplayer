using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Wall/Station counter representing the kitchen electrical breaker / fuse box.
/// Implements IHasProgress for standard progress bar display.
/// Outside of a blackout, the fuse box is inactive: no prompt is displayed and interactions are ignored.
/// During a blackout, approaching displays the 'E' prompt. Holding 'E' for 3 seconds fills the circular progress ring
/// and restores power across the kitchen.
/// </summary>
public class FuseBoxCounter : BaseCounter, IHasProgress {

    public static event EventHandler OnAnyFuseBoxSpawned;

    // ===== EVENTS =====
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnRepairClick;
    public event EventHandler OnFuseRepaired;
    public event EventHandler OnFuseTripped;

    [Header("Fuse Settings")]
    [SerializeField] private int requiredClicksToFix = 3;
    [SerializeField] private GameObject redLightVisual;
    [SerializeField] private GameObject greenLightVisual;
    [SerializeField] private ParticleSystem sparkParticleSystem;

    private NetworkVariable<int> currentClicks = new NetworkVariable<int>(0);

    [Header("Hold Settings")]
    [SerializeField] private float holdDuration = 3.0f;
    private InteractKeyPromptUI keyPromptUI;

    private void Awake() {
        keyPromptUI = GetComponentInChildren<InteractKeyPromptUI>(true);
        if (keyPromptUI != null) {
            keyPromptUI.ConfigureHold(true, holdDuration, "E");
            keyPromptUI.SetCanShowPredicate(IsFuseTripped);
            keyPromptUI.OnHoldCompleted += KeyPromptUI_OnHoldCompleted;
        }
    }

    private void Start() {
        if (keyPromptUI != null) {
            keyPromptUI.ConfigureHold(true, holdDuration, "E");
            keyPromptUI.SetCanShowPredicate(IsFuseTripped);
        }
    }

    private bool IsFuseTripped() {
        return KitchenCrisisManager.Instance != null && KitchenCrisisManager.Instance.IsBlackout();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        currentClicks.OnValueChanged += CurrentClicks_OnValueChanged;

        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnBlackoutStarted += CrisisManager_OnBlackoutStarted;
            KitchenCrisisManager.Instance.OnBlackoutEnded += CrisisManager_OnBlackoutEnded;
        }

        UpdateVisuals(KitchenCrisisManager.Instance != null && KitchenCrisisManager.Instance.IsBlackout());
        OnAnyFuseBoxSpawned?.Invoke(this, EventArgs.Empty);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        currentClicks.OnValueChanged -= CurrentClicks_OnValueChanged;

        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnBlackoutStarted -= CrisisManager_OnBlackoutStarted;
            KitchenCrisisManager.Instance.OnBlackoutEnded -= CrisisManager_OnBlackoutEnded;
        }
    }

    private void CrisisManager_OnBlackoutStarted(object sender, EventArgs e) {
        if (IsServer) {
            currentClicks.Value = 0;
        }
        UpdateVisuals(true);
        OnFuseTripped?.Invoke(this, EventArgs.Empty);
    }

    private void CrisisManager_OnBlackoutEnded(object sender, EventArgs e) {
        if (IsServer) {
            currentClicks.Value = 0;
        }
        UpdateVisuals(false);
        OnFuseRepaired?.Invoke(this, EventArgs.Empty);
    }

    private void CurrentClicks_OnValueChanged(int previousValue, int newValue) {
        float progress = (float)newValue / requiredClicksToFix;
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = progress
        });

        if (newValue > previousValue) {
            OnRepairClick?.Invoke(this, EventArgs.Empty);
            if (sparkParticleSystem != null) {
                sparkParticleSystem.Play();
            }
        }
    }

    private void KeyPromptUI_OnHoldCompleted() {
        if (!IsFuseTripped()) return;
        AdvanceRepairServerRpc();
    }

    public override void Interact(Player player) {
        if (!IsFuseTripped()) return;
        // Holding 'E' is the primary action handled by InteractKeyPromptUI
    }

    public override void InteractAlternate(Player player) {
        // 'F' key does not interact with the fuse box
    }

    [ServerRpc(RequireOwnership = false)]
    private void AdvanceRepairServerRpc() {
        if (!IsFuseTripped()) return;

        currentClicks.Value = requiredClicksToFix;

        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.EndBlackoutServerRpc();
        }

        if (sparkParticleSystem != null) {
            PlaySparksClientRpc();
        }

        currentClicks.Value = 0;
    }

    [ClientRpc]
    private void PlaySparksClientRpc() {
        if (sparkParticleSystem != null) {
            sparkParticleSystem.Play();
        }
    }

    private void UpdateVisuals(bool isTripped) {
        if (redLightVisual != null) redLightVisual.SetActive(isTripped);
        if (greenLightVisual != null) greenLightVisual.SetActive(!isTripped);
    }
}

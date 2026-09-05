using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Portable Fire Extinguisher that can be picked up with E,
/// carried in the player's hands, sprayed by HOLDING F (Interact Alternate),
/// and dropped/placed back on any counter with E.
/// Spraying takes 3 seconds of continuous foam to fully put out a stove fire.
/// Fully event-driven and synchronized over Unity Netcode.
/// </summary>
public class PortableFireExtinguisher : KitchenObject {

    public static event EventHandler OnAnyExtinguisherSprayed;

    // ===== EVENTS =====
    public event EventHandler OnSprayStarted;
    public event EventHandler OnSprayStopped;

    [Header("Extinguisher Spray Settings")]
    [SerializeField] private ParticleSystem foamParticleSystem;
    [SerializeField] private float sprayRange = 3.5f;

    private NetworkVariable<bool> isSpraying = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        isSpraying.OnValueChanged += IsSpraying_OnValueChanged;
        UpdateFoamVisuals(isSpraying.Value);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        isSpraying.OnValueChanged -= IsSpraying_OnValueChanged;
    }

    private void IsSpraying_OnValueChanged(bool previousValue, bool newValue) {
        UpdateFoamVisuals(newValue);

        if (newValue) {
            OnSprayStarted?.Invoke(this, EventArgs.Empty);
            OnAnyExtinguisherSprayed?.Invoke(this, EventArgs.Empty);
        } else {
            OnSprayStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateFoamVisuals(bool spraying) {
        if (foamParticleSystem != null) {
            if (spraying && !foamParticleSystem.isPlaying) {
                foamParticleSystem.Play();
            } else if (!spraying && foamParticleSystem.isPlaying) {
                foamParticleSystem.Stop();
            }
        }
    }

    /// <summary>
    /// Legacy trigger handler (hold F is now tracked in Update).
    /// </summary>
    public void Use(Player player) {
        // Holding F is tracked dynamically in Update()
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSprayingServerRpc(bool spraying) {
        isSpraying.Value = spraying;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopSprayingServerRpc() {
        if (isSpraying.Value) {
            isSpraying.Value = false;
        }
    }

    private void Update() {
        IKitchenObjectParent parent = GetKitchenObjectParent();
        if (parent == null || !(parent is Player player)) {
            if (IsServer && isSpraying.Value) {
                SetSprayingServerRpc(false);
            }
            return;
        }

        // Only the local player holding the extinguisher checks hold input
        if (player.IsOwner) {
            bool isHoldingF = false;
            if (GameInput.Instance != null) {
                isHoldingF = GameInput.Instance.IsInteractAlternatePressed();
            }
            if (!isHoldingF) {
                isHoldingF = Input.GetKey(KeyCode.F);
            }

            if (isHoldingF != isSpraying.Value) {
                SetSprayingServerRpc(isHoldingF);
            }
        }

        if (!isSpraying.Value) return;

        Vector3 sprayOrigin = transform.position;
        Vector3 sprayDir = player.transform.forward;

        // Extinguish burning hazards within spray cone and range
        RaycastHit[] hits = Physics.SphereCastAll(sprayOrigin, 1.2f, sprayDir, sprayRange);
        foreach (RaycastHit hit in hits) {
            FireHazard hazard = hit.collider.GetComponentInParent<FireHazard>();
            if (hazard != null && hazard.IsBurning()) {
                hazard.ApplyExtinguish(Time.deltaTime);
            }
        }

        FireHazard[] allHazards = FindObjectsByType<FireHazard>(FindObjectsSortMode.None);
        foreach (FireHazard hazard in allHazards) {
            if (hazard != null && hazard.IsBurning()) {
                Vector3 toHazard = hazard.transform.position - sprayOrigin;
                if (toHazard.magnitude <= sprayRange && Vector3.Angle(sprayDir, toHazard) < 60f) {
                    hazard.ApplyExtinguish(Time.deltaTime);
                }
            }
        }
    }
}

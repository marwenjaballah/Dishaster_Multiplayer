using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Observes KitchenCrisisManager events to dynamically control scene lighting,
/// emergency flashing beacon, warning sounds, and visual atmosphere.
/// </summary>
public class CrisisVisualEffects : MonoBehaviour {

    [Header("Lighting")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private Light emergencyRedLight;
    [SerializeField] private float normalLightIntensity = 1.0f;
    [SerializeField] private float blackoutLightIntensity = 0.08f;
    [SerializeField] private float transitionSpeed = 3.0f;

    [Header("Emergency Beacon Settings")]
    [SerializeField] private float beaconPulseSpeed = 4.0f;
    [SerializeField] private float minBeaconIntensity = 0.2f;
    [SerializeField] private float maxBeaconIntensity = 2.5f;

    private float targetLightIntensity;
    private bool isBlackoutActive;

    private void Start() {
        if (mainDirectionalLight == null) {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights) {
                if (l.type == LightType.Directional) {
                    mainDirectionalLight = l;
                    break;
                }
            }
        }

        if (mainDirectionalLight != null) {
            targetLightIntensity = normalLightIntensity;
        }

        // Setup emergency beacon light if not provided
        if (emergencyRedLight == null) {
            GameObject beaconGo = new GameObject("EmergencyRedBeaconLight");
            beaconGo.transform.SetParent(transform);
            beaconGo.transform.position = new Vector3(0, 4.5f, 0);

            emergencyRedLight = beaconGo.AddComponent<Light>();
            emergencyRedLight.type = LightType.Point;
            emergencyRedLight.color = Color.red;
            emergencyRedLight.range = 15f;
            emergencyRedLight.intensity = 0f;
            emergencyRedLight.enabled = false;
        }

        // Subscribe to crisis events
        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnBlackoutStarted += CrisisManager_OnBlackoutStarted;
            KitchenCrisisManager.Instance.OnBlackoutEnded += CrisisManager_OnBlackoutEnded;
        }
    }

    private void OnDestroy() {
        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnBlackoutStarted -= CrisisManager_OnBlackoutStarted;
            KitchenCrisisManager.Instance.OnBlackoutEnded -= CrisisManager_OnBlackoutEnded;
        }
    }

    private void CrisisManager_OnBlackoutStarted(object sender, EventArgs e) {
        isBlackoutActive = true;
        targetLightIntensity = blackoutLightIntensity;
        if (emergencyRedLight != null) {
            emergencyRedLight.enabled = true;
        }
    }

    private void CrisisManager_OnBlackoutEnded(object sender, EventArgs e) {
        isBlackoutActive = false;
        targetLightIntensity = normalLightIntensity;
        if (emergencyRedLight != null) {
            emergencyRedLight.intensity = 0f;
            emergencyRedLight.enabled = false;
        }
    }

    private void Update() {
        // Smoothly interpolate directional light
        if (mainDirectionalLight != null) {
            mainDirectionalLight.intensity = Mathf.MoveTowards(
                mainDirectionalLight.intensity,
                targetLightIntensity,
                transitionSpeed * Time.deltaTime
            );
        }

        // Pulse red beacon light during blackout
        if (isBlackoutActive && emergencyRedLight != null && emergencyRedLight.enabled) {
            float pulse = Mathf.PingPong(Time.time * beaconPulseSpeed, 1.0f);
            emergencyRedLight.intensity = Mathf.Lerp(minBeaconIntensity, maxBeaconIntensity, pulse);
        }
    }
}

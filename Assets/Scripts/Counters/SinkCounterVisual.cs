using System;
using UnityEngine;

/// <summary>
/// Provides visual feedback and water splash particles for SinkCounter scrubbing.
/// </summary>
public class SinkCounterVisual : MonoBehaviour {

    [SerializeField] private SinkCounter sinkCounter;
    [SerializeField] private GameObject waterSplashParticles;

    private void Start() {
        if (sinkCounter != null) {
            sinkCounter.OnWash += SinkCounter_OnWash;
            sinkCounter.OnWashComplete += SinkCounter_OnWashComplete;
        }
    }

    private void OnDestroy() {
        if (sinkCounter != null) {
            sinkCounter.OnWash -= SinkCounter_OnWash;
            sinkCounter.OnWashComplete -= SinkCounter_OnWashComplete;
        }
    }

    private void SinkCounter_OnWash(object sender, EventArgs e) {
        if (waterSplashParticles != null) {
            waterSplashParticles.SetActive(false);
            waterSplashParticles.SetActive(true);
        }
    }

    private void SinkCounter_OnWashComplete(object sender, EventArgs e) {
        if (waterSplashParticles != null) {
            waterSplashParticles.SetActive(false);
        }
    }
}

using System;
using UnityEngine;

/// <summary>
/// Visual controller for the Deep Fryer station: toggles oil bubbling particles and frying basket.
/// </summary>
public class DeepFryerCounterVisual : MonoBehaviour {

    [SerializeField] private DeepFryerCounter deepFryerCounter;
    [SerializeField] private GameObject oilBubblesGameObject;
    [SerializeField] private GameObject fryingSizzleParticles;

    private void Start() {
        if (deepFryerCounter != null) {
            deepFryerCounter.OnStateChanged += DeepFryerCounter_OnStateChanged;
        }
    }

    private void OnDestroy() {
        if (deepFryerCounter != null) {
            deepFryerCounter.OnStateChanged -= DeepFryerCounter_OnStateChanged;
        }
    }

    private void DeepFryerCounter_OnStateChanged(object sender, DeepFryerCounter.OnStateChangedEventArgs e) {
        bool isCooking = e.state == DeepFryerCounter.State.Frying || e.state == DeepFryerCounter.State.Fried;
        if (oilBubblesGameObject != null) {
            oilBubblesGameObject.SetActive(isCooking);
        }
        if (fryingSizzleParticles != null) {
            fryingSizzleParticles.SetActive(isCooking);
        }
    }
}

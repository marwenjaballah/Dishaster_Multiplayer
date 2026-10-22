using System;
using UnityEngine;

/// <summary>
/// Visual feedback for the Beverage Dispenser: toggles pouring stream visual and splash effects.
/// </summary>
public class BeverageDispenserVisual : MonoBehaviour {

    [SerializeField] private BeverageDispenserCounter dispenserCounter;
    [SerializeField] private GameObject pouringStreamGameObject;

    private void Start() {
        if (dispenserCounter != null) {
            dispenserCounter.OnPourStarted += DispenserCounter_OnPourStarted;
            dispenserCounter.OnPourCompleted += DispenserCounter_OnPourCompleted;
        }
    }

    private void OnDestroy() {
        if (dispenserCounter != null) {
            dispenserCounter.OnPourStarted -= DispenserCounter_OnPourStarted;
            dispenserCounter.OnPourCompleted -= DispenserCounter_OnPourCompleted;
        }
    }

    private void DispenserCounter_OnPourStarted(object sender, EventArgs e) {
        if (pouringStreamGameObject != null) {
            pouringStreamGameObject.SetActive(true);
        }
    }

    private void DispenserCounter_OnPourCompleted(object sender, EventArgs e) {
        if (pouringStreamGameObject != null) {
            pouringStreamGameObject.SetActive(false);
        }
    }
}

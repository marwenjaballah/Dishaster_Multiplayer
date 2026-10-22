using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedicated world-space UI for the Beverage Dispenser station.
/// Features cold soda theme, vibrant violet-to-cyan pouring gauge, and full cup feedback.
/// </summary>
public class BeverageDispenserCounterUI : MonoBehaviour {

    [Header("References")]
    [SerializeField] private BeverageDispenserCounter dispenserCounter;
    [SerializeField] private GameObject uiContainer;
    [SerializeField] private Image fillProgressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private Image statusIconImage;

    [Header("Color Palette")]
    [SerializeField] private Color pouringColor = new Color(0.55f, 0.33f, 0.96f); // Soda Purple #8B5CF6
    [SerializeField] private Color completeColor = new Color(0.13f, 0.77f, 0.37f); // Full Emerald #22C55E

    private Camera mainCamera;
    private Coroutine completeCoroutine;

    private void Awake() {
        if (dispenserCounter == null) {
            dispenserCounter = GetComponentInParent<BeverageDispenserCounter>();
        }
        mainCamera = Camera.main;
        Hide();
    }

    private void Start() {
        if (dispenserCounter != null) {
            dispenserCounter.OnProgressChanged += DispenserCounter_OnProgressChanged;
            dispenserCounter.OnPourStarted += DispenserCounter_OnPourStarted;
            dispenserCounter.OnPourCompleted += DispenserCounter_OnPourCompleted;
        }
        Hide();
    }

    private void LateUpdate() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
        }
        if (mainCamera != null && uiContainer != null && uiContainer.activeSelf) {
            transform.LookAt(transform.position + mainCamera.transform.forward);
        }
    }

    private void OnDestroy() {
        if (dispenserCounter != null) {
            dispenserCounter.OnProgressChanged -= DispenserCounter_OnProgressChanged;
            dispenserCounter.OnPourStarted -= DispenserCounter_OnPourStarted;
            dispenserCounter.OnPourCompleted -= DispenserCounter_OnPourCompleted;
        }
    }

    private void DispenserCounter_OnPourStarted(object sender, EventArgs e) {
        Show();
        if (statusText != null) {
            statusText.text = "DISPENSING SODA...";
            statusText.color = pouringColor;
        }
        if (fillProgressBar != null) {
            fillProgressBar.color = pouringColor;
            fillProgressBar.fillAmount = 0f;
        }
        if (progressPercentText != null) {
            progressPercentText.text = "0%";
        }
    }

    private void DispenserCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e) {
        if (e.progressNormalized > 0f) {
            Show();
            if (fillProgressBar != null) {
                fillProgressBar.fillAmount = e.progressNormalized;
                fillProgressBar.color = pouringColor;
            }
            if (progressPercentText != null) {
                progressPercentText.text = $"{Mathf.RoundToInt(e.progressNormalized * 100f)}%";
            }
        }
    }

    private void DispenserCounter_OnPourCompleted(object sender, EventArgs e) {
        if (completeCoroutine != null) StopCoroutine(completeCoroutine);
        completeCoroutine = StartCoroutine(ShowCupFullRoutine());
    }

    private System.Collections.IEnumerator ShowCupFullRoutine() {
        Show();
        if (fillProgressBar != null) {
            fillProgressBar.fillAmount = 1f;
            fillProgressBar.color = completeColor;
        }
        if (statusText != null) {
            statusText.text = "CUP FULL!";
            statusText.color = completeColor;
        }
        if (progressPercentText != null) {
            progressPercentText.text = "READY";
        }

        yield return new WaitForSeconds(1.0f);
        Hide();
    }

    private void Show() {
        if (uiContainer != null) uiContainer.SetActive(true);
    }

    private void Hide() {
        if (uiContainer != null) uiContainer.SetActive(false);
    }
}

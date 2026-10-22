using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedicated world-space UI for the Deep Fryer station.
/// Features sizzling oil theme, golden-amber frying progress, crispy ready status, and burning oil alerts.
/// </summary>
public class DeepFryerCounterUI : MonoBehaviour {

    [Header("References")]
    [SerializeField] private DeepFryerCounter deepFryerCounter;
    [SerializeField] private GameObject uiContainer;
    [SerializeField] private Image fillProgressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private Image statusIconImage;

    [Header("Color Palette")]
    [SerializeField] private Color fryingColor = new Color(0.96f, 0.62f, 0.04f); // Golden Amber #F59E0B
    [SerializeField] private Color readyColor = new Color(0.13f, 0.77f, 0.37f);  // Crispy Green #22C55E
    [SerializeField] private Color burningColor = new Color(0.94f, 0.27f, 0.27f); // Danger Red #EF4444

    private Camera mainCamera;
    private DeepFryerCounter.State currentState = DeepFryerCounter.State.Idle;

    private void Awake() {
        if (deepFryerCounter == null) {
            deepFryerCounter = GetComponentInParent<DeepFryerCounter>();
        }
        mainCamera = Camera.main;
        Hide();
    }

    private void Start() {
        if (deepFryerCounter != null) {
            deepFryerCounter.OnProgressChanged += DeepFryerCounter_OnProgressChanged;
            deepFryerCounter.OnStateChanged += DeepFryerCounter_OnStateChanged;
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

        // Pulse effect when burning
        if (currentState == DeepFryerCounter.State.Burned && uiContainer != null && uiContainer.activeSelf) {
            float pulse = 1f + Mathf.PingPong(Time.time * 4f, 0.15f);
            uiContainer.transform.localScale = Vector3.one * pulse;
        }
    }

    private void OnDestroy() {
        if (deepFryerCounter != null) {
            deepFryerCounter.OnProgressChanged -= DeepFryerCounter_OnProgressChanged;
            deepFryerCounter.OnStateChanged -= DeepFryerCounter_OnStateChanged;
        }
    }

    private void DeepFryerCounter_OnStateChanged(object sender, DeepFryerCounter.OnStateChangedEventArgs e) {
        currentState = e.state;

        switch (e.state) {
            case DeepFryerCounter.State.Idle:
                Hide();
                if (uiContainer != null) uiContainer.transform.localScale = Vector3.one;
                break;

            case DeepFryerCounter.State.Frying:
                Show();
                if (statusText != null) {
                    statusText.text = "DEEP FRYING...";
                    statusText.color = fryingColor;
                }
                if (fillProgressBar != null) fillProgressBar.color = fryingColor;
                break;

            case DeepFryerCounter.State.Fried:
                Show();
                if (statusText != null) {
                    statusText.text = "CRISPY & READY!";
                    statusText.color = readyColor;
                }
                if (fillProgressBar != null) {
                    fillProgressBar.color = readyColor;
                    fillProgressBar.fillAmount = 1f;
                }
                if (progressPercentText != null) progressPercentText.text = "DONE";
                break;

            case DeepFryerCounter.State.Burned:
                Show();
                if (statusText != null) {
                    statusText.text = "BURNING OIL!";
                    statusText.color = burningColor;
                }
                if (fillProgressBar != null) {
                    fillProgressBar.color = burningColor;
                    fillProgressBar.fillAmount = 1f;
                }
                if (progressPercentText != null) progressPercentText.text = "BURNING";
                break;
        }
    }

    private void DeepFryerCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e) {
        if (currentState == DeepFryerCounter.State.Frying) {
            if (e.progressNormalized > 0f) {
                Show();
                if (fillProgressBar != null) fillProgressBar.fillAmount = e.progressNormalized;
                if (progressPercentText != null) {
                    progressPercentText.text = $"{Mathf.RoundToInt(e.progressNormalized * 100f)}%";
                }
            }
        }
    }

    private void Show() {
        if (uiContainer != null) uiContainer.SetActive(true);
    }

    private void Hide() {
        if (uiContainer != null) uiContainer.SetActive(false);
    }
}

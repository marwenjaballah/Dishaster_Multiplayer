using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedicated world-space UI for the Sink Counter dishwashing station.
/// Displays animated scrubbing progress, aqua bubble fill theme, and clean sparkle feedback.
/// </summary>
public class SinkCounterUI : MonoBehaviour {

    [Header("References")]
    [SerializeField] private SinkCounter sinkCounter;
    [SerializeField] private GameObject uiContainer;
    [SerializeField] private Image fillProgressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private Image statusIconImage;

    [Header("Visual Theme")]
    [SerializeField] private Color washColor = new Color(0.02f, 0.71f, 0.83f); // Aqua Cyan #06B6D4
    [SerializeField] private Color completeColor = new Color(0.13f, 0.77f, 0.37f); // Emerald #22C55E

    private Camera mainCamera;
    private Coroutine flashCoroutine;

    private void Awake() {
        if (sinkCounter == null) {
            sinkCounter = GetComponentInParent<SinkCounter>();
        }
        mainCamera = Camera.main;
        Hide();
    }

    private void Start() {
        if (sinkCounter != null) {
            sinkCounter.OnProgressChanged += SinkCounter_OnProgressChanged;
            sinkCounter.OnWash += SinkCounter_OnWash;
            sinkCounter.OnWashComplete += SinkCounter_OnWashComplete;
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
        if (sinkCounter != null) {
            sinkCounter.OnProgressChanged -= SinkCounter_OnProgressChanged;
            sinkCounter.OnWash -= SinkCounter_OnWash;
            sinkCounter.OnWashComplete -= SinkCounter_OnWashComplete;
        }
    }

    private void SinkCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e) {
        if (e.progressNormalized > 0f && e.progressNormalized < 1f) {
            Show();
            if (fillProgressBar != null) {
                fillProgressBar.fillAmount = e.progressNormalized;
                fillProgressBar.color = washColor;
            }
            if (statusText != null) {
                statusText.text = "SCRUBBING DISHES";
                statusText.color = washColor;
            }
            if (progressPercentText != null) {
                progressPercentText.text = $"{Mathf.RoundToInt(e.progressNormalized * 100f)}%";
            }
        } else if (e.progressNormalized <= 0f) {
            Hide();
        }
    }

    private void SinkCounter_OnWash(object sender, EventArgs e) {
        Show();
        // Gentle micro-bounce when scrubbed
        if (uiContainer != null) {
            uiContainer.transform.localScale = Vector3.one * 1.08f;
        }
    }

    private void SinkCounter_OnWashComplete(object sender, EventArgs e) {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(ShowCleanCompleteRoutine());
    }

    private System.Collections.IEnumerator ShowCleanCompleteRoutine() {
        Show();
        if (fillProgressBar != null) {
            fillProgressBar.fillAmount = 1f;
            fillProgressBar.color = completeColor;
        }
        if (statusText != null) {
            statusText.text = "SPARKLING CLEAN!";
            statusText.color = completeColor;
        }
        if (progressPercentText != null) {
            progressPercentText.text = "100%";
        }

        yield return new WaitForSeconds(1.2f);
        Hide();
    }

    private void Update() {
        // Return scale smoothly to normal
        if (uiContainer != null && uiContainer.transform.localScale.x > 1f) {
            uiContainer.transform.localScale = Vector3.MoveTowards(uiContainer.transform.localScale, Vector3.one, Time.deltaTime * 3f);
        }
    }

    private void Show() {
        if (uiContainer != null) uiContainer.SetActive(true);
    }

    private void Hide() {
        if (uiContainer != null) uiContainer.SetActive(false);
    }
}

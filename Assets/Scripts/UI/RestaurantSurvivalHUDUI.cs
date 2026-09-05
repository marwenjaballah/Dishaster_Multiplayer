using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top HUD for Restaurant Survival Mode:
/// Displays Cash Balance, Dynamic Star Rating, Next Rent Countdown, and Bankruptcy Overlay.
/// </summary>
public class RestaurantSurvivalHUDUI : MonoBehaviour {

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI cashBalanceText;
    [SerializeField] private TextMeshProUGUI starRatingText;
    [SerializeField] private TextMeshProUGUI nextBillText;
    [SerializeField] private TextMeshProUGUI billWarningText;

    [Header("Bankruptcy Overlay")]
    [SerializeField] private GameObject bankruptcyPanel;
    [SerializeField] private TextMeshProUGUI bankruptcyDetailsText;

    [Header("Visual Colors")]
    [SerializeField] private Color normalCashColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] private Color negativeCashColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color warningBillColor = new Color(1f, 0.4f, 0.1f);

    private float pulseTimer = 0f;
    private bool isSubscribed = false;

    private void Start() {
        TrySubscribeToEconomyManager();

        if (bankruptcyPanel != null) {
            bankruptcyPanel.SetActive(false);
        }

        if (billWarningText != null) {
            billWarningText.gameObject.SetActive(false);
        }

        UpdateVisuals();
    }

    private void TrySubscribeToEconomyManager() {
        if (!isSubscribed && RestaurantEconomyManager.Instance != null) {
            RestaurantEconomyManager.Instance.OnBalanceChanged += Instance_OnBalanceChanged;
            RestaurantEconomyManager.Instance.OnRatingChanged += Instance_OnRatingChanged;
            RestaurantEconomyManager.Instance.OnBillPaid += Instance_OnBillPaid;
            RestaurantEconomyManager.Instance.OnBankruptcy += Instance_OnBankruptcy;
            isSubscribed = true;
            UpdateVisuals();
        }
    }

    private void Update() {
        if (!isSubscribed) {
            TrySubscribeToEconomyManager();
        }

        if (RestaurantEconomyManager.Instance == null) return;

        UpdateTimerDisplay();

        // Warning pulse when bill is close (< 15s)
        float billTimer = RestaurantEconomyManager.Instance.GetNextBillTimer();
        if (billTimer <= 15f && billTimer > 0f) {
            pulseTimer += Time.deltaTime * 5f;
            float alpha = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;

            if (nextBillText != null) {
                nextBillText.color = Color.Lerp(Color.white, warningBillColor, alpha);
            }
            if (billWarningText != null) {
                billWarningText.gameObject.SetActive(true);
                billWarningText.text = $"⚠️ RENT DUE IN {Mathf.CeilToInt(billTimer)}s!";
            }
        } else {
            if (nextBillText != null) {
                nextBillText.color = Color.white;
            }
            if (billWarningText != null) {
                billWarningText.gameObject.SetActive(false);
            }
        }
    }

    private void Instance_OnBalanceChanged(object sender, RestaurantEconomyManager.OnBalanceChangedEventArgs e) {
        UpdateVisuals();
    }

    private void Instance_OnRatingChanged(object sender, RestaurantEconomyManager.OnRatingChangedEventArgs e) {
        UpdateVisuals();
    }

    private void Instance_OnBillPaid(object sender, RestaurantEconomyManager.OnBillPaidEventArgs e) {
        UpdateVisuals();
    }

    private void Instance_OnBankruptcy(object sender, EventArgs e) {
        if (bankruptcyPanel != null) {
            bankruptcyPanel.SetActive(true);
            if (bankruptcyDetailsText != null) {
                int finalReviews = RestaurantEconomyManager.Instance.GetTotalReviews();
                float finalRating = RestaurantEconomyManager.Instance.GetStarRating();
                int debt = Mathf.Abs(RestaurantEconomyManager.Instance.GetCurrentBalance());
                bankruptcyDetailsText.text = $"You couldn't afford the rent!\nDebt: -${debt}\nFinal Reputation: {finalRating:F1}★ ({finalReviews} reviews)";
            }
        }
    }

    private void UpdateVisuals() {
        if (RestaurantEconomyManager.Instance == null) return;

        // Cash Balance
        int balance = RestaurantEconomyManager.Instance.GetCurrentBalance();
        if (cashBalanceText != null) {
            cashBalanceText.text = $"${balance}";
            cashBalanceText.color = balance >= 0 ? normalCashColor : negativeCashColor;
        }

        // Star Rating
        float rating = RestaurantEconomyManager.Instance.GetStarRating();
        int totalReviews = RestaurantEconomyManager.Instance.GetTotalReviews();
        if (starRatingText != null) {
            starRatingText.text = $"Rating: {rating:F1} / 5.0 <size=65%>({totalReviews} reviews)</size>";
            // Color based on rating tier
            if (rating >= 4.0f) {
                starRatingText.color = new Color(1.0f, 0.85f, 0.2f); // Gold
            } else if (rating >= 2.5f) {
                starRatingText.color = new Color(0.9f, 0.9f, 0.9f); // Silver/White
            } else {
                starRatingText.color = new Color(1.0f, 0.4f, 0.3f); // Red/Orange warning
            }
        }

        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay() {
        if (RestaurantEconomyManager.Instance == null) return;

        float timer = Mathf.Max(0f, RestaurantEconomyManager.Instance.GetNextBillTimer());
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        int billAmount = RestaurantEconomyManager.Instance.GetCurrentBillAmount();
        int billCycle = RestaurantEconomyManager.Instance.GetCurrentBillCycle();

        if (nextBillText != null) {
            nextBillText.text = $"Rent #{billCycle} (${billAmount}): {minutes:00}:{seconds:00}";
        }
    }

    private void OnDestroy() {
        if (RestaurantEconomyManager.Instance != null) {
            RestaurantEconomyManager.Instance.OnBalanceChanged -= Instance_OnBalanceChanged;
            RestaurantEconomyManager.Instance.OnRatingChanged -= Instance_OnRatingChanged;
            RestaurantEconomyManager.Instance.OnBillPaid -= Instance_OnBillPaid;
            RestaurantEconomyManager.Instance.OnBankruptcy -= Instance_OnBankruptcy;
        }
    }
}

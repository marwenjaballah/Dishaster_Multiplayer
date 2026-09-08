using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top HUD for Restaurant Survival Mode:
/// Displays Cash Balance, Dynamic Star Rating, Next Rent Countdown, Bankruptcy Overlay,
/// and Real-time Active Crisis Banner & Emergency Instructions.
/// </summary>
public class RestaurantSurvivalHUDUI : MonoBehaviour {

    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI cashBalanceText;
    [SerializeField] private TextMeshProUGUI starRatingText;
    [SerializeField] private TextMeshProUGUI nextBillText;
    [SerializeField] private TextMeshProUGUI billWarningText;

    [Header("Active Crisis Alert UI")]
    [SerializeField] private GameObject crisisAlertPanel;
    [SerializeField] private CanvasGroup crisisCanvasGroup;
    [SerializeField] private Image crisisAlertBackground;
    [SerializeField] private Image crisisAlertAccentBar;
    [SerializeField] private TextMeshProUGUI crisisTitleText;
    [SerializeField] private TextMeshProUGUI crisisInstructionText;
    [SerializeField] private TextMeshProUGUI crisisTimerText;

    [Header("Bankruptcy Overlay")]
    [SerializeField] private GameObject bankruptcyPanel;
    [SerializeField] private TextMeshProUGUI bankruptcyDetailsText;

    [Header("Visual Colors")]
    [SerializeField] private Color normalCashColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField] private Color negativeCashColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color warningBillColor = new Color(1f, 0.4f, 0.1f);

    [Header("Crisis Colors")]
    [SerializeField] private Color blackoutColor = new Color(0.25f, 0.75f, 1f);       // Neon Electric Blue
    [SerializeField] private Color fireColor = new Color(1f, 0.35f, 0.15f);           // Intense Flame Red-Orange
    [SerializeField] private Color waterLeakColor = new Color(0.15f, 0.85f, 0.95f);   // Cyan Water
    [SerializeField] private Color multipleCrisisColor = new Color(1f, 0.15f, 0.35f); // Neon Crimson Alert

    private float pulseTimer = 0f;
    private float crisisPulseTimer = 0f;
    private bool isSubscribed = false;
    private bool isCrisisSubscribed = false;

    private void Start() {
        TrySubscribeToManagers();

        if (bankruptcyPanel != null) {
            bankruptcyPanel.SetActive(false);
        }

        if (billWarningText != null) {
            billWarningText.gameObject.SetActive(false);
        }

        if (crisisAlertPanel != null) {
            crisisAlertPanel.SetActive(false);
        }

        UpdateVisuals();
        UpdateCrisisVisuals();
    }

    private void TrySubscribeToManagers() {
        if (!isSubscribed && RestaurantEconomyManager.Instance != null) {
            RestaurantEconomyManager.Instance.OnBalanceChanged += Instance_OnBalanceChanged;
            RestaurantEconomyManager.Instance.OnRatingChanged += Instance_OnRatingChanged;
            RestaurantEconomyManager.Instance.OnBillPaid += Instance_OnBillPaid;
            RestaurantEconomyManager.Instance.OnBankruptcy += Instance_OnBankruptcy;
            isSubscribed = true;
            UpdateVisuals();
        }

        if (!isCrisisSubscribed && KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnBlackoutStarted += CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnBlackoutEnded += CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnWaterLeakStarted += CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnWaterLeakEnded += CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnFireSpawned += CrisisManager_OnFireSpawned;
            KitchenCrisisManager.Instance.OnFireExtinguished += CrisisManager_OnFireExtinguished;
            KitchenCrisisManager.Instance.OnCrisisStateChanged += CrisisManager_OnCrisisEvent;
            isCrisisSubscribed = true;
            UpdateCrisisVisuals();
        }
    }

    private void Update() {
        if (!isSubscribed || !isCrisisSubscribed) {
            TrySubscribeToManagers();
        }

        UpdateTimerDisplay();
        UpdateCrisisTimerAndPulse();

        // Warning pulse when bill is close (< 15s)
        if (RestaurantEconomyManager.Instance != null) {
            float billTimer = RestaurantEconomyManager.Instance.GetNextBillTimer();
            if (billTimer <= 15f && billTimer > 0f) {
                pulseTimer += Time.deltaTime * 5f;
                float alpha = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;

                if (nextBillText != null) {
                    nextBillText.color = Color.Lerp(Color.white, warningBillColor, alpha);
                }
                if (billWarningText != null) {
                    billWarningText.gameObject.SetActive(true);
                    billWarningText.text = $"RENT DUE IN {Mathf.CeilToInt(billTimer)}s!";
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
    }

    private void CrisisManager_OnCrisisEvent(object sender, EventArgs e) {
        UpdateCrisisVisuals();
    }

    private void CrisisManager_OnFireSpawned(object sender, KitchenCrisisManager.OnFireSpawnedEventArgs e) {
        UpdateCrisisVisuals();
    }

    private void CrisisManager_OnFireExtinguished(object sender, KitchenCrisisManager.OnFireExtinguishedEventArgs e) {
        UpdateCrisisVisuals();
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
            bankruptcyPanel.SetActive(false);
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
            starRatingText.text = $"RATING: {rating:F1} / 5.0 <size=70%>({totalReviews} REVIEWS)</size>";
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
            nextBillText.text = $"RENT #{billCycle} (${billAmount}): {minutes:00}:{seconds:00}";
        }
    }

    private void UpdateCrisisVisuals() {
        if (crisisAlertPanel == null) return;

        if (KitchenCrisisManager.Instance == null || !KitchenCrisisManager.Instance.HasActiveCrisis) {
            crisisAlertPanel.SetActive(false);
            return;
        }

        crisisAlertPanel.SetActive(true);

        bool isBlackout = KitchenCrisisManager.Instance.IsBlackout();
        bool isWaterLeak = KitchenCrisisManager.Instance.IsWaterLeakActive();
        int fireCount = KitchenCrisisManager.Instance.ActiveFireCount;

        int activeCrisisCount = (isBlackout ? 1 : 0) + (isWaterLeak ? 1 : 0) + (fireCount > 0 ? 1 : 0);

        Color accentColor;
        string title;
        string instruction;

        if (activeCrisisCount > 1) {
            accentColor = multipleCrisisColor;
            title = "CRISIS ALERT: MULTIPLE HAZARDS";
            var details = new System.Collections.Generic.List<string>();
            if (isBlackout) details.Add("Blackout");
            if (isWaterLeak) details.Add("Water Leak");
            if (fireCount > 0) details.Add($"{fireCount} Fire{(fireCount > 1 ? "s" : "")}");
            instruction = string.Join(" | ", details);
        } else if (isBlackout) {
            accentColor = blackoutColor;
            title = "ELECTRICAL BLACKOUT";
            instruction = "Interact with Electrical Breaker Panel to restore lights!";
        } else if (isWaterLeak) {
            accentColor = waterLeakColor;
            title = "PIPE BURST / FLOODING";
            instruction = "Turn off Main Water Valve Counter to stop leak!";
        } else if (fireCount > 0) {
            accentColor = fireColor;
            title = fireCount > 1 ? $"KITCHEN FIRES ({fireCount})" : "KITCHEN FIRE";
            instruction = "Grab Fire Extinguisher and extinguish burning counter!";
        } else {
            crisisAlertPanel.SetActive(false);
            return;
        }

        if (crisisAlertAccentBar != null) crisisAlertAccentBar.color = accentColor;
        if (crisisTitleText != null) {
            crisisTitleText.text = title;
            crisisTitleText.color = accentColor;
        }
        if (crisisInstructionText != null) {
            crisisInstructionText.text = instruction;
        }

        UpdateCrisisTimerAndPulse();
    }

    private void UpdateCrisisTimerAndPulse() {
        if (crisisAlertPanel == null || !crisisAlertPanel.activeSelf) return;

        crisisPulseTimer += Time.deltaTime * 4f;
        float pulse = (Mathf.Sin(crisisPulseTimer) + 1f) * 0.5f;

        if (crisisCanvasGroup != null) {
            crisisCanvasGroup.alpha = Mathf.Lerp(0.85f, 1f, pulse);
        }

        if (crisisTimerText != null && RestaurantEconomyManager.Instance != null && KitchenCrisisManager.Instance != null && KitchenCrisisManager.Instance.HasActiveCrisis) {
            float duration = RestaurantEconomyManager.Instance.GetCrisisDurationTimer();
            float grace = RestaurantEconomyManager.Instance.GetCrisisGracePeriod();

            if (duration < grace) {
                float remainingGrace = Mathf.Max(0f, grace - duration);
                crisisTimerText.text = $"GRACE: {Mathf.CeilToInt(remainingGrace)}s";
                crisisTimerText.color = Color.Lerp(new Color(1f, 0.85f, 0.2f), Color.white, pulse);
            } else {
                float penaltyTimer = RestaurantEconomyManager.Instance.GetCrisisPenaltyTimer();
                float penaltyInterval = RestaurantEconomyManager.Instance.GetCrisisPenaltyInterval();
                float remainingPenalty = Mathf.Max(0f, penaltyInterval - penaltyTimer);
                crisisTimerText.text = $"PENALTY IN {Mathf.CeilToInt(remainingPenalty)}s!";
                crisisTimerText.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(1f, 0.6f, 0.2f), pulse);
            }
        }
    }

    private void OnDestroy() {
        if (RestaurantEconomyManager.Instance != null) {
            RestaurantEconomyManager.Instance.OnBalanceChanged -= Instance_OnBalanceChanged;
            RestaurantEconomyManager.Instance.OnRatingChanged -= Instance_OnRatingChanged;
            RestaurantEconomyManager.Instance.OnBillPaid -= Instance_OnBillPaid;
            RestaurantEconomyManager.Instance.OnBankruptcy -= Instance_OnBankruptcy;
        }

        if (KitchenCrisisManager.Instance != null) {
            KitchenCrisisManager.Instance.OnBlackoutStarted -= CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnBlackoutEnded -= CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnWaterLeakStarted -= CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnWaterLeakEnded -= CrisisManager_OnCrisisEvent;
            KitchenCrisisManager.Instance.OnFireSpawned -= CrisisManager_OnFireSpawned;
            KitchenCrisisManager.Instance.OnFireExtinguished -= CrisisManager_OnFireExtinguished;
            KitchenCrisisManager.Instance.OnCrisisStateChanged -= CrisisManager_OnCrisisEvent;
        }
    }
}

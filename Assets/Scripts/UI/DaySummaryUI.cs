using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Displays the end-of-day financial statement, customer rating, and 1-3 star achievement.
    /// Provides the transition button to enter the Night Market upgrade shop.
    /// </summary>
    public class DaySummaryUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI dayTitleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Financial Breakdown Texts")]
        [SerializeField] private TextMeshProUGUI grossIncomeText;
        [SerializeField] private TextMeshProUGUI tipsText;
        [SerializeField] private TextMeshProUGUI rentText;
        [SerializeField] private TextMeshProUGUI finesText;
        [SerializeField] private TextMeshProUGUI netProfitText;

        [Header("Performance & Reviews")]
        [SerializeField] private TextMeshProUGUI dishesDeliveredText;
        [SerializeField] private TextMeshProUGUI starsAwardedText;
        [SerializeField] private TextMeshProUGUI ratingText;
        [SerializeField] private TextMeshProUGUI customerReviewsText;

        [Header("Debt Warning Alert")]
        [SerializeField] private GameObject debtWarningPanel;
        [SerializeField] private TextMeshProUGUI debtWarningText;

        [Header("Action Buttons")]
        [SerializeField] private Button proceedToNightMarketButton;

        [Header("Visual Colors")]
        [SerializeField] private Color positiveProfitColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color negativeProfitColor = new Color(0.95f, 0.2f, 0.2f);
        [SerializeField] private Color expenseColor = new Color(0.95f, 0.4f, 0.3f);

        private void Awake()
        {
            if (proceedToNightMarketButton != null)
            {
                proceedToNightMarketButton.onClick.AddListener(OnProceedClicked);
            }
        }

        private void Start()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.OnDaySummaryCalculated += DayLoopManager_OnDaySummaryCalculated;
                DayLoopManager.Instance.OnDayPhaseChanged += DayLoopManager_OnDayPhaseChanged;
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.OnDaySummaryCalculated -= DayLoopManager_OnDaySummaryCalculated;
                DayLoopManager.Instance.OnDayPhaseChanged -= DayLoopManager_OnDayPhaseChanged;
            }
        }

        private void DayLoopManager_OnDayPhaseChanged(object sender, DayLoopManager.OnDayPhaseChangedEventArgs e)
        {
            if (e.newPhase != DayLoopManager.DayPhase.DaySummary)
            {
                Hide();
            }
        }

        private void DayLoopManager_OnDaySummaryCalculated(object sender, DayLoopManager.OnDaySummaryCalculatedEventArgs e)
        {
            DisplaySummary(e);
            Show();
        }

        private void DisplaySummary(DayLoopManager.OnDaySummaryCalculatedEventArgs data)
        {
            if (dayTitleText != null)
            {
                dayTitleText.text = $"DAY {data.dayNumber} COMPLETED!";
            }

            if (grossIncomeText != null) grossIncomeText.text = $"+${data.grossIncome}";
            if (tipsText != null) tipsText.text = $"+${data.tips}";
            if (rentText != null)
            {
                rentText.text = $"-${data.rent}";
                rentText.color = expenseColor;
            }
            if (finesText != null)
            {
                finesText.text = $"-${data.fines}";
                finesText.color = expenseColor;
            }

            if (netProfitText != null)
            {
                string sign = data.netProfit >= 0 ? "+$" : "-$";
                netProfitText.text = $"{sign}{Mathf.Abs(data.netProfit)}";
                netProfitText.color = data.netProfit >= 0 ? positiveProfitColor : negativeProfitColor;
            }

            if (dishesDeliveredText != null)
            {
                dishesDeliveredText.text = $"{data.dishesDelivered} Meals Served";
            }

            if (ratingText != null)
            {
                ratingText.text = $"{data.starRating:F1} ★";
            }

            if (starsAwardedText != null)
            {
                starsAwardedText.text = data.starsAwarded switch
                {
                    3 => "★★★ PERFECTION!",
                    2 => "★★☆ GREAT SHIFT",
                    _ => "★☆☆ SURVIVED"
                };
            }

            // Generate Yelp Customer Reviews
            var reviews = CustomerReviewManager.GenerateDailyReviews(
                data.dishesDelivered, 
                data.starRating, 
                data.tips, 
                data.fines, 
                data.isDebtStrike
            );

            if (customerReviewsText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<b>CUSTOMER REVIEWS:</b>");
                foreach (var rev in reviews)
                {
                    string starsStr = new string('★', rev.stars) + new string('☆', 5 - rev.stars);
                    sb.AppendLine($"<color=#F59E0B>{starsStr}</color> <b>{rev.authorHandle}:</b> <i>\"{rev.comment}\"</i>");
                }
                customerReviewsText.text = sb.ToString();
            }
            else if (subtitleText != null && reviews.Count > 0)
            {
                var primaryRev = reviews[0];
                string starsStr = new string('★', primaryRev.stars);
                subtitleText.text = $"\"{primaryRev.comment}\" — {primaryRev.authorHandle} ({starsStr})";
            }

            // Debt Strike Banner
            if (debtWarningPanel != null)
            {
                debtWarningPanel.SetActive(data.isDebtStrike && !data.isBankrupt);
                if (debtWarningText != null && data.isDebtStrike)
                {
                    debtWarningText.text = $"⚠️ DEBT WARNING: Strike {data.currentDebtStrikes}/2! Two consecutive negative days trigger Bankruptcy.";
                }
            }
        }

        private void OnProceedClicked()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.RequestProceedToNightMarketRpc();
            }
        }

        public void Show()
        {
            if (summaryPanel != null) summaryPanel.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            if (summaryPanel != null) summaryPanel.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}

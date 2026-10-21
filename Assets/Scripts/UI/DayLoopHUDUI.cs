using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Displays the current Day, Shift Title, and Shift Timer progress on the player's HUD.
    /// Automatically updates in real-time as DayLoopManager cycles through phases.
    /// </summary>
    public class DayLoopHUDUI : MonoBehaviour
    {
        [Header("UI Text References")]
        [SerializeField] private TextMeshProUGUI dayNumberText;
        [SerializeField] private TextMeshProUGUI shiftTitleText;
        [SerializeField] private TextMeshProUGUI shiftTimerText;

        [Header("Progress & Visuals")]
        [SerializeField] private Slider shiftProgressSlider;
        [SerializeField] private Image shiftAccentBar;

        [Header("Shift Accent Colors")]
        [SerializeField] private Color morningColor = new Color(0.2f, 0.85f, 1f);     // Cyan Morning
        [SerializeField] private Color lunchRushColor = new Color(1f, 0.80f, 0.2f);   // Bright Gold
        [SerializeField] private Color afternoonColor = new Color(0.3f, 0.9f, 0.45f); // Green Delivery
        [SerializeField] private Color dinnerRushColor = new Color(1f, 0.45f, 0.15f); // Vibrant Orange
        [SerializeField] private Color closingColor = new Color(0.9f, 0.25f, 0.35f);  // Warning Crimson
        [SerializeField] private Color nightMarketColor = new Color(0.7f, 0.4f, 1f);  // Neon Purple

        private void Start()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.OnDayPhaseChanged += DayLoopManager_OnDayPhaseChanged;
                DayLoopManager.Instance.OnDayAdvanced += DayLoopManager_OnDayAdvanced;
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.OnDayPhaseChanged -= DayLoopManager_OnDayPhaseChanged;
                DayLoopManager.Instance.OnDayAdvanced -= DayLoopManager_OnDayAdvanced;
            }
        }

        private void Update()
        {
            if (DayLoopManager.Instance == null) return;

            // Update timer text and progress bar smoothly
            float timer = DayLoopManager.Instance.PhaseTimer;
            if (timer > 0f && timer < 1000f)
            {
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                if (shiftTimerText != null)
                {
                    shiftTimerText.text = $"{minutes:00}:{seconds:00}";
                }

                if (shiftProgressSlider != null)
                {
                    shiftProgressSlider.value = DayLoopManager.Instance.PhaseProgressNormalized;
                }
            }
            else
            {
                if (shiftTimerText != null)
                {
                    shiftTimerText.text = "";
                }
                if (shiftProgressSlider != null)
                {
                    shiftProgressSlider.value = 1f;
                }
            }
        }

        private void DayLoopManager_OnDayPhaseChanged(object sender, DayLoopManager.OnDayPhaseChangedEventArgs e)
        {
            UpdateDisplay();
        }

        private void DayLoopManager_OnDayAdvanced(object sender, DayLoopManager.OnDayAdvancedEventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (DayLoopManager.Instance == null) return;

            int day = DayLoopManager.Instance.CurrentDay;
            if (dayNumberText != null)
            {
                dayNumberText.text = $"DAY {day}";
            }

            DayLoopManager.DayPhase phase = DayLoopManager.Instance.CurrentPhase;
            if (shiftTitleText != null)
            {
                shiftTitleText.text = DayLoopManager.Instance.GetPhaseTitle();
            }

            Color accent = GetColorForPhase(phase);
            if (shiftAccentBar != null)
            {
                shiftAccentBar.color = accent;
            }
            if (shiftTitleText != null)
            {
                shiftTitleText.color = accent;
            }
        }

        private Color GetColorForPhase(DayLoopManager.DayPhase phase)
        {
            return phase switch
            {
                DayLoopManager.DayPhase.Shift1_MorningPrep => morningColor,
                DayLoopManager.DayPhase.Shift2_LunchRush   => lunchRushColor,
                DayLoopManager.DayPhase.Shift3_Afternoon   => afternoonColor,
                DayLoopManager.DayPhase.Shift4_DinnerRush  => dinnerRushColor,
                DayLoopManager.DayPhase.ClosingTime        => closingColor,
                DayLoopManager.DayPhase.NightMarket        => nightMarketColor,
                _ => morningColor
            };
        }
    }
}

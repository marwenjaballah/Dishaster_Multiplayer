using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Master State Machine for the Day Loop:
/// Day Start -> 4 Shifts (Morning, Lunch, Afternoon, Dinner) -> Closing Time -> Day Summary -> Night Market -> Day N+1
/// Server-authoritative with synchronized NetworkVariables for multiplayer clients.
/// </summary>
public class DayLoopManager : NetworkBehaviour {

    public static DayLoopManager Instance { get; private set; }

    public static void ResetStaticData() {
        OnAnyDayPhaseChanged = null;
        OnAnyDayAdvanced = null;
        OnAnyDaySummaryCalculated = null;
    }

    public static event EventHandler<OnDayPhaseChangedEventArgs> OnAnyDayPhaseChanged;
    public static event EventHandler<OnDayAdvancedEventArgs> OnAnyDayAdvanced;
    public static event EventHandler<OnDaySummaryCalculatedEventArgs> OnAnyDaySummaryCalculated;

    public event EventHandler<OnDayPhaseChangedEventArgs> OnDayPhaseChanged;
    public event EventHandler<OnDayAdvancedEventArgs> OnDayAdvanced;
    public event EventHandler<OnDaySummaryCalculatedEventArgs> OnDaySummaryCalculated;

    public enum DayPhase {
        DayStartAnnouncement = 0,
        Shift1_MorningPrep   = 1,
        Shift2_LunchRush     = 2,
        Shift3_Afternoon     = 3,
        Shift4_DinnerRush    = 4,
        ClosingTime          = 5,
        DaySummary           = 6,
        NightMarket          = 7,
        GameOver             = 8
    }

    public class OnDayPhaseChangedEventArgs : EventArgs {
        public DayPhase previousPhase;
        public DayPhase newPhase;
        public int dayNumber;
        public float phaseDuration;
    }

    public class OnDayAdvancedEventArgs : EventArgs {
        public int dayNumber;
        public int rentAmount;
    }

    public class OnDaySummaryCalculatedEventArgs : EventArgs {
        public int dayNumber;
        public int grossIncome;
        public int tips;
        public int rent;
        public int fines;
        public int netProfit;
        public int dishesDelivered;
        public float starRating;
        public int starsAwarded;
        public bool isDebtStrike;
        public int currentDebtStrikes;
        public bool isBankrupt;
    }

    [Header("⏱️ Shift Durations (Seconds)")]
    [SerializeField] private float dayStartDuration = 4f;
    [SerializeField] private float morningShiftDuration = 75f;
    [SerializeField] private float lunchRushDuration = 90f;
    [SerializeField] private float afternoonShiftDuration = 60f;
    [SerializeField] private float dinnerRushDuration = 90f;
    [SerializeField] private float closingTimeDuration = 30f;

    [Header("💵 Daily Rent Economy")]
    [SerializeField] private int baseDailyRent = 150;
    [SerializeField] private float rentInflationRate = 1.20f; // 20% growth per day
    [SerializeField] private int maxDebtStrikes = 2;          // 2 consecutive negative days = Bankruptcy

    // ── Synchronized State ────────────────────────────────────────────────────
    private readonly NetworkVariable<int> currentDay = new NetworkVariable<int>(1);
    private readonly NetworkVariable<DayPhase> currentPhase = new NetworkVariable<DayPhase>(DayPhase.DayStartAnnouncement);
    private readonly NetworkVariable<float> phaseTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> phaseDuration = new NetworkVariable<float>(4f);

    // Day Financial Stats (Reset each morning)
    private readonly NetworkVariable<int> dayGrossIncome = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> dayTips = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> dayRent = new NetworkVariable<int>(150);
    private readonly NetworkVariable<int> dayFines = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> dayNetProfit = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> dayDishesDelivered = new NetworkVariable<int>(0);
    private readonly NetworkVariable<int> dayStars = new NetworkVariable<int>(3);
    private readonly NetworkVariable<int> consecutiveDebtStrikes = new NetworkVariable<int>(0);
    private readonly NetworkVariable<bool> isBankruptcyGameOver = new NetworkVariable<bool>(false);

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        currentPhase.OnValueChanged += (prev, curr) => {
            OnDayPhaseChangedEventArgs args = new OnDayPhaseChangedEventArgs {
                previousPhase = prev,
                newPhase = curr,
                dayNumber = currentDay.Value,
                phaseDuration = phaseDuration.Value
            };
            OnDayPhaseChanged?.Invoke(this, args);
            OnAnyDayPhaseChanged?.Invoke(this, args);
        };

        currentDay.OnValueChanged += (prev, curr) => {
            OnDayAdvancedEventArgs args = new OnDayAdvancedEventArgs {
                dayNumber = curr,
                rentAmount = dayRent.Value
            };
            OnDayAdvanced?.Invoke(this, args);
            OnAnyDayAdvanced?.Invoke(this, args);
        };

        if (IsServer) {
            dayRent.Value = CalculateRentForDay(currentDay.Value);
            SetPhase(DayPhase.DayStartAnnouncement, dayStartDuration);
        }
    }

    private void Update() {
        if (!IsServer) return;
        if (KitchenGameManager.Instance != null && !KitchenGameManager.Instance.IsGamePlaying()) return;

        UpdatePhaseTimer();
    }

    private void UpdatePhaseTimer() {
        if (currentPhase.Value == DayPhase.DaySummary ||
            currentPhase.Value == DayPhase.NightMarket ||
            currentPhase.Value == DayPhase.GameOver) {
            return; // Waiting for players to proceed via UI RPC
        }

        phaseTimer.Value -= Time.deltaTime;

        if (phaseTimer.Value <= 0f) {
            AdvanceToNextPhase();
        }
    }

    private void AdvanceToNextPhase() {
        switch (currentPhase.Value) {
            case DayPhase.DayStartAnnouncement:
                SetPhase(DayPhase.Shift1_MorningPrep, morningShiftDuration);
                SyncDifficultyTier(Difficulty.DifficultyTier.Tier1_MorningPrep);
                break;

            case DayPhase.Shift1_MorningPrep:
                SetPhase(DayPhase.Shift2_LunchRush, lunchRushDuration);
                SyncDifficultyTier(Difficulty.DifficultyTier.Tier2_LunchRush);
                break;

            case DayPhase.Shift2_LunchRush:
                SetPhase(DayPhase.Shift3_Afternoon, afternoonShiftDuration);
                SyncDifficultyTier(Difficulty.DifficultyTier.Tier2_LunchRush);
                break;

            case DayPhase.Shift3_Afternoon:
                SetPhase(DayPhase.Shift4_DinnerRush, dinnerRushDuration);
                SyncDifficultyTier(Difficulty.DifficultyTier.Tier3_DinnerSurge);
                break;

            case DayPhase.Shift4_DinnerRush:
                SetPhase(DayPhase.ClosingTime, closingTimeDuration);
                break;

            case DayPhase.ClosingTime:
                CalculateAndTriggerDaySummary();
                break;
        }
    }

    private void SetPhase(DayPhase newPhase, float duration) {
        currentPhase.Value = newPhase;
        phaseDuration.Value = duration;
        phaseTimer.Value = duration;
    }

    private void SyncDifficultyTier(Difficulty.DifficultyTier tier) {
        if (Difficulty.DifficultyManager.Instance != null) {
            Difficulty.DifficultyManager.Instance.ForceSetDifficultyTier(tier);
        }
    }

    // ── Day Summary Calculation ───────────────────────────────────────────────

    private void CalculateAndTriggerDaySummary() {
        int gross = dayGrossIncome.Value;
        int tips = dayTips.Value;
        int fines = dayFines.Value;
        int rent = dayRent.Value;

        // Deduct rent through RestaurantEconomyManager
        if (RestaurantEconomyManager.Instance != null) {
            RestaurantEconomyManager.Instance.DeductRentAtEndOfDay(rent);
        }

        int netProfit = (gross + tips) - (rent + fines);
        dayNetProfit.Value = netProfit;

        // Evaluate customer star rating
        float avgRating = 3.5f;
        if (RestaurantEconomyManager.Instance != null) {
            avgRating = RestaurantEconomyManager.Instance.CurrentStarRating;
        }

        // Award Day Stars (1 to 3)
        int stars = 1;
        if (netProfit > 0 && avgRating >= 4.0f) {
            stars = 3;
        } else if (netProfit >= 0 || avgRating >= 3.0f) {
            stars = 2;
        }
        dayStars.Value = stars;

        // Debt & Bankruptcy Check
        int currentBalance = RestaurantEconomyManager.Instance != null ? RestaurantEconomyManager.Instance.CurrentBalance : 0;
        bool isDebt = currentBalance < 0;

        if (isDebt) {
            consecutiveDebtStrikes.Value++;
        } else {
            consecutiveDebtStrikes.Value = 0; // Reset debt strikes if balance is positive
        }

        bool bankrupt = consecutiveDebtStrikes.Value >= maxDebtStrikes;
        if (bankrupt) {
            isBankruptcyGameOver.Value = true;
            SetPhase(DayPhase.GameOver, float.MaxValue);
            if (RestaurantEconomyManager.Instance != null) {
                RestaurantEconomyManager.Instance.TriggerBankruptcy();
            }
        } else {
            SetPhase(DayPhase.DaySummary, float.MaxValue);
        }

        OnDaySummaryCalculatedEventArgs summaryArgs = new OnDaySummaryCalculatedEventArgs {
            dayNumber = currentDay.Value,
            grossIncome = gross,
            tips = tips,
            rent = rent,
            fines = fines,
            netProfit = netProfit,
            dishesDelivered = dayDishesDelivered.Value,
            starRating = avgRating,
            starsAwarded = stars,
            isDebtStrike = isDebt,
            currentDebtStrikes = consecutiveDebtStrikes.Value,
            isBankrupt = bankrupt
        };

        BroadcastDaySummaryClientRpc(
            summaryArgs.dayNumber,
            summaryArgs.grossIncome,
            summaryArgs.tips,
            summaryArgs.rent,
            summaryArgs.fines,
            summaryArgs.netProfit,
            summaryArgs.dishesDelivered,
            summaryArgs.starRating,
            summaryArgs.starsAwarded,
            summaryArgs.isDebtStrike,
            summaryArgs.currentDebtStrikes,
            summaryArgs.isBankrupt
        );
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastDaySummaryClientRpc(
        int dayNumber,
        int gross,
        int tips,
        int rent,
        int fines,
        int netProfit,
        int dishes,
        float rating,
        int stars,
        bool isDebt,
        int strikes,
        bool bankrupt) {

        OnDaySummaryCalculatedEventArgs args = new OnDaySummaryCalculatedEventArgs {
            dayNumber = dayNumber,
            grossIncome = gross,
            tips = tips,
            rent = rent,
            fines = fines,
            netProfit = netProfit,
            dishesDelivered = dishes,
            starRating = rating,
            starsAwarded = stars,
            isDebtStrike = isDebt,
            currentDebtStrikes = strikes,
            isBankrupt = bankrupt
        };

        OnDaySummaryCalculated?.Invoke(this, args);
        OnAnyDaySummaryCalculated?.Invoke(this, args);
    }

    // ── RPCs: State Transitions ───────────────────────────────────────────────

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestProceedToNightMarketRpc() {
        if (currentPhase.Value != DayPhase.DaySummary) return;
        SetPhase(DayPhase.NightMarket, float.MaxValue);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStartNextDayRpc() {
        if (currentPhase.Value != DayPhase.NightMarket && currentPhase.Value != DayPhase.DaySummary) return;

        // Advance Day
        currentDay.Value++;
        dayRent.Value = CalculateRentForDay(currentDay.Value);

        // Reset Daily Stats
        dayGrossIncome.Value = 0;
        dayTips.Value = 0;
        dayFines.Value = 0;
        dayNetProfit.Value = 0;
        dayDishesDelivered.Value = 0;
        dayStars.Value = 3;

        // Start Day N+1
        SetPhase(DayPhase.DayStartAnnouncement, dayStartDuration);
    }

    // ── Daily Financial Recording API ─────────────────────────────────────────

    public void RecordOrderCompleted(int payout, int tip) {
        if (!IsServer) return;
        dayGrossIncome.Value += payout;
        dayTips.Value += tip;
        dayDishesDelivered.Value++;
    }

    public void RecordFine(int fineAmount) {
        if (!IsServer) return;
        dayFines.Value += fineAmount;
    }

    public int CalculateRentForDay(int day) {
        if (day <= 1) return baseDailyRent;
        return Mathf.RoundToInt(baseDailyRent * Mathf.Pow(rentInflationRate, day - 1));
    }

    // ── Public Getters ────────────────────────────────────────────────────────

    public int CurrentDay => currentDay.Value;
    public DayPhase CurrentPhase => currentPhase.Value;
    public float PhaseTimer => phaseTimer.Value;
    public float PhaseDuration => phaseDuration.Value;
    public float PhaseProgressNormalized => phaseDuration.Value > 0f ? Mathf.Clamp01(1f - (phaseTimer.Value / phaseDuration.Value)) : 0f;
    public int DayGrossIncome => dayGrossIncome.Value;
    public int DayTips => dayTips.Value;
    public int DayRent => dayRent.Value;
    public int DayFines => dayFines.Value;
    public int DayNetProfit => dayNetProfit.Value;
    public int DayDishesDelivered => dayDishesDelivered.Value;
    public int DayStars => dayStars.Value;
    public int ConsecutiveDebtStrikes => consecutiveDebtStrikes.Value;
    public bool IsBankrupt => isBankruptcyGameOver.Value;

    public bool CanAcceptNewOrders() {
        return currentPhase.Value == DayPhase.Shift1_MorningPrep ||
               currentPhase.Value == DayPhase.Shift2_LunchRush ||
               currentPhase.Value == DayPhase.Shift3_Afternoon ||
               currentPhase.Value == DayPhase.Shift4_DinnerRush;
    }

    public bool IsActiveShift() {
        return CanAcceptNewOrders() || currentPhase.Value == DayPhase.ClosingTime;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddShiftBonusTimeServerRpc(float seconds) {
        if (CanAcceptNewOrders()) {
            phaseTimer.Value += seconds;
            NotifyShiftTimeBonusClientRpc(seconds);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyShiftTimeBonusClientRpc(float seconds) {
        Debug.Log($"[DayLoopManager] Combo Reward! +{seconds:F0}s added to current shift.");
    }

    public string GetPhaseTitle() {
        return currentPhase.Value switch {
            DayPhase.DayStartAnnouncement => $"Day {currentDay.Value} Starting!",
            DayPhase.Shift1_MorningPrep   => "Shift 1: Morning Prep",
            DayPhase.Shift2_LunchRush     => "Shift 2: Lunch Rush",
            DayPhase.Shift3_Afternoon     => "Shift 3: Afternoon Orders",
            DayPhase.Shift4_DinnerRush    => "Shift 4: Dinner Rush",
            DayPhase.ClosingTime          => "Closing Time: Finish Orders!",
            DayPhase.DaySummary           => $"Day {currentDay.Value} Summary",
            DayPhase.NightMarket          => "Night Market Equipment Shop",
            DayPhase.GameOver             => "Restaurant Bankrupt!",
            _ => "Operating"
        };
    }
}

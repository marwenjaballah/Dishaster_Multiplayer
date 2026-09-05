using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the restaurant survival economy in Table Service mode:
/// - Cash balance & earnings/fines
/// - Dynamic customer star ratings using weighted moving average
/// - Recurring rent/utility bills countdown and bankruptcy fail condition
/// - Crisis penalty tracking (lingering blackouts/fires)
/// </summary>
public class RestaurantEconomyManager : NetworkBehaviour {

    public static RestaurantEconomyManager Instance { get; private set; }

    // ===== EVENTS =====
    public event EventHandler<OnBalanceChangedEventArgs> OnBalanceChanged;
    public event EventHandler<OnRatingChangedEventArgs> OnRatingChanged;
    public event EventHandler<OnReviewSubmittedEventArgs> OnReviewSubmitted;
    public event EventHandler<OnBillPaidEventArgs> OnBillPaid;
    public event EventHandler OnBillWarning;
    public event EventHandler OnBankruptcy;

    public class OnBalanceChangedEventArgs : EventArgs {
        public int currentBalance;
        public int changeAmount;
        public string reason;
    }

    public class OnRatingChangedEventArgs : EventArgs {
        public float currentRating;
        public int totalReviews;
    }

    public class OnReviewSubmittedEventArgs : EventArgs {
        public float reviewStars;
        public string reviewComment;
    }

    public class OnBillPaidEventArgs : EventArgs {
        public int billAmount;
        public int cycleNumber;
        public bool wasSuccessful;
    }

    [Header("Starting Economy Settings")]
    [SerializeField] private int startingBalance = 100;
    [SerializeField] private float startingStarRating = 3.5f;
    [SerializeField] private int initialReviewWeight = 5;

    [Header("Bill / Rent Settings")]
    [SerializeField] private float billInterval = 75f; // Seconds per rent cycle
    [SerializeField] private int initialBillAmount = 60;
    [SerializeField] private float billInflationRate = 1.35f; // Multiplier per cycle
    [SerializeField] private float warningTimeBeforeBill = 15f;

    [Header("Wrong Order & Penalty Settings")]
    [SerializeField] private int wrongOrderFine = 15;
    [SerializeField] private float crisisGracePeriod = 10f; // Seconds before crisis starts hurting rating
    [SerializeField] private float crisisPenaltyInterval = 10f; // Review penalty tick every X seconds

    // Networked Variables
    private NetworkVariable<int> currentBalance = new NetworkVariable<int>(100);
    private NetworkVariable<float> currentStarRating = new NetworkVariable<float>(3.5f);
    private NetworkVariable<int> totalReviewsCount = new NetworkVariable<int>(5);
    private NetworkVariable<float> nextBillTimer = new NetworkVariable<float>(75f);
    private NetworkVariable<int> currentBillAmount = new NetworkVariable<int>(60);
    private NetworkVariable<int> currentBillCycle = new NetworkVariable<int>(1);
    private NetworkVariable<bool> isBankrupt = new NetworkVariable<bool>(false);

    // Crisis tracking
    private float crisisDurationTimer = 0f;
    private float crisisPenaltyTimer = 0f;
    private bool warningFiredThisCycle = false;

    private void Awake() {
        Instance = this;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        currentBalance.OnValueChanged += (prev, curr) => {
            OnBalanceChanged?.Invoke(this, new OnBalanceChangedEventArgs {
                currentBalance = curr,
                changeAmount = curr - prev,
                reason = curr > prev ? "Income" : "Expense"
            });
        };

        currentStarRating.OnValueChanged += (prev, curr) => {
            OnRatingChanged?.Invoke(this, new OnRatingChangedEventArgs {
                currentRating = curr,
                totalReviews = totalReviewsCount.Value
            });
        };

        if (IsServer) {
            currentBalance.Value = startingBalance;
            currentStarRating.Value = startingStarRating;
            totalReviewsCount.Value = initialReviewWeight;
            nextBillTimer.Value = billInterval;
            currentBillAmount.Value = initialBillAmount;
            currentBillCycle.Value = 1;
            isBankrupt.Value = false;
        }
    }

    private void Update() {
        if (!IsServer) return;
        if (isBankrupt.Value) return;
        if (KitchenGameManager.Instance == null || !KitchenGameManager.Instance.IsGamePlaying()) return;

        // Bill cycle countdown
        nextBillTimer.Value -= Time.deltaTime;

        if (nextBillTimer.Value <= warningTimeBeforeBill && !warningFiredThisCycle) {
            warningFiredThisCycle = true;
            NotifyBillWarningClientRpc();
        }

        if (nextBillTimer.Value <= 0f) {
            ProcessBillPayment();
        }

        // Crisis tracking
        HandleCrisisReputationDecay();
    }

    // ===== REPUTATION & REVIEW FORMULA =====
    /// <summary>
    /// Applies the weighted moving average formula:
    /// New Rating = ((Current Rating * Total Reviews) + New Stars) / (Total Reviews + 1)
    /// </summary>
    public void SubmitCustomerReview(float stars, string reason = "") {
        if (!IsServer) return;

        float oldRating = currentStarRating.Value;
        int currentCount = totalReviewsCount.Value;

        float newRating = ((oldRating * currentCount) + stars) / (currentCount + 1);
        newRating = Mathf.Clamp(newRating, 1.0f, 5.0f);

        currentStarRating.Value = (float)Math.Round(newRating, 2);
        totalReviewsCount.Value = currentCount + 1;

        NotifyReviewSubmittedClientRpc(stars, reason);
    }

    [ClientRpc]
    private void NotifyReviewSubmittedClientRpc(float stars, string reason) {
        OnReviewSubmitted?.Invoke(this, new OnReviewSubmittedEventArgs {
            reviewStars = stars,
            reviewComment = reason
        });
    }

    // ===== DELIVERY REWARDS & FINES =====

    public void ProcessDeliveryReward(int basePayout, int tip, float reviewStars) {
        if (!IsServer) return;

        // Apply bonus tip if rating is high (>= 4.5 stars)
        if (currentStarRating.Value >= 4.5f) {
            tip = Mathf.RoundToInt(tip * 1.25f);
        }

        int totalEarnings = basePayout + tip;
        currentBalance.Value += totalEarnings;

        SubmitCustomerReview(reviewStars, tip > 0 ? "Fast & Delicious!" : "Good Food");
    }

    public void ProcessWrongDeliveryPenalty() {
        if (!IsServer) return;

        currentBalance.Value -= wrongOrderFine;
        SubmitCustomerReview(1.0f, "Wrong Dish Rejected!");
    }

    public void ProcessExpiredOrderPenalty() {
        if (!IsServer) return;

        SubmitCustomerReview(1.0f, "Left Due To Slow Service!");
    }

    // ===== BILL PROCESSING & BANKRUPTCY =====

    private void ProcessBillPayment() {
        int bill = currentBillAmount.Value;
        int cycle = currentBillCycle.Value;

        if (currentBalance.Value >= bill) {
            // Bill paid successfully
            currentBalance.Value -= bill;
            currentBillCycle.Value = cycle + 1;
            currentBillAmount.Value = Mathf.RoundToInt(initialBillAmount * Mathf.Pow(billInflationRate, cycle));
            nextBillTimer.Value = billInterval;
            warningFiredThisCycle = false;

            NotifyBillPaidClientRpc(bill, cycle, true);
        } else {
            // Cannot afford bill -> Bankruptcy!
            currentBalance.Value -= bill; // Dips into negative
            isBankrupt.Value = true;

            NotifyBillPaidClientRpc(bill, cycle, false);
            NotifyBankruptcyClientRpc();
        }
    }

    [ClientRpc]
    private void NotifyBillWarningClientRpc() {
        OnBillWarning?.Invoke(this, EventArgs.Empty);
    }

    [ClientRpc]
    private void NotifyBillPaidClientRpc(int amount, int cycle, bool success) {
        OnBillPaid?.Invoke(this, new OnBillPaidEventArgs {
            billAmount = amount,
            cycleNumber = cycle,
            wasSuccessful = success
        });
    }

    [ClientRpc]
    private void NotifyBankruptcyClientRpc() {
        OnBankruptcy?.Invoke(this, EventArgs.Empty);
    }

    // ===== CRISIS RESPONSE MONITORING =====

    private void HandleCrisisReputationDecay() {
        if (KitchenCrisisManager.Instance == null) return;

        bool hasCrisis = KitchenCrisisManager.Instance.HasActiveCrisis;

        if (hasCrisis) {
            crisisDurationTimer += Time.deltaTime;

            if (crisisDurationTimer > crisisGracePeriod) {
                crisisPenaltyTimer += Time.deltaTime;
                if (crisisPenaltyTimer >= crisisPenaltyInterval) {
                    crisisPenaltyTimer = 0f;
                    SubmitCustomerReview(1.0f, "Chaos in the kitchen!");
                }
            }
        } else {
            crisisDurationTimer = 0f;
            crisisPenaltyTimer = 0f;
        }
    }

    // ===== GETTERS =====

    public int GetCurrentBalance() => currentBalance.Value;
    public float GetStarRating() => currentStarRating.Value;
    public int GetTotalReviews() => totalReviewsCount.Value;
    public float GetNextBillTimer() => nextBillTimer.Value;
    public int GetCurrentBillAmount() => currentBillAmount.Value;
    public int GetCurrentBillCycle() => currentBillCycle.Value;
    public bool IsBankrupt() => isBankrupt.Value;
    public int GetWrongOrderFine() => wrongOrderFine;

    public override void OnDestroy() {
        base.OnDestroy();
        if (Instance == this) {
            Instance = null;
        }
    }
}

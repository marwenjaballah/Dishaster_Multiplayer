using System;
using Unity.Netcode;
using UnityEngine;

namespace Difficulty
{
    public enum DifficultyTier
    {
        Tier1_MorningPrep = 0, // Very Easy / Calm
        Tier2_LunchRush   = 1, // Moderate
        Tier3_DinnerSurge = 2, // Challenging
        Tier4_Overdrive   = 3  // Extreme Chaos
    }

    /// <summary>
    /// Master Coordinator for game difficulty & progression.
    /// Manages time-based shift transitions and dynamically scales crises,
    /// dining table orders, sidewalk deliveries, and economy parameters.
    /// All tier settings are fully customizable in the Unity Inspector.
    /// </summary>
    public class DifficultyManager : NetworkBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────────
        public event EventHandler<OnDifficultyTierChangedEventArgs> OnDifficultyTierChanged;
        public class OnDifficultyTierChangedEventArgs : EventArgs
        {
            public DifficultyTier previousTier;
            public DifficultyTier newTier;
            public string tierTitle;
            public string tierDescription;
        }

        [System.Serializable]
        public struct TierSettings
        {
            [Header("⏱️ Shift Info & Duration")]
            [Tooltip("Display name for this shift (e.g. Morning Prep, Lunch Rush).")]
            public string tierName;
            [Tooltip("Subtitle description shown on the HUD announcement banner.")]
            public string tierDescription;
            [Tooltip("How long this shift lasts (in seconds) before advancing to the next shift.")]
            public float durationSeconds;

            [Header("🔥 Kitchen Crises (Time Between Crises)")]
            [Tooltip("Minimum seconds between kitchen crisis events in this shift.")]
            public float crisisMinInterval;
            [Tooltip("Maximum seconds between kitchen crisis events in this shift.")]
            public float crisisMaxInterval;
            [Tooltip("Seconds players have to resolve/extinguish a crisis before consequences occur.")]
            public float crisisGracePeriod;

            [Header("🛵 Sidewalk Deliveries (Time Between Deliveries)")]
            [Tooltip("Maximum sidewalk delivery customers active outside at the same time.")]
            public int maxDeliveryCustomers;
            [Tooltip("Patience timer (seconds) for each sidewalk delivery customer.")]
            public float deliveryOrderTimer;
            [Tooltip("Time (seconds) between spawning new sidewalk delivery customers.")]
            public float deliverySpawnInterval;

            [Header("🍽️ Dining Tables (Time Between Table Orders)")]
            [Tooltip("Maximum tables that can have active customer orders at the same time.")]
            public int maxConcurrentTableOrders;
            [Tooltip("Minimum seconds after a table is cleared before a new customer sits/orders.")]
            public float tableMinRespawnDelay;
            [Tooltip("Maximum seconds after a table is cleared before a new customer sits/orders.")]
            public float tableMaxRespawnDelay;

            [Header("🍽️ Table Service Economy")]
            [Tooltip("Base payout for completing a dining table order.")]
            public int tableBasePayout;
            [Tooltip("Bonus cash per ingredient on the plate for table orders.")]
            public int tablePerIngredientBonus;
            [Tooltip("Max tip for fast table service (>75% patience).")]
            public int tableMaxTip;
            [Tooltip("Fine deducted when a table receives the wrong order.")]
            public int tableWrongOrderFine;

            [Header("🛵 Sidewalk Delivery Economy")]
            [Tooltip("Base payout for completing a sidewalk delivery.")]
            public int deliveryBasePayout;
            [Tooltip("Bonus cash per ingredient on the dish for sidewalk delivery.")]
            public int deliveryPerIngredientBonus;
            [Tooltip("Max tip for fast sidewalk delivery (>75% patience).")]
            public int deliveryMaxTip;
            [Tooltip("Fine deducted when a sidewalk customer receives the wrong delivery.")]
            public int deliveryWrongOrderFine;

            [Header("💎 Global Shift Tip Multiplier")]
            [Tooltip("Tip multiplier applied to all deliveries during this shift.")]
            public float tipMultiplier;
        }

        // ── Inspector Settings ────────────────────────────────────────────────────
        [Header("Initial Calm Period")]
        [Tooltip("Seconds at game start before any crisis can possibly trigger.")]
        [SerializeField] private float initialCrisisImmunitySeconds = 60f;

        [Header("Customizable Shift Tiers")]
        [SerializeField] private TierSettings tier1Settings = new TierSettings
        {
            tierName = "Morning Prep",
            tierDescription = "Calm Pacing • Easy Orders",
            durationSeconds = 90f,
            crisisMinInterval = 70f,
            crisisMaxInterval = 90f,
            crisisGracePeriod = 20f,
            maxDeliveryCustomers = 1,
            deliveryOrderTimer = 120f,
            deliverySpawnInterval = 20f,
            maxConcurrentTableOrders = 2,
            tableMinRespawnDelay = 14f,
            tableMaxRespawnDelay = 22f,
            tableBasePayout = 25,
            tablePerIngredientBonus = 5,
            tableMaxTip = 15,
            tableWrongOrderFine = 10,
            deliveryBasePayout = 35,
            deliveryPerIngredientBonus = 8,
            deliveryMaxTip = 25,
            deliveryWrongOrderFine = 10,
            tipMultiplier = 1.25f
        };

        [SerializeField] private TierSettings tier2Settings = new TierSettings
        {
            tierName = "Lunch Rush",
            tierDescription = "Moderate Pacing • Orders Increasing",
            durationSeconds = 150f,
            crisisMinInterval = 45f,
            crisisMaxInterval = 60f,
            crisisGracePeriod = 14f,
            maxDeliveryCustomers = 2,
            deliveryOrderTimer = 100f,
            deliverySpawnInterval = 14f,
            maxConcurrentTableOrders = 3,
            tableMinRespawnDelay = 9f,
            tableMaxRespawnDelay = 16f,
            tableBasePayout = 25,
            tablePerIngredientBonus = 5,
            tableMaxTip = 15,
            tableWrongOrderFine = 15,
            deliveryBasePayout = 35,
            deliveryPerIngredientBonus = 8,
            deliveryMaxTip = 25,
            deliveryWrongOrderFine = 15,
            tipMultiplier = 1.0f
        };

        [SerializeField] private TierSettings tier3Settings = new TierSettings
        {
            tierName = "Dinner Surge",
            tierDescription = "High Intensity • Faster Orders",
            durationSeconds = 160f,
            crisisMinInterval = 30f,
            crisisMaxInterval = 45f,
            crisisGracePeriod = 10f,
            maxDeliveryCustomers = 3,
            deliveryOrderTimer = 85f,
            deliverySpawnInterval = 10f,
            maxConcurrentTableOrders = 5,
            tableMinRespawnDelay = 6f,
            tableMaxRespawnDelay = 11f,
            tableBasePayout = 30,
            tablePerIngredientBonus = 6,
            tableMaxTip = 20,
            tableWrongOrderFine = 20,
            deliveryBasePayout = 40,
            deliveryPerIngredientBonus = 10,
            deliveryMaxTip = 35,
            deliveryWrongOrderFine = 20,
            tipMultiplier = 1.15f
        };

        [SerializeField] private TierSettings tier4Settings = new TierSettings
        {
            tierName = "Kitchen Overdrive",
            tierDescription = "Maximum Chaos • Double Tips!",
            durationSeconds = float.MaxValue,
            crisisMinInterval = 22f,
            crisisMaxInterval = 35f,
            crisisGracePeriod = 7f,
            maxDeliveryCustomers = 4,
            deliveryOrderTimer = 70f,
            deliverySpawnInterval = 8f,
            maxConcurrentTableOrders = 6,
            tableMinRespawnDelay = 4f,
            tableMaxRespawnDelay = 8f,
            tableBasePayout = 35,
            tablePerIngredientBonus = 8,
            tableMaxTip = 30,
            tableWrongOrderFine = 25,
            deliveryBasePayout = 50,
            deliveryPerIngredientBonus = 12,
            deliveryMaxTip = 50,
            deliveryWrongOrderFine = 25,
            tipMultiplier = 1.5f
        };

        // ── Networked State ───────────────────────────────────────────────────────
        private readonly NetworkVariable<DifficultyTier> _currentTier = new(
            DifficultyTier.Tier1_MorningPrep,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float _elapsedPlayingTime;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _currentTier.OnValueChanged += OnTierValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _currentTier.OnValueChanged -= OnTierValueChanged;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (KitchenGameManager.Instance == null || !KitchenGameManager.Instance.IsGamePlaying()) return;

            _elapsedPlayingTime += Time.deltaTime;
            EvaluateDifficultyProgression();
        }

        // ── Tier Evaluation ───────────────────────────────────────────────────────

        private void EvaluateDifficultyProgression()
        {
            float t1End = tier1Settings.durationSeconds;
            float t2End = t1End + tier2Settings.durationSeconds;
            float t3End = t2End + tier3Settings.durationSeconds;

            DifficultyTier evaluatedTier;
            if (_elapsedPlayingTime < t1End)
            {
                evaluatedTier = DifficultyTier.Tier1_MorningPrep;
            }
            else if (_elapsedPlayingTime < t2End)
            {
                evaluatedTier = DifficultyTier.Tier2_LunchRush;
            }
            else if (_elapsedPlayingTime < t3End)
            {
                evaluatedTier = DifficultyTier.Tier3_DinnerSurge;
            }
            else
            {
                evaluatedTier = DifficultyTier.Tier4_Overdrive;
            }

            if (_currentTier.Value != evaluatedTier)
            {
                _currentTier.Value = evaluatedTier;
            }
        }

        private void OnTierValueChanged(DifficultyTier previousTier, DifficultyTier newTier)
        {
            TierSettings settings = GetSettingsForTier(newTier);
            Debug.Log($"[DifficultyManager] Difficulty advanced to {newTier} ({settings.tierName})!");

            OnDifficultyTierChanged?.Invoke(this, new OnDifficultyTierChangedEventArgs
            {
                previousTier = previousTier,
                newTier = newTier,
                tierTitle = settings.tierName,
                tierDescription = settings.tierDescription
            });
        }

        // ── Public Getters for Subsystems ─────────────────────────────────────────

        public DifficultyTier CurrentTier => _currentTier.Value;

        public TierSettings CurrentSettings => GetSettingsForTier(_currentTier.Value);

        public TierSettings GetSettingsForTier(DifficultyTier tier)
        {
            return tier switch
            {
                DifficultyTier.Tier1_MorningPrep => tier1Settings,
                DifficultyTier.Tier2_LunchRush   => tier2Settings,
                DifficultyTier.Tier3_DinnerSurge => tier3Settings,
                DifficultyTier.Tier4_Overdrive   => tier4Settings,
                _ => tier1Settings
            };
        }

        public bool IsInInitialCrisisImmunity()
        {
            return _elapsedPlayingTime < initialCrisisImmunitySeconds;
        }

        public float ElapsedPlayingTime => _elapsedPlayingTime;

        // ── Optimized Payout & Economy Calculations ───────────────────────────────

        /// <summary>
        /// Calculates base pay, tip, and review stars for a table delivery based on current shift tier.
        /// </summary>
        public void CalculateTablePayout(int ingredientCount, float patience, out int basePayout, out int tip, out float stars)
        {
            TierSettings s = CurrentSettings;
            basePayout = s.tableBasePayout + (ingredientCount * s.tablePerIngredientBonus);

            if (patience >= 0.75f)
            {
                stars = 5.0f;
                tip = s.tableMaxTip;
            }
            else if (patience >= 0.40f)
            {
                stars = 4.5f;
                tip = Mathf.RoundToInt(s.tableMaxTip * 0.6f);
            }
            else if (patience >= 0.20f)
            {
                stars = 4.0f;
                tip = Mathf.RoundToInt(s.tableMaxTip * 0.3f);
            }
            else
            {
                stars = 3.0f;
                tip = Mathf.Max(1, Mathf.RoundToInt(s.tableMaxTip * 0.1f));
            }

            tip = Mathf.RoundToInt(tip * s.tipMultiplier);
        }

        /// <summary>
        /// Calculates base pay, tip, and review stars for a sidewalk delivery based on current shift tier.
        /// </summary>
        public void CalculateDeliveryPayout(int ingredientCount, float patience, out int basePayout, out int tip, out float stars)
        {
            TierSettings s = CurrentSettings;
            basePayout = s.deliveryBasePayout + (ingredientCount * s.deliveryPerIngredientBonus);

            if (patience >= 0.75f)
            {
                stars = 5.0f;
                tip = s.deliveryMaxTip;
            }
            else if (patience >= 0.40f)
            {
                stars = 4.5f;
                tip = Mathf.RoundToInt(s.deliveryMaxTip * 0.6f);
            }
            else if (patience >= 0.20f)
            {
                stars = 4.0f;
                tip = Mathf.RoundToInt(s.deliveryMaxTip * 0.3f);
            }
            else
            {
                stars = 3.0f;
                tip = Mathf.Max(1, Mathf.RoundToInt(s.deliveryMaxTip * 0.1f));
            }

            tip = Mathf.RoundToInt(tip * s.tipMultiplier);
        }

        public int GetTableWrongOrderFine() => CurrentSettings.tableWrongOrderFine;
        public int GetDeliveryWrongOrderFine() => CurrentSettings.deliveryWrongOrderFine;
    }
}

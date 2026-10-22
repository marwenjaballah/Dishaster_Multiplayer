using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative manager for the Night Market upgrade catalog.
/// Players spend accumulated restaurant funds between shifts on permanent equipment upgrades.
/// </summary>
public class NightMarketManager : NetworkBehaviour {

    public static NightMarketManager Instance { get; private set; }

    public static void ResetStaticData() {
        OnAnyUpgradePurchased = null;
    }

    public static event EventHandler<OnUpgradePurchasedEventArgs> OnAnyUpgradePurchased;
    public event EventHandler<OnUpgradePurchasedEventArgs> OnUpgradePurchased;

    public enum UpgradeType {
        TurboScooter         = 0,
        AutoChopStation      = 1,
        FireSprinklerSystem  = 2,
        HeavyDutyDeliveryBag = 3,
        NonSlipShoes         = 4,
        SpeedyStove          = 5
    }

    public class OnUpgradePurchasedEventArgs : EventArgs {
        public UpgradeType upgradeType;
        public string title;
        public int cost;
    }

    [System.Serializable]
    public struct UpgradeInfo {
        public UpgradeType type;
        public string title;
        public string description;
        public int cost;
        public string iconName;
    }

    private static readonly UpgradeInfo[] Catalog = new UpgradeInfo[] {
        new UpgradeInfo {
            type = UpgradeType.TurboScooter,
            title = "Turbo Electric Scooter",
            description = "+35% Top speed & faster acceleration across city streets.",
            cost = 120,
            iconName = "Scooter"
        },
        new UpgradeInfo {
            type = UpgradeType.AutoChopStation,
            title = "Auto-Chop Knife",
            description = "Double chopping speed on all cutting counters.",
            cost = 100,
            iconName = "Knife"
        },
        new UpgradeInfo {
            type = UpgradeType.FireSprinklerSystem,
            title = "Kitchen Sprinklers",
            description = "Auto-extinguishes stove grease fires within 5 seconds.",
            cost = 140,
            iconName = "Sprinkler"
        },
        new UpgradeInfo {
            type = UpgradeType.HeavyDutyDeliveryBag,
            title = "Heavy-Duty Thermal Bag",
            description = "Expands delivery bag capacity from 3 meals to 5 meals.",
            cost = 150,
            iconName = "Bag"
        },
        new UpgradeInfo {
            type = UpgradeType.NonSlipShoes,
            title = "Non-Slip Chef Shoes",
            description = "Immune to puddle slips and movement slowdowns.",
            cost = 80,
            iconName = "Shoes"
        },
        new UpgradeInfo {
            type = UpgradeType.SpeedyStove,
            title = "Turbo Induction Stove",
            description = "Patties & ingredients cook 30% faster on all stoves.",
            cost = 110,
            iconName = "Stove"
        }
    };

    // Bitmask of purchased upgrades (bit index = (int)UpgradeType)
    private readonly NetworkVariable<ulong> purchasedUpgradesMask = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        purchasedUpgradesMask.OnValueChanged += (prev, curr) => {
            // Find newly purchased upgrades
            for (int i = 0; i < Catalog.Length; i++) {
                ulong bit = 1UL << i;
                if ((curr & bit) != 0 && (prev & bit) == 0) {
                    UpgradeInfo info = Catalog[i];
                    OnUpgradePurchasedEventArgs args = new OnUpgradePurchasedEventArgs {
                        upgradeType = (UpgradeType)i,
                        title = info.title,
                        cost = info.cost
                    };
                    OnUpgradePurchased?.Invoke(this, args);
                    OnAnyUpgradePurchased?.Invoke(this, args);
                }
            }
        };
    }

    // ── Purchase RPC ──────────────────────────────────────────────────────────

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestPurchaseUpgradeRpc(UpgradeType upgradeType) {
        if (HasUpgrade(upgradeType)) {
            Debug.LogWarning($"[NightMarket] Upgrade {upgradeType} is already unlocked!");
            return;
        }

        UpgradeInfo info = GetUpgradeInfo(upgradeType);

        if (RestaurantEconomyManager.Instance == null) {
            Debug.LogError("[NightMarket] Cannot purchase upgrade: RestaurantEconomyManager not found!");
            return;
        }

        if (RestaurantEconomyManager.Instance.CurrentBalance < info.cost) {
            Debug.LogWarning($"[NightMarket] Insufficient funds to buy {info.title}! Need ${info.cost}, have ${RestaurantEconomyManager.Instance.CurrentBalance}.");
            return;
        }

        // Deduct balance and apply upgrade
        bool success = RestaurantEconomyManager.Instance.DeductBalance(info.cost);
        if (success) {
            purchasedUpgradesMask.Value |= (1UL << (int)upgradeType);
            Debug.Log($"[NightMarket] Successfully purchased {info.title} for ${info.cost}!");
        }
    }

    // ── Public Query API ──────────────────────────────────────────────────────

    public bool HasUpgrade(UpgradeType upgradeType) {
        ulong bit = 1UL << (int)upgradeType;
        return (purchasedUpgradesMask.Value & bit) != 0;
    }

    public UpgradeInfo GetUpgradeInfo(UpgradeType upgradeType) {
        int index = (int)upgradeType;
        if (index >= 0 && index < Catalog.Length) {
            return Catalog[index];
        }
        return default;
    }

    public IReadOnlyList<UpgradeInfo> GetAllUpgrades() => Catalog;

    public ulong GetPurchasedUpgradesMask() => purchasedUpgradesMask.Value;

    public void SetPurchasedUpgradesMaskServer(ulong mask) {
        if (!IsServer) return;
        purchasedUpgradesMask.Value = mask;
    }
}

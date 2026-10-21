using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Displays the Night Market equipment catalog between shifts.
    /// Allows players to purchase restaurant and vehicle upgrades and advance to Day N+1.
    /// </summary>
    public class NightMarketShopUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Header Texts")]
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI balanceText;

        [Header("Upgrade Card Items")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private GameObject upgradeCardPrefab;

        [Header("Footer Actions")]
        [SerializeField] private Button startNextDayButton;
        [SerializeField] private TextMeshProUGUI nextDayButtonText;

        [Header("Visual Colors")]
        [SerializeField] private Color affordableColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color unaffordableColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color ownedColor = new Color(0.5f, 0.5f, 0.5f);

        // Internal card representation
        private readonly List<UpgradeCardUI> _activeCards = new List<UpgradeCardUI>();

        private class UpgradeCardUI
        {
            public NightMarketManager.UpgradeType type;
            public GameObject cardObject;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI descText;
            public TextMeshProUGUI costText;
            public Button purchaseButton;
            public TextMeshProUGUI buttonLabel;
        }

        private void Awake()
        {
            if (startNextDayButton != null)
            {
                startNextDayButton.onClick.AddListener(OnStartNextDayClicked);
            }
        }

        private void Start()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.OnDayPhaseChanged += DayLoopManager_OnDayPhaseChanged;
            }

            if (NightMarketManager.Instance != null)
            {
                NightMarketManager.Instance.OnUpgradePurchased += NightMarket_OnUpgradePurchased;
            }

            if (RestaurantEconomyManager.Instance != null)
            {
                RestaurantEconomyManager.Instance.OnBalanceChanged += Economy_OnBalanceChanged;
            }

            InitializeCatalogCards();
            Hide();
        }

        private void OnDestroy()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.OnDayPhaseChanged -= DayLoopManager_OnDayPhaseChanged;
            }

            if (NightMarketManager.Instance != null)
            {
                NightMarketManager.Instance.OnUpgradePurchased -= NightMarket_OnUpgradePurchased;
            }

            if (RestaurantEconomyManager.Instance != null)
            {
                RestaurantEconomyManager.Instance.OnBalanceChanged -= Economy_OnBalanceChanged;
            }
        }

        private void DayLoopManager_OnDayPhaseChanged(object sender, DayLoopManager.OnDayPhaseChangedEventArgs e)
        {
            if (e.newPhase == DayLoopManager.DayPhase.NightMarket)
            {
                RefreshDisplay();
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void NightMarket_OnUpgradePurchased(object sender, NightMarketManager.OnUpgradePurchasedEventArgs e)
        {
            RefreshDisplay();
        }

        private void Economy_OnBalanceChanged(object sender, RestaurantEconomyManager.OnBalanceChangedEventArgs e)
        {
            UpdateBalanceText(e.currentBalance);
            RefreshCardButtonStates();
        }

        private void InitializeCatalogCards()
        {
            if (cardsContainer == null || NightMarketManager.Instance == null) return;

            // Clear previous cards
            _activeCards.Clear();
            var catalog = NightMarketManager.Instance.GetAllUpgrades();

            foreach (var item in catalog)
            {
                NightMarketManager.UpgradeType type = item.type;
                GameObject cardObj = null;

                if (upgradeCardPrefab != null)
                {
                    cardObj = Instantiate(upgradeCardPrefab, cardsContainer);
                }
                else
                {
                    // Fallback to finding pre-existing cards by name
                    Transform existing = cardsContainer.Find($"Card_{type}");
                    if (existing != null) cardObj = existing.gameObject;
                }

                if (cardObj == null) continue;

                UpgradeCardUI cardUI = new UpgradeCardUI
                {
                    type = type,
                    cardObject = cardObj,
                    titleText = cardObj.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>(),
                    descText = cardObj.transform.Find("DescText")?.GetComponent<TextMeshProUGUI>(),
                    costText = cardObj.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>(),
                    purchaseButton = cardObj.transform.Find("PurchaseButton")?.GetComponent<Button>()
                };

                if (cardUI.purchaseButton != null)
                {
                    cardUI.buttonLabel = cardUI.purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
                    cardUI.purchaseButton.onClick.AddListener(() => OnBuyClicked(type));
                }

                if (cardUI.titleText != null) cardUI.titleText.text = item.title;
                if (cardUI.descText != null) cardUI.descText.text = item.description;
                if (cardUI.costText != null) cardUI.costText.text = $"${item.cost}";

                _activeCards.Add(cardUI);
            }
        }

        private void RefreshDisplay()
        {
            int balance = RestaurantEconomyManager.Instance != null ? RestaurantEconomyManager.Instance.CurrentBalance : 0;
            UpdateBalanceText(balance);

            int nextDay = (DayLoopManager.Instance != null ? DayLoopManager.Instance.CurrentDay : 1) + 1;
            if (nextDayButtonText != null)
            {
                nextDayButtonText.text = $"START DAY {nextDay} ➔";
            }

            RefreshCardButtonStates();
        }

        private void UpdateBalanceText(int balance)
        {
            if (balanceText != null)
            {
                balanceText.text = $"Available Balance: ${balance}";
                balanceText.color = balance >= 0 ? affordableColor : unaffordableColor;
            }
        }

        private void RefreshCardButtonStates()
        {
            if (NightMarketManager.Instance == null) return;
            int currentCash = RestaurantEconomyManager.Instance != null ? RestaurantEconomyManager.Instance.CurrentBalance : 0;

            foreach (var card in _activeCards)
            {
                bool owned = NightMarketManager.Instance.HasUpgrade(card.type);
                var info = NightMarketManager.Instance.GetUpgradeInfo(card.type);

                if (owned)
                {
                    if (card.purchaseButton != null) card.purchaseButton.interactable = false;
                    if (card.buttonLabel != null) card.buttonLabel.text = "✓ UNLOCKED";
                    if (card.costText != null)
                    {
                        card.costText.text = "OWNED";
                        card.costText.color = ownedColor;
                    }
                }
                else
                {
                    bool canAfford = currentCash >= info.cost;
                    if (card.purchaseButton != null) card.purchaseButton.interactable = canAfford;
                    if (card.buttonLabel != null) card.buttonLabel.text = canAfford ? "BUY UPGRADE" : "NOT ENOUGH CASH";
                    if (card.costText != null)
                    {
                        card.costText.text = $"${info.cost}";
                        card.costText.color = canAfford ? affordableColor : unaffordableColor;
                    }
                }
            }
        }

        private void OnBuyClicked(NightMarketManager.UpgradeType type)
        {
            if (NightMarketManager.Instance != null)
            {
                NightMarketManager.Instance.RequestPurchaseUpgradeRpc(type);
            }
        }

        private void OnStartNextDayClicked()
        {
            if (DayLoopManager.Instance != null)
            {
                DayLoopManager.Instance.RequestStartNextDayRpc();
            }
        }

        public void Show()
        {
            if (shopPanel != null) shopPanel.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour {


    [Header("Headers & Details")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subDetailsText;

    [Header("Statistics Grid")]
    [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private TextMeshProUGUI financialStatsText;
    [SerializeField] private TextMeshProUGUI reputationStatsText;

    [Header("Controls")]
    [SerializeField] private Button rematchButton;
    [SerializeField] private TextMeshProUGUI rematchButtonText;
    [SerializeField] private TextMeshProUGUI rematchStatusText;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button playAgainButton; // Legacy fallback


    private void Awake() {
        if (rematchButton != null) {
            rematchButton.onClick.AddListener(() => {
                KitchenGameManager.Instance.ToggleLocalPlayerRematchVote();
                UpdateRematchVisuals();
            });
        }

        if (mainMenuButton != null) {
            mainMenuButton.onClick.AddListener(() => {
                KitchenGameManager.Instance.LeaveGameSession();
            });
        }

        if (playAgainButton != null) {
            playAgainButton.onClick.AddListener(() => {
                KitchenGameManager.Instance.LeaveGameSession();
            });
        }
    }

    private void Start() {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        KitchenGameManager.Instance.OnRematchVotesChanged += KitchenGameManager_OnRematchVotesChanged;

        Hide();
    }

    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e) {
        if (KitchenGameManager.Instance.IsGameOver()) {
            Show();
            UpdateStatsDisplay();
            UpdateRematchVisuals();
        } else {
            Hide();
        }
    }

    private void KitchenGameManager_OnRematchVotesChanged(object sender, EventArgs e) {
        if (gameObject.activeSelf) {
            UpdateRematchVisuals();
        }
    }

    private void UpdateStatsDisplay() {
        bool isBankrupt = KitchenGameManager.Instance.IsBankruptcyGameOver();

        // 1. Title & Subtitle
        if (titleText != null) {
            titleText.text = isBankrupt
                ? "<color=#FF4444>RESTAURANT BANKRUPT</color>"
                : "<color=#FFB703>DAY COMPLETE</color>";
        }

        // 2. Delivered orders count
        int successfulOrders = DeliveryManager.Instance != null ? DeliveryManager.Instance.GetSuccessfulRecipesAmount() : 0;
        if (recipesDeliveredText != null) {
            recipesDeliveredText.text = successfulOrders.ToString();
        }

        // 3. Economy & Survival Stats
        if (RestaurantEconomyManager.Instance != null && KitchenGameMultiplayer.tableServiceMode) {
            int balance = RestaurantEconomyManager.Instance.GetCurrentBalance();
            int cycle = RestaurantEconomyManager.Instance.GetCurrentBillCycle();
            float rating = RestaurantEconomyManager.Instance.GetStarRating();
            int reviews = RestaurantEconomyManager.Instance.GetTotalReviews();

            if (subDetailsText != null) {
                subDetailsText.text = isBankrupt
                    ? $"Could not afford the rent! Survived {cycle - 1} rent cycles."
                    : $"Kitchen survived {cycle} rent cycles successfully!";
            }

            if (financialStatsText != null) {
                financialStatsText.text = balance >= 0
                    ? $"FINAL BALANCE: <color=#00E5FF>${balance}</color>"
                    : $"FINAL DEBT: <color=#FF4444>-${Mathf.Abs(balance)}</color>";
            }

            if (reputationStatsText != null) {
                reputationStatsText.text = $"REPUTATION: <color=#FFD700>{rating:F1} / 5.0</color> ({reviews} REVIEWS)";
            }
        } else {
            if (subDetailsText != null) subDetailsText.text = "Kitchen shift completed!";
            if (financialStatsText != null) financialStatsText.text = "";
            if (reputationStatsText != null) reputationStatsText.text = "";
        }
    }

    private void UpdateRematchVisuals() {
        bool hasVoted = KitchenGameManager.Instance.HasLocalPlayerVotedRematch();
        int votes = KitchenGameManager.Instance.GetRematchVotesCount();
        int total = KitchenGameManager.Instance.GetTotalPlayersCount();

        if (rematchButtonText != null) {
            rematchButtonText.text = hasVoted ? "READY FOR REMATCH [WAITING]" : "REMATCH / PLAY AGAIN";
        }

        if (rematchStatusText != null) {
            if (total > 1) {
                rematchStatusText.text = $"VOTES: {votes} / {total} PLAYERS READY";
            } else {
                rematchStatusText.text = hasVoted ? "RESTARTING GAME..." : "PRESS REMATCH TO RESTART";
            }
        }
    }

    private void Show() {
        gameObject.SetActive(true);
        if (rematchButton != null) {
            rematchButton.Select();
        } else if (playAgainButton != null) {
            playAgainButton.Select();
        }
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        if (KitchenGameManager.Instance != null) {
            KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
            KitchenGameManager.Instance.OnRematchVotesChanged -= KitchenGameManager_OnRematchVotesChanged;
        }
    }

}
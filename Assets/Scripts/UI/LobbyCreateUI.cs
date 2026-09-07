using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour {


    [Header("Core Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button createPublicButton;
    [SerializeField] private Button createPrivateButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;

    [Header("Game Mode Card Selection (New)")]
    [SerializeField] private Button tableServiceModeButton;
    [SerializeField] private Button classicModeButton;
    [SerializeField] private Image tableServiceCardImage;
    [SerializeField] private Image classicCardImage;
    [SerializeField] private GameObject tableServiceActiveIndicator;
    [SerializeField] private GameObject classicActiveIndicator;
    [SerializeField] private TextMeshProUGUI modeDescriptionText;
    [SerializeField] private Sprite tableServiceActiveSprite;
    [SerializeField] private Sprite classicActiveSprite;
    [SerializeField] private Sprite defaultCardSprite;

    [Header("Legacy Fallback Controls")]
    [SerializeField] private Button gameModeToggleButton;
    [SerializeField] private TextMeshProUGUI gameModeToggleText;

    private readonly Color activeCardColor = new Color(1f, 1f, 1f, 1f);
    private readonly Color inactiveCardColor = new Color(0.65f, 0.7f, 0.8f, 0.75f);


    private void Awake() {
        createPublicButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.CreateLobby(lobbyNameInputField.text, false);
        });
        createPrivateButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.CreateLobby(lobbyNameInputField.text, true);
        });
        closeButton.onClick.AddListener(() => {
            Hide();
        });

        if (tableServiceModeButton != null) {
            tableServiceModeButton.onClick.AddListener(() => {
                SetGameMode(true);
            });
        }

        if (classicModeButton != null) {
            classicModeButton.onClick.AddListener(() => {
                SetGameMode(false);
            });
        }

        if (gameModeToggleButton != null) {
            gameModeToggleButton.onClick.AddListener(() => {
                SetGameMode(!KitchenGameMultiplayer.tableServiceMode);
            });
        }
    }

    private void Start() {
        Hide();
    }

    public void Show() {
        gameObject.SetActive(true);
        RefreshModeVisuals();
        createPublicButton.Select();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    public void SetGameMode(bool isTableService) {
        KitchenGameMultiplayer.tableServiceMode = isTableService;
        RefreshModeVisuals();
    }

    private void RefreshModeVisuals() {
        bool isTableService = KitchenGameMultiplayer.tableServiceMode;

        // Update card active indicators
        if (tableServiceActiveIndicator != null) {
            tableServiceActiveIndicator.SetActive(isTableService);
        }
        if (classicActiveIndicator != null) {
            classicActiveIndicator.SetActive(!isTableService);
        }

        // Update card background colors and sprites
        if (tableServiceCardImage != null) {
            if (tableServiceActiveSprite != null && defaultCardSprite != null) {
                tableServiceCardImage.sprite = isTableService ? tableServiceActiveSprite : defaultCardSprite;
            }
            tableServiceCardImage.color = isTableService ? activeCardColor : inactiveCardColor;
        }
        if (classicCardImage != null) {
            if (classicActiveSprite != null && defaultCardSprite != null) {
                classicCardImage.sprite = !isTableService ? classicActiveSprite : defaultCardSprite;
            }
            classicCardImage.color = !isTableService ? activeCardColor : inactiveCardColor;
        }

        // Update mode description text
        if (modeDescriptionText != null) {
            modeDescriptionText.text = isTableService
                ? "<color=#FFD700>Table Service & City Deliveries</color>\n<size=80%>Dine-in customer tables, scooter delivery runs, kitchen crises, and customer tips.</size>"
                : "<color=#00E5FF>Classic Kitchen Rush</color>\n<size=80%>Pure fast-paced cooking rush. Prep orders and deliver directly to the kitchen counter window.</size>";
        }

        // Legacy toggle text fallback
        if (gameModeToggleText != null) {
            gameModeToggleText.text = isTableService
                ? "Mode: Table Service & Deliveries"
                : "Mode: Classic Kitchen";
        }
    }

}
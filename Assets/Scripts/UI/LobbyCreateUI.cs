using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour {


    [SerializeField] private Button closeButton;
    [SerializeField] private Button createPublicButton;
    [SerializeField] private Button createPrivateButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;

    // Wire up in Inspector: a Button whose child Text shows the current mode
    [SerializeField] private Button gameModeToggleButton;
    [SerializeField] private TextMeshProUGUI gameModeToggleText;



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

        if (gameModeToggleButton != null) {
            gameModeToggleButton.onClick.AddListener(() => {
                KitchenGameMultiplayer.tableServiceMode = !KitchenGameMultiplayer.tableServiceMode;
                RefreshModeText();
            });
        }
    }

    private void Start() {
        Hide();
    }

    public void Show() {
        gameObject.SetActive(true);
        RefreshModeText();
        createPublicButton.Select();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void RefreshModeText() {
        if (gameModeToggleText != null) {
            gameModeToggleText.text = KitchenGameMultiplayer.tableServiceMode
                ? "Mode: Table Service"
                : "Mode: Classic";
        }
    }

}
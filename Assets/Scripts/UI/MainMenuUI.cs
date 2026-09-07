using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {


    [SerializeField] private Button playMultiplayerButton;
    [SerializeField] private Button playSingleplayerButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button gameModeToggleButton;
    [SerializeField] private TextMeshProUGUI gameModeToggleText;
    [SerializeField] private OptionsUI optionsUI;


    private void Awake() {
        playMultiplayerButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.playMultiplayer = true;
            Loader.Load(Loader.Scene.LobbyScene);
        });
        playSingleplayerButton.onClick.AddListener(() => {
            KitchenGameMultiplayer.playMultiplayer = false;
            Loader.Load(Loader.Scene.LobbyScene);
        });

        if (optionsButton != null) {
            optionsButton.onClick.AddListener(() => {
                Hide();
                if (optionsUI != null) {
                    optionsUI.Show(Show);
                } else if (OptionsUI.Instance != null) {
                    OptionsUI.Instance.Show(Show);
                }
            });
        }

        if (quitButton != null) {
            quitButton.onClick.AddListener(() => {
                Application.Quit();
            });
        }

        if (gameModeToggleButton != null) {
            gameModeToggleButton.onClick.AddListener(() => {
                KitchenGameMultiplayer.tableServiceMode = !KitchenGameMultiplayer.tableServiceMode;
                UpdateModeText();
            });
            UpdateModeText();
        }

        Time.timeScale = 1f;
    }

    public void Show() {
        gameObject.SetActive(true);
        if (playMultiplayerButton != null) playMultiplayerButton.Select();
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    private void UpdateModeText() {
        if (gameModeToggleText != null) {
            gameModeToggleText.text = KitchenGameMultiplayer.tableServiceMode ? "Mode: Table Service" : "Mode: Classic";
        }
    }

}
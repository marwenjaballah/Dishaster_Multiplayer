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

        if (quitButton != null) {
            quitButton.onClick.AddListener(() => {
                Application.Quit();
            });
        }

        EnsureOptionsButton();

        if (gameModeToggleButton != null) {
            gameModeToggleButton.onClick.AddListener(() => {
                KitchenGameMultiplayer.tableServiceMode = !KitchenGameMultiplayer.tableServiceMode;
                UpdateModeText();
            });
            UpdateModeText();
        }

        Time.timeScale = 1f;
    }

    private void EnsureOptionsButton() {
        if (optionsButton == null && quitButton != null) {
            GameObject optBtnObj = Instantiate(quitButton.gameObject, quitButton.transform.parent);
            optBtnObj.name = "OptionsButton";
            optBtnObj.transform.SetSiblingIndex(quitButton.transform.GetSiblingIndex());

            optionsButton = optBtnObj.GetComponent<Button>();
            TextMeshProUGUI btnText = optBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) {
                btnText.text = "SETTINGS";
            }
        }

        if (optionsButton != null) {
            optionsButton.onClick.AddListener(() => {
                ShowOptions();
            });
        }
    }

    private void ShowOptions() {
        if (OptionsUI.Instance != null) {
            Hide();
            OptionsUI.Instance.Show(Show);
        } else if (optionsUI != null) {
            Hide();
            optionsUI.Show(Show);
        } else {
            OptionsUI existing = FindObjectOfType<OptionsUI>(true);
            if (existing != null) {
                Hide();
                existing.Show(Show);
            } else {
                GameObject optionsObj = new GameObject("OptionsUI", typeof(RectTransform), typeof(OptionsUI));
                optionsObj.transform.SetParent(transform.parent, false);
                OptionsUI opt = optionsObj.GetComponent<OptionsUI>();
                Hide();
                opt.Show(Show);
            }
        }
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
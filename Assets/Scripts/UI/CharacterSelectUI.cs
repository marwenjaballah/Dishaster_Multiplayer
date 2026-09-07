using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour {


    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private TextMeshProUGUI copyCodeButtonText;


    private void Awake() {
        mainMenuButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.LeaveLobby();
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        readyButton.onClick.AddListener(() => {
            CharacterSelectReady.Instance.SetPlayerReady();
            UpdateReadyState();
        });

        if (copyCodeButton != null) {
            copyCodeButton.onClick.AddListener(() => {
                Lobby lobby = KitchenGameLobby.Instance.GetLobby();
                if (lobby != null && !string.IsNullOrEmpty(lobby.LobbyCode)) {
                    GUIUtility.systemCopyBuffer = lobby.LobbyCode;
                    StartCoroutine(ShowCopiedFeedback());
                }
            });
        }
    }

    private void Start() {
        Lobby lobby = KitchenGameLobby.Instance.GetLobby();

        if (lobby != null) {
            if (lobbyNameText != null) lobbyNameText.text = "Lobby: " + lobby.Name;
            if (lobbyCodeText != null) lobbyCodeText.text = "Code: " + lobby.LobbyCode;
        } else {
            if (lobbyNameText != null) lobbyNameText.text = "Lobby: Local Host";
            if (lobbyCodeText != null) lobbyCodeText.text = "Code: ---";
        }

        if (gameModeText != null) {
            gameModeText.text = KitchenGameMultiplayer.tableServiceMode ? "Mode: TABLE SERVICE" : "Mode: CLASSIC";
        }

        if (CharacterSelectReady.Instance != null) {
            CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReady_OnReadyChanged;
        }

        UpdateReadyState();
    }

    private void CharacterSelectReady_OnReadyChanged(object sender, System.EventArgs e) {
        UpdateReadyState();
    }

    private void UpdateReadyState() {
        if (CharacterSelectReady.Instance != null && NetworkManager.Singleton != null) {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            bool isReady = CharacterSelectReady.Instance.IsPlayerReady(localClientId);

            if (readyButtonText != null) {
                if (isReady) {
                    readyButtonText.text = "<color=#00F5D4>READY</color> <size=70%>(WAITING...)</size>";
                } else {
                    readyButtonText.text = "READY UP";
                }
            }

            readyButton.interactable = !isReady;
        }
    }

    private IEnumerator ShowCopiedFeedback() {
        if (copyCodeButtonText != null) {
            string originalText = copyCodeButtonText.text;
            copyCodeButtonText.text = "<color=#00F5D4>COPIED!</color>";
            yield return new WaitForSecondsRealtime(1.5f);
            copyCodeButtonText.text = originalText;
        }
    }

    private void OnDestroy() {
        if (CharacterSelectReady.Instance != null) {
            CharacterSelectReady.Instance.OnReadyChanged -= CharacterSelectReady_OnReadyChanged;
        }
    }
}
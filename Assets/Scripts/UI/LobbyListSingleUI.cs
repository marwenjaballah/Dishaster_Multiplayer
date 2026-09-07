using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListSingleUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI lobbyNameText;


    private Lobby lobby;


    private void Awake() {
        GetComponent<Button>().onClick.AddListener(() => {
            KitchenGameLobby.Instance.JoinWithId(lobby.Id);
        });
    }

    public void SetLobby(Lobby lobby) {
        this.lobby = lobby;
        string modeBadge = "";
        if (lobby.Data != null && lobby.Data.ContainsKey(KitchenGameLobby.KEY_GAME_MODE)) {
            string mode = lobby.Data[KitchenGameLobby.KEY_GAME_MODE].Value;
            if (mode == "Table Service") {
                modeBadge = " <color=#FFD700><size=80%>[Table Service]</size></color>";
            } else {
                modeBadge = " <color=#00E5FF><size=80%>[Classic]</size></color>";
            }
        }
        lobbyNameText.text = lobby.Name + modeBadge;
    }

}
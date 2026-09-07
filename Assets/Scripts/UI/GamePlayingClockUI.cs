using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayingClockUI : MonoBehaviour {


    [SerializeField] private Image timerImage;


    private void Start() {
        if (KitchenGameMultiplayer.tableServiceMode) {
            gameObject.SetActive(false);
        }
    }

    private void Update() {
        if (timerImage != null && KitchenGameManager.Instance != null) {
            timerImage.fillAmount = KitchenGameManager.Instance.GetGamePlayingTimerNormalized();
        }
    }
}
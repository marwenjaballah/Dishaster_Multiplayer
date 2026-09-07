using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour {


    [SerializeField] private TextMeshProUGUI keyMoveUpText;
    [SerializeField] private TextMeshProUGUI keyMoveDownText;
    [SerializeField] private TextMeshProUGUI keyMoveLeftText;
    [SerializeField] private TextMeshProUGUI keyMoveRightText;
    [SerializeField] private TextMeshProUGUI keyInteractText;
    [SerializeField] private TextMeshProUGUI keyInteractAlternateText;
    [SerializeField] private TextMeshProUGUI keyPauseText;
    [SerializeField] private TextMeshProUGUI keyGamepadInteractText;
    [SerializeField] private TextMeshProUGUI keyGamepadInteractAlternateText;
    [SerializeField] private TextMeshProUGUI keyGamepadPauseText;
    [SerializeField] private TextMeshProUGUI pressInteractToStartText;


    private void Start() {
        if (GameInput.Instance != null) {
            GameInput.Instance.OnBindingRebind += GameInput_OnBindingRebind;
        }
        if (KitchenGameManager.Instance != null) {
            KitchenGameManager.Instance.OnLocalPlayerReadyChanged += KitchenGameManager_OnLocalPlayerReadyChanged;
        }

        UpdateVisual();

        Show();
    }

    private void KitchenGameManager_OnLocalPlayerReadyChanged(object sender, System.EventArgs e) {
        if (KitchenGameManager.Instance != null && KitchenGameManager.Instance.IsLocalPlayerReady()) {
            Hide();
        }
    }

    private void GameInput_OnBindingRebind(object sender, System.EventArgs e) {
        UpdateVisual();
    }

    private void UpdateVisual() {
        if (GameInput.Instance != null) {
            if (keyMoveUpText != null) keyMoveUpText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Up);
            if (keyMoveDownText != null) keyMoveDownText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Down);
            if (keyMoveLeftText != null) keyMoveLeftText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Left);
            if (keyMoveRightText != null) keyMoveRightText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Right);
            if (keyInteractText != null) keyInteractText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Interact);
            if (keyInteractAlternateText != null) keyInteractAlternateText.text = GameInput.Instance.GetBindingText(GameInput.Binding.InteractAlternate);
            if (keyPauseText != null) keyPauseText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Pause);
            if (keyGamepadInteractText != null) keyGamepadInteractText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Interact);
            if (keyGamepadInteractAlternateText != null) keyGamepadInteractAlternateText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_InteractAlternate);
            if (keyGamepadPauseText != null) keyGamepadPauseText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Pause);

            if (pressInteractToStartText != null) {
                string interactKey = GameInput.Instance.GetBindingText(GameInput.Binding.Interact);
                string gamepadKey = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Interact);
                pressInteractToStartText.text = $"PRESS <color=#FFB703>[{interactKey}]</color> OR <color=#00F5D4>[{gamepadKey}]</color> TO START";
            }
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
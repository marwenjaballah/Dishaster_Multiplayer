using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour {


    public static OptionsUI Instance { get; private set; }


    [Header("Volume Controls")]
    [SerializeField] private Slider soundEffectsSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button soundEffectsButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private TextMeshProUGUI soundEffectsText;
    [SerializeField] private TextMeshProUGUI musicText;

    [Header("Keybinding Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button moveUpButton;
    [SerializeField] private Button moveDownButton;
    [SerializeField] private Button moveLeftButton;
    [SerializeField] private Button moveRightButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button interactAlternateButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button gamepadInteractButton;
    [SerializeField] private Button gamepadInteractAlternateButton;
    [SerializeField] private Button gamepadPauseButton;
    [SerializeField] private TextMeshProUGUI moveUpText;
    [SerializeField] private TextMeshProUGUI moveDownText;
    [SerializeField] private TextMeshProUGUI moveLeftText;
    [SerializeField] private TextMeshProUGUI moveRightText;
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private TextMeshProUGUI interactAlternateText;
    [SerializeField] private TextMeshProUGUI pauseText;
    [SerializeField] private TextMeshProUGUI gamepadInteractText;
    [SerializeField] private TextMeshProUGUI gamepadInteractAlternateText;
    [SerializeField] private TextMeshProUGUI gamepadPauseText;
    [SerializeField] private Transform pressToRebindKeyTransform;


    private Action onCloseButtonAction;


    private void Awake() {
        Instance = this;

        if (soundEffectsSlider != null) {
            soundEffectsSlider.onValueChanged.AddListener((float val) => {
                SoundManager.Instance.SetVolume(val);
                UpdateVisual();
            });
        }
        if (musicSlider != null) {
            musicSlider.onValueChanged.AddListener((float val) => {
                MusicManager.Instance.SetVolume(val);
                UpdateVisual();
            });
        }

        if (soundEffectsButton != null) {
            soundEffectsButton.onClick.AddListener(() => {
                SoundManager.Instance.ChangeVolume();
                UpdateVisual();
            });
        }
        if (musicButton != null) {
            musicButton.onClick.AddListener(() => {
                MusicManager.Instance.ChangeVolume();
                UpdateVisual();
            });
        }
        closeButton.onClick.AddListener(() => {
            Hide();
            onCloseButtonAction?.Invoke();
        });

        moveUpButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Up); });
        moveDownButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Down); });
        moveLeftButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Left); });
        moveRightButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Right); });
        interactButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Interact); });
        interactAlternateButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.InteractAlternate); });
        pauseButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Pause); });
        gamepadInteractButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Gamepad_Interact); });
        gamepadInteractAlternateButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Gamepad_InteractAlternate); });
        gamepadPauseButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Gamepad_Pause); });
    }

    private void Start() {
        KitchenGameManager.Instance.OnLocalGameUnpaused += KitchenGameManager_OnGameUnpaused;

        UpdateVisual();

        HidePressToRebindKey();
        Hide();
    }

    private void KitchenGameManager_OnGameUnpaused(object sender, System.EventArgs e) {
        Hide();
    }

    private void UpdateVisual() {
        float sfxVol = SoundManager.Instance.GetVolume();
        float musVol = MusicManager.Instance.GetVolume();

        if (soundEffectsSlider != null) soundEffectsSlider.SetValueWithoutNotify(sfxVol);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(musVol);

        if (soundEffectsText != null) soundEffectsText.text = "SFX: " + Mathf.RoundToInt(sfxVol * 100f) + "%";
        if (musicText != null) musicText.text = "MUSIC: " + Mathf.RoundToInt(musVol * 100f) + "%";

        moveUpText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Up);
        moveDownText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Down);
        moveLeftText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Left);
        moveRightText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Right);
        interactText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Interact);
        interactAlternateText.text = GameInput.Instance.GetBindingText(GameInput.Binding.InteractAlternate);
        pauseText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Pause);
        gamepadInteractText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Interact);
        gamepadInteractAlternateText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_InteractAlternate);
        gamepadPauseText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Pause);
    }

    public void Show(Action onCloseButtonAction) {
        this.onCloseButtonAction = onCloseButtonAction;

        gameObject.SetActive(true);

        soundEffectsButton.Select();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void ShowPressToRebindKey() {
        pressToRebindKeyTransform.gameObject.SetActive(true);
    }

    private void HidePressToRebindKey() {
        pressToRebindKeyTransform.gameObject.SetActive(false);
    }

    private void RebindBinding(GameInput.Binding binding) {
        ShowPressToRebindKey();
        GameInput.Instance.RebindBinding(binding, () => {
            HidePressToRebindKey();
            UpdateVisual();
        });
    }

}

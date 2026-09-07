using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour {


    public static OptionsUI Instance { get; private set; }

    private const string PLAYER_PREFS_SOUND_EFFECTS_VOLUME = "SoundEffectsVolume";
    private const string PLAYER_PREFS_MUSIC_VOLUME = "MusicVolume";


    [Header("Volume Controls")]
    [SerializeField] private Slider soundEffectsSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button soundEffectsButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private TextMeshProUGUI soundEffectsText;
    [SerializeField] private TextMeshProUGUI musicText;

    [Header("Display & Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowModeDropdown;
    [SerializeField] private Button resolutionButton;
    [SerializeField] private Button windowModeButton;
    [SerializeField] private TextMeshProUGUI resolutionText;
    [SerializeField] private TextMeshProUGUI windowModeText;

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
    private bool isShowing;


    private void Awake() {
        Instance = this;

        if (soundEffectsSlider != null) {
            soundEffectsSlider.onValueChanged.AddListener((float val) => {
                SetSfxVolume(val);
            });
        }
        if (musicSlider != null) {
            musicSlider.onValueChanged.AddListener((float val) => {
                SetMusicVolume(val);
            });
        }

        if (soundEffectsButton != null) {
            soundEffectsButton.onClick.AddListener(() => {
                float v = GetSfxVolume() + 0.1f;
                if (v > 1.05f) v = 0f;
                SetSfxVolume(v);
                if (soundEffectsSlider != null) soundEffectsSlider.SetValueWithoutNotify(v);
            });
        }
        if (musicButton != null) {
            musicButton.onClick.AddListener(() => {
                float v = GetMusicVolume() + 0.1f;
                if (v > 1.05f) v = 0f;
                SetMusicVolume(v);
                if (musicSlider != null) musicSlider.SetValueWithoutNotify(v);
            });
        }

        // Resolution Dropdown
        if (resolutionDropdown != null) {
            SetupResolutionDropdown();
        }

        // Window Mode Dropdown
        if (windowModeDropdown != null) {
            SetupWindowModeDropdown();
        }

        // Resolution and Window Mode Button fallback (if present)
        if (resolutionButton != null) {
            resolutionButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleResolution(1);
            });
        }
        if (windowModeButton != null) {
            windowModeButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleWindowMode(1);
            });
        }

        if (closeButton != null) {
            closeButton.onClick.AddListener(() => {
                Hide();
                onCloseButtonAction?.Invoke();
            });
        }

        if (moveUpButton != null) moveUpButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Up); });
        if (moveDownButton != null) moveDownButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Down); });
        if (moveLeftButton != null) moveLeftButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Left); });
        if (moveRightButton != null) moveRightButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Move_Right); });
        if (interactButton != null) interactButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Interact); });
        if (interactAlternateButton != null) interactAlternateButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.InteractAlternate); });
        if (pauseButton != null) pauseButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Pause); });
        if (gamepadInteractButton != null) gamepadInteractButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Gamepad_Interact); });
        if (gamepadInteractAlternateButton != null) gamepadInteractAlternateButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Gamepad_InteractAlternate); });
        if (gamepadPauseButton != null) gamepadPauseButton.onClick.AddListener(() => { RebindBinding(GameInput.Binding.Gamepad_Pause); });
    }

    private void Start() {
        if (KitchenGameManager.Instance != null) {
            KitchenGameManager.Instance.OnLocalGameUnpaused += KitchenGameManager_OnGameUnpaused;
        }

        if (DisplaySettingsManager.Instance != null) {
            DisplaySettingsManager.Instance.OnDisplaySettingsChanged += DisplaySettingsManager_OnDisplaySettingsChanged;
        }

        SetupResolutionDropdown();
        SetupWindowModeDropdown();
        UpdateVisual();

        HidePressToRebindKey();
        if (!isShowing) {
            Hide();
        }
    }

    private void DisplaySettingsManager_OnDisplaySettingsChanged(object sender, EventArgs e) {
        UpdateVisual();
    }

    private float GetSfxVolume() {
        if (SoundManager.Instance != null) return SoundManager.Instance.GetVolume();
        return PlayerPrefs.GetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, 1f);
    }

    private float GetMusicVolume() {
        if (MusicManager.Instance != null) return MusicManager.Instance.GetVolume();
        return PlayerPrefs.GetFloat(PLAYER_PREFS_MUSIC_VOLUME, 0.3f);
    }

    private void SetSfxVolume(float val) {
        val = Mathf.Clamp01(val);
        if (SoundManager.Instance != null) {
            SoundManager.Instance.SetVolume(val);
        } else {
            PlayerPrefs.SetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, val);
            PlayerPrefs.Save();
        }
        if (soundEffectsText != null) {
            soundEffectsText.text = "SFX: " + Mathf.RoundToInt(val * 100f) + "%";
        }
    }

    private void SetMusicVolume(float val) {
        val = Mathf.Clamp01(val);
        if (MusicManager.Instance != null) {
            MusicManager.Instance.SetVolume(val);
        } else {
            PlayerPrefs.SetFloat(PLAYER_PREFS_MUSIC_VOLUME, val);
            PlayerPrefs.Save();
        }
        if (musicText != null) {
            musicText.text = "MUSIC: " + Mathf.RoundToInt(val * 100f) + "%";
        }
    }

    private void SetupResolutionDropdown() {
        if (resolutionDropdown == null || DisplaySettingsManager.Instance == null) return;
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var res in DisplaySettingsManager.Instance.GetResolutions()) {
            options.Add(res.ToString());
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(DisplaySettingsManager.Instance.GetCurrentResolutionIndex());
        resolutionDropdown.onValueChanged.AddListener((int index) => {
            DisplaySettingsManager.Instance.SetResolutionIndex(index);
        });
    }

    private void SetupWindowModeDropdown() {
        if (windowModeDropdown == null || DisplaySettingsManager.Instance == null) return;
        windowModeDropdown.onValueChanged.RemoveAllListeners();
        windowModeDropdown.ClearOptions();
        List<string> options = new List<string> {
            DisplaySettingsManager.Instance.GetWindowModeText(DisplaySettingsManager.WindowMode.ExclusiveFullScreen),
            DisplaySettingsManager.Instance.GetWindowModeText(DisplaySettingsManager.WindowMode.BorderlessWindow),
            DisplaySettingsManager.Instance.GetWindowModeText(DisplaySettingsManager.WindowMode.Windowed)
        };
        windowModeDropdown.AddOptions(options);
        windowModeDropdown.SetValueWithoutNotify((int)DisplaySettingsManager.Instance.GetCurrentWindowMode());
        windowModeDropdown.onValueChanged.AddListener((int index) => {
            DisplaySettingsManager.Instance.SetWindowMode((DisplaySettingsManager.WindowMode)index);
        });
    }

    private void KitchenGameManager_OnGameUnpaused(object sender, System.EventArgs e) {
        Hide();
    }

    private void UpdateVisual() {
        float sfxVol = GetSfxVolume();
        float musVol = GetMusicVolume();

        if (soundEffectsSlider != null) soundEffectsSlider.SetValueWithoutNotify(sfxVol);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(musVol);

        if (soundEffectsText != null) soundEffectsText.text = "SFX: " + Mathf.RoundToInt(sfxVol * 100f) + "%";
        if (musicText != null) musicText.text = "MUSIC: " + Mathf.RoundToInt(musVol * 100f) + "%";

        if (DisplaySettingsManager.Instance != null) {
            if (resolutionDropdown != null) {
                resolutionDropdown.SetValueWithoutNotify(DisplaySettingsManager.Instance.GetCurrentResolutionIndex());
            }
            if (windowModeDropdown != null) {
                windowModeDropdown.SetValueWithoutNotify((int)DisplaySettingsManager.Instance.GetCurrentWindowMode());
            }
            if (resolutionText != null) {
                resolutionText.text = "RES: " + DisplaySettingsManager.Instance.GetCurrentResolutionText();
            }
            if (windowModeText != null) {
                windowModeText.text = "MODE: " + DisplaySettingsManager.Instance.GetCurrentWindowModeText();
            }
        }

        if (GameInput.Instance != null) {
            if (moveUpText != null) moveUpText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Up);
            if (moveDownText != null) moveDownText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Down);
            if (moveLeftText != null) moveLeftText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Left);
            if (moveRightText != null) moveRightText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Move_Right);
            if (interactText != null) interactText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Interact);
            if (interactAlternateText != null) interactAlternateText.text = GameInput.Instance.GetBindingText(GameInput.Binding.InteractAlternate);
            if (pauseText != null) pauseText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Pause);
            if (gamepadInteractText != null) gamepadInteractText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Interact);
            if (gamepadInteractAlternateText != null) gamepadInteractAlternateText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_InteractAlternate);
            if (gamepadPauseText != null) gamepadPauseText.text = GameInput.Instance.GetBindingText(GameInput.Binding.Gamepad_Pause);
        }
    }

    public void Show(Action onCloseButtonAction) {
        this.onCloseButtonAction = onCloseButtonAction;
        isShowing = true;

        gameObject.SetActive(true);

        HidePressToRebindKey();
        SetupResolutionDropdown();
        SetupWindowModeDropdown();
        UpdateVisual();

        if (closeButton != null) {
            closeButton.Select();
        }
    }

    public void Hide() {
        isShowing = false;
        HidePressToRebindKey();
        gameObject.SetActive(false);
    }

    private void ShowPressToRebindKey() {
        if (pressToRebindKeyTransform != null) {
            pressToRebindKeyTransform.gameObject.SetActive(true);
        }
    }

    private void HidePressToRebindKey() {
        if (pressToRebindKeyTransform != null) {
            pressToRebindKeyTransform.gameObject.SetActive(false);
        }
    }

    private void RebindBinding(GameInput.Binding binding) {
        if (GameInput.Instance != null) {
            ShowPressToRebindKey();
            GameInput.Instance.RebindBinding(binding, () => {
                HidePressToRebindKey();
                UpdateVisual();
            });
        } else {
            HidePressToRebindKey();
        }
    }

    private void OnDestroy() {
        if (DisplaySettingsManager.Instance != null) {
            DisplaySettingsManager.Instance.OnDisplaySettingsChanged -= DisplaySettingsManager_OnDisplaySettingsChanged;
        }
    }
}

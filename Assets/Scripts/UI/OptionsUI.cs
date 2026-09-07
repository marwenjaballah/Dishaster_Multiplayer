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

    [Header("Display & Resolution")]
    [SerializeField] private Button resolutionButton;
    [SerializeField] private Button resolutionPrevButton;
    [SerializeField] private Button resolutionNextButton;
    [SerializeField] private TextMeshProUGUI resolutionText;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [SerializeField] private Button windowModeButton;
    [SerializeField] private Button windowModePrevButton;
    [SerializeField] private Button windowModeNextButton;
    [SerializeField] private TextMeshProUGUI windowModeText;
    [SerializeField] private TMP_Dropdown windowModeDropdown;

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

        EnsureDisplayControls();

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

        // Resolution Controls
        if (resolutionButton != null) {
            resolutionButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleResolution(1);
            });
        }
        if (resolutionNextButton != null) {
            resolutionNextButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleResolution(1);
            });
        }
        if (resolutionPrevButton != null) {
            resolutionPrevButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleResolution(-1);
            });
        }
        if (resolutionDropdown != null) {
            SetupResolutionDropdown();
        }

        // Window Mode Controls
        if (windowModeButton != null) {
            windowModeButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleWindowMode(1);
            });
        }
        if (windowModeNextButton != null) {
            windowModeNextButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleWindowMode(1);
            });
        }
        if (windowModePrevButton != null) {
            windowModePrevButton.onClick.AddListener(() => {
                if (DisplaySettingsManager.Instance != null) DisplaySettingsManager.Instance.CycleWindowMode(-1);
            });
        }
        if (windowModeDropdown != null) {
            SetupWindowModeDropdown();
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

        UpdateVisual();

        HidePressToRebindKey();
        Hide();
    }

    private void EnsureDisplayControls() {
        if (resolutionButton == null && resolutionDropdown == null) {
            resolutionButton = CreateSettingsButton("ResolutionButton", new Vector2(0, 48), "RES: " + (DisplaySettingsManager.Instance != null ? DisplaySettingsManager.Instance.GetCurrentResolutionText() : "1920 x 1080"), out resolutionText);
        }

        if (windowModeButton == null && windowModeDropdown == null) {
            windowModeButton = CreateSettingsButton("WindowModeButton", new Vector2(0, 0), "MODE: " + (DisplaySettingsManager.Instance != null ? DisplaySettingsManager.Instance.GetCurrentWindowModeText() : "BORDERLESS WINDOW"), out windowModeText);
        }
    }

    private Button CreateSettingsButton(string name, Vector2 anchoredPos, string defaultText, out TextMeshProUGUI textComp) {
        textComp = null;
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(transform, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(460, 42);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.2f, 0.85f, 1f, 1f);
        colors.pressedColor = new Color(0.15f, 0.7f, 0.85f, 1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        textComp = textObj.GetComponent<TextMeshProUGUI>();
        textComp.text = defaultText;
        textComp.fontSize = 20;
        textComp.fontStyle = FontStyles.Bold;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = Color.white;

        if (soundEffectsText != null && soundEffectsText.font != null) {
            textComp.font = soundEffectsText.font;
        }

        return btn;
    }

    private void DisplaySettingsManager_OnDisplaySettingsChanged(object sender, EventArgs e) {
        UpdateVisual();
    }

    private void SetupResolutionDropdown() {
        if (DisplaySettingsManager.Instance == null) return;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var res in DisplaySettingsManager.Instance.GetResolutions()) {
            options.Add(res.ToString());
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = DisplaySettingsManager.Instance.GetCurrentResolutionIndex();
        resolutionDropdown.onValueChanged.AddListener((int index) => {
            DisplaySettingsManager.Instance.SetResolutionIndex(index);
        });
    }

    private void SetupWindowModeDropdown() {
        if (DisplaySettingsManager.Instance == null) return;
        windowModeDropdown.ClearOptions();
        List<string> options = new List<string> {
            DisplaySettingsManager.Instance.GetWindowModeText(DisplaySettingsManager.WindowMode.ExclusiveFullScreen),
            DisplaySettingsManager.Instance.GetWindowModeText(DisplaySettingsManager.WindowMode.BorderlessWindow),
            DisplaySettingsManager.Instance.GetWindowModeText(DisplaySettingsManager.WindowMode.Windowed)
        };
        windowModeDropdown.AddOptions(options);
        windowModeDropdown.value = (int)DisplaySettingsManager.Instance.GetCurrentWindowMode();
        windowModeDropdown.onValueChanged.AddListener((int index) => {
            DisplaySettingsManager.Instance.SetWindowMode((DisplaySettingsManager.WindowMode)index);
        });
    }

    private void KitchenGameManager_OnGameUnpaused(object sender, System.EventArgs e) {
        Hide();
    }

    private void UpdateVisual() {
        float sfxVol = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
        float musVol = MusicManager.Instance != null ? MusicManager.Instance.GetVolume() : 1f;

        if (soundEffectsSlider != null) soundEffectsSlider.SetValueWithoutNotify(sfxVol);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(musVol);

        if (soundEffectsText != null) soundEffectsText.text = "SFX: " + Mathf.RoundToInt(sfxVol * 100f) + "%";
        if (musicText != null) musicText.text = "MUSIC: " + Mathf.RoundToInt(musVol * 100f) + "%";

        if (DisplaySettingsManager.Instance != null) {
            if (resolutionText != null) {
                resolutionText.text = "RES: " + DisplaySettingsManager.Instance.GetCurrentResolutionText();
            }
            if (windowModeText != null) {
                windowModeText.text = "MODE: " + DisplaySettingsManager.Instance.GetCurrentWindowModeText();
            }
            if (resolutionDropdown != null) {
                resolutionDropdown.SetValueWithoutNotify(DisplaySettingsManager.Instance.GetCurrentResolutionIndex());
            }
            if (windowModeDropdown != null) {
                windowModeDropdown.SetValueWithoutNotify((int)DisplaySettingsManager.Instance.GetCurrentWindowMode());
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

        gameObject.SetActive(true);

        if (soundEffectsButton != null) {
            soundEffectsButton.Select();
        }
    }

    private void Hide() {
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
        ShowPressToRebindKey();
        GameInput.Instance.RebindBinding(binding, () => {
            HidePressToRebindKey();
            UpdateVisual();
        });
    }

    private void OnDestroy() {
        if (DisplaySettingsManager.Instance != null) {
            DisplaySettingsManager.Instance.OnDisplaySettingsChanged -= DisplaySettingsManager_OnDisplaySettingsChanged;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class DisplaySettingsManager : MonoBehaviour {

    public static DisplaySettingsManager Instance { get; private set; }

    public const string PREFS_RES_WIDTH = "Display_ResWidth";
    public const string PREFS_RES_HEIGHT = "Display_ResHeight";
    public const string PREFS_RES_REFRESH = "Display_ResRefresh";
    public const string PREFS_WINDOW_MODE = "Display_WindowMode";

    public enum WindowMode {
        ExclusiveFullScreen = 0,
        BorderlessWindow = 1,
        Windowed = 2
    }

    public struct ResolutionItem {
        public int width;
        public int height;
        public int refreshRate;

        public ResolutionItem(int width, int height, int refreshRate = 60) {
            this.width = width;
            this.height = height;
            this.refreshRate = refreshRate;
        }

        public override string ToString() {
            return $"{width} x {height}";
        }
    }

    public event EventHandler OnDisplaySettingsChanged;

    private List<ResolutionItem> availableResolutions = new List<ResolutionItem>();
    private int currentResolutionIndex = 0;
    private WindowMode currentWindowMode = WindowMode.BorderlessWindow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize() {
        if (Instance == null) {
            GameObject go = new GameObject("DisplaySettingsManager");
            Instance = go.AddComponent<DisplaySettingsManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeResolutions();
        LoadAndApplySettings();
    }

    private void InitializeResolutions() {
        availableResolutions.Clear();

        Resolution[] screenResolutions = Screen.resolutions;
        HashSet<string> uniqueResolutions = new HashSet<string>();

        if (screenResolutions != null && screenResolutions.Length > 0) {
            for (int i = 0; i < screenResolutions.Length; i++) {
                Resolution res = screenResolutions[i];
                if (res.width < 1024 || res.height < 600) continue; // Skip sub-HD resolutions

                string key = $"{res.width}x{res.height}";
                if (!uniqueResolutions.Contains(key)) {
                    uniqueResolutions.Add(key);
                    int refresh = (int)Math.Round((double)res.refreshRateRatio.value > 0 ? (double)res.refreshRateRatio.value : 60);
                    availableResolutions.Add(new ResolutionItem(res.width, res.height, refresh));
                }
            }
        }

        // Standard fallback resolutions if query returned few or empty
        if (availableResolutions.Count == 0) {
            int[][] standardResolutions = new int[][] {
                new int[] { 3840, 2160 },
                new int[] { 2560, 1440 },
                new int[] { 1920, 1080 },
                new int[] { 1600, 900 },
                new int[] { 1366, 768 },
                new int[] { 1280, 720 }
            };

            foreach (var res in standardResolutions) {
                string key = $"{res[0]}x{res[1]}";
                if (!uniqueResolutions.Contains(key)) {
                    uniqueResolutions.Add(key);
                    availableResolutions.Add(new ResolutionItem(res[0], res[1], 60));
                }
            }
        }

        // Sort descending: highest resolution first
        availableResolutions.Sort((a, b) => {
            int comp = (b.width * b.height).CompareTo(a.width * a.height);
            return comp != 0 ? comp : b.width.CompareTo(a.width);
        });
    }

    public void LoadAndApplySettings() {
        int savedWidth = PlayerPrefs.GetInt(PREFS_RES_WIDTH, Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920);
        int savedHeight = PlayerPrefs.GetInt(PREFS_RES_HEIGHT, Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080);
        int savedMode = PlayerPrefs.GetInt(PREFS_WINDOW_MODE, (int)WindowMode.BorderlessWindow);

        currentWindowMode = (WindowMode)Mathf.Clamp(savedMode, 0, 2);

        // Find matching resolution index
        currentResolutionIndex = 0;
        for (int i = 0; i < availableResolutions.Count; i++) {
            if (availableResolutions[i].width == savedWidth && availableResolutions[i].height == savedHeight) {
                currentResolutionIndex = i;
                break;
            }
        }

        ApplyDisplaySettings(false);
    }

    public void ApplyDisplaySettings(bool saveToPrefs = true) {
        if (availableResolutions.Count == 0) return;

        ResolutionItem selectedRes = availableResolutions[currentResolutionIndex];
        FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

        switch (currentWindowMode) {
            case WindowMode.ExclusiveFullScreen:
                fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case WindowMode.BorderlessWindow:
                fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case WindowMode.Windowed:
                fullScreenMode = FullScreenMode.Windowed;
                break;
        }

        Screen.SetResolution(selectedRes.width, selectedRes.height, fullScreenMode, new RefreshRate { numerator = (uint)selectedRes.refreshRate, denominator = 1 });

        if (saveToPrefs) {
            PlayerPrefs.SetInt(PREFS_RES_WIDTH, selectedRes.width);
            PlayerPrefs.SetInt(PREFS_RES_HEIGHT, selectedRes.height);
            PlayerPrefs.SetInt(PREFS_RES_REFRESH, selectedRes.refreshRate);
            PlayerPrefs.SetInt(PREFS_WINDOW_MODE, (int)currentWindowMode);
            PlayerPrefs.Save();
        }

        OnDisplaySettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public List<ResolutionItem> GetResolutions() => availableResolutions;

    public int GetCurrentResolutionIndex() => currentResolutionIndex;

    public void SetResolutionIndex(int index) {
        if (index >= 0 && index < availableResolutions.Count) {
            currentResolutionIndex = index;
            ApplyDisplaySettings(true);
        }
    }

    public void CycleResolution(int delta = 1) {
        if (availableResolutions.Count == 0) return;
        currentResolutionIndex = (currentResolutionIndex + delta + availableResolutions.Count) % availableResolutions.Count;
        ApplyDisplaySettings(true);
    }

    public WindowMode GetCurrentWindowMode() => currentWindowMode;

    public void SetWindowMode(WindowMode mode) {
        currentWindowMode = mode;
        ApplyDisplaySettings(true);
    }

    public void CycleWindowMode(int delta = 1) {
        int count = Enum.GetValues(typeof(WindowMode)).Length;
        int next = ((int)currentWindowMode + delta + count) % count;
        currentWindowMode = (WindowMode)next;
        ApplyDisplaySettings(true);
    }

    public string GetWindowModeText(WindowMode mode) {
        switch (mode) {
            case WindowMode.ExclusiveFullScreen: return "EXCLUSIVE FULLSCREEN";
            case WindowMode.BorderlessWindow: return "BORDERLESS WINDOW";
            case WindowMode.Windowed: return "WINDOWED";
            default: return mode.ToString();
        }
    }

    public string GetCurrentResolutionText() {
        if (currentResolutionIndex >= 0 && currentResolutionIndex < availableResolutions.Count) {
            return availableResolutions[currentResolutionIndex].ToString();
        }
        return $"{Screen.width} x {Screen.height}";
    }

    public string GetCurrentWindowModeText() {
        return GetWindowModeText(currentWindowMode);
    }
}

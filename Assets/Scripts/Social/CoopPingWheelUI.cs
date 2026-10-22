using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Radial Co-op Ping & Emote Wheel UI.
/// Activates when holding 'Q' (Keyboard) or D-Pad Up (Gamepad).
/// Selecting an option triggers in-world 3D beacons, audio alerts, and HUD notifications.
/// </summary>
public class CoopPingWheelUI : MonoBehaviour {

    public static CoopPingWheelUI Instance { get; private set; }

    private readonly struct PingOption {
        public readonly CoopPingManager.PingType Type;
        public readonly string Title;
        public readonly string Emoji;
        public readonly float CenterAngle; // In degrees (0 = top, 90 = right, etc.)

        public PingOption(CoopPingManager.PingType type, string title, string emoji, float centerAngle) {
            Type = type;
            Title = title;
            Emoji = emoji;
            CenterAngle = centerAngle;
        }
    }

    private static readonly PingOption[] OPTIONS = new PingOption[] {
        new PingOption(CoopPingManager.PingType.OrderUp,       "Order Up!",     "(*)",   0f),
        new PingOption(CoopPingManager.PingType.GoingCity,     "Going City!",   ">>",  60f),
        new PingOption(CoopPingManager.PingType.ThankYou,      "Thanks!",       "+1", 120f),
        new PingOption(CoopPingManager.PingType.NeedHelp,      "Need Help!",    "SOS", 180f),
        new PingOption(CoopPingManager.PingType.NeedPlates,    "Need Plates!",  "[O]", 240f),
        new PingOption(CoopPingManager.PingType.EmergencyFire, "Fire / Alert!", "(!)", 300f)
    };


    [Header("UI Root")]
    private GameObject wheelPanel;
    private TextMeshProUGUI centerTitleText;
    private List<Image> segmentBackgrounds;
    private List<TextMeshProUGUI> segmentTexts;

    [Header("Toast Notifications")]
    private GameObject toastContainer;
    private TextMeshProUGUI toastText;
    private float toastHideTimer;

    private bool isWheelOpen;
    private int selectedIndex = -1;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        segmentBackgrounds = new List<Image>();
        segmentTexts = new List<TextMeshProUGUI>();

        BuildUI();
        HideWheel();
    }

    private void Start() {
        CoopPingManager.OnAnyPingReceived += CoopPingManager_OnAnyPingReceived;
    }

    private void OnDestroy() {
        CoopPingManager.OnAnyPingReceived -= CoopPingManager_OnAnyPingReceived;
        if (Instance == this) {
            Instance = null;
        }
    }

    private void CoopPingManager_OnAnyPingReceived(object sender, CoopPingManager.OnPingEventArgs e) {
        ShowToast($"{e.senderPlayerName}: {CoopPingManager.GetPingEmoji(e.pingType)} {e.messageText}", e.senderColor);
    }

    private void Update() {
        HandleInput();

        if (toastHideTimer > 0f) {
            toastHideTimer -= Time.deltaTime;
            if (toastHideTimer <= 0f && toastContainer != null) {
                toastContainer.SetActive(false);
            }
        }

        if (isWheelOpen) {
            UpdateWheelSelection();
        }
    }

    private void HandleInput() {
        bool pingKeyPressed = false;
        bool pingKeyReleased = false;

        if (Keyboard.current != null) {
            if (Keyboard.current.qKey.wasPressedThisFrame) pingKeyPressed = true;
            if (Keyboard.current.qKey.wasReleasedThisFrame) pingKeyReleased = true;
        }

        if (Gamepad.current != null) {
            if (Gamepad.current.dpad.up.wasPressedThisFrame) pingKeyPressed = true;
            if (Gamepad.current.dpad.up.wasReleasedThisFrame) pingKeyReleased = true;
        }

        if (pingKeyPressed && !isWheelOpen) {
            OpenWheel();
        } else if (pingKeyReleased && isWheelOpen) {
            CommitPingAndClose();
        }
    }

    public void OpenWheel() {
        isWheelOpen = true;
        wheelPanel.SetActive(true);
        selectedIndex = -1;
        centerTitleText.text = "Select Callout";
    }

    private void UpdateWheelSelection() {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir = mousePos - screenCenter;

        float dist = dir.magnitude;
        if (dist < 40f) {
            // Deadzone
            selectedIndex = -1;
            centerTitleText.text = "Hover an action";
            UpdateSegmentHighlights();
            return;
        }

        // Calculate angle: 0 at top (Y+), 90 at right (X+)
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        // Find closest option
        int bestIndex = 0;
        float minDiff = 360f;

        for (int i = 0; i < OPTIONS.Length; i++) {
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, OPTIONS[i].CenterAngle));
            if (diff < minDiff) {
                minDiff = diff;
                bestIndex = i;
            }
        }

        selectedIndex = bestIndex;
        centerTitleText.text = $"{OPTIONS[selectedIndex].Emoji} {OPTIONS[selectedIndex].Title}";
        UpdateSegmentHighlights();
    }

    private void UpdateSegmentHighlights() {
        for (int i = 0; i < segmentBackgrounds.Count; i++) {
            bool isSelected = (i == selectedIndex);
            segmentBackgrounds[i].color = isSelected ? new Color(0.2f, 0.7f, 1f, 0.95f) : new Color(0.08f, 0.12f, 0.18f, 0.85f);
            segmentTexts[i].color = isSelected ? Color.black : Color.white;
            segmentBackgrounds[i].transform.localScale = isSelected ? Vector3.one * 1.15f : Vector3.one;
        }
    }

    public void CommitPingAndClose() {
        if (selectedIndex >= 0 && selectedIndex < OPTIONS.Length && CoopPingManager.Instance != null) {
            CoopPingManager.PingType type = OPTIONS[selectedIndex].Type;
            Vector3 targetPos = DeterminePingWorldTarget();
            CoopPingManager.Instance.SendPing(type, targetPos);
        }

        HideWheel();
    }

    private Vector3 DeterminePingWorldTarget() {
        // Raycast from camera center to find what the player is pointing at
        Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0)) : new Ray(Vector3.zero, Vector3.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 50f)) {
            return hit.point;
        }

        // Fallback: in front of local player
        if (Player.LocalInstance != null) {
            return Player.LocalInstance.transform.position + Player.LocalInstance.transform.forward * 2f;
        }

        return Vector3.zero;
    }

    public void HideWheel() {
        isWheelOpen = false;
        selectedIndex = -1;
        if (wheelPanel != null) {
            wheelPanel.SetActive(false);
        }
    }

    private void ShowToast(string message, Color playerColor) {
        if (toastContainer == null || toastText == null) return;

        toastText.text = message;
        toastText.color = playerColor;
        toastContainer.SetActive(true);
        toastHideTimer = 3.5f;
    }

    private void BuildUI() {
        // Ensure root Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) {
            GameObject canvasGo = new GameObject("CoopPingCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasGo.transform, false);
        }

        // Wheel Panel
        wheelPanel = new GameObject("PingWheelPanel");
        wheelPanel.transform.SetParent(transform, false);
        RectTransform wheelRect = wheelPanel.AddComponent<RectTransform>();
        wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
        wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
        wheelRect.sizeDelta = new Vector2(400, 400);

        // Center hub
        GameObject centerGo = new GameObject("CenterHub");
        centerGo.transform.SetParent(wheelPanel.transform, false);
        Image centerImg = centerGo.AddComponent<Image>();
        centerImg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
        RectTransform centerRect = centerGo.GetComponent<RectTransform>();
        centerRect.sizeDelta = new Vector2(140, 140);

        GameObject centerTextGo = new GameObject("CenterTitle");
        centerTextGo.transform.SetParent(centerGo.transform, false);
        centerTitleText = centerTextGo.AddComponent<TextMeshProUGUI>();
        centerTitleText.text = "Select Callout";
        centerTitleText.fontSize = 15;
        centerTitleText.alignment = TextAlignmentOptions.Center;
        centerTitleText.fontStyle = FontStyles.Bold;
        centerTitleText.color = Color.white;
        RectTransform ctRect = centerTextGo.GetComponent<RectTransform>();
        ctRect.sizeDelta = new Vector2(130, 80);

        // Radial segments
        float radius = 135f;
        for (int i = 0; i < OPTIONS.Length; i++) {
            float rad = OPTIONS[i].CenterAngle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius);

            GameObject segGo = new GameObject($"Segment_{OPTIONS[i].Type}");
            segGo.transform.SetParent(wheelPanel.transform, false);
            Image segImg = segGo.AddComponent<Image>();
            segImg.color = new Color(0.08f, 0.12f, 0.18f, 0.85f);
            RectTransform segRect = segGo.GetComponent<RectTransform>();
            segRect.anchoredPosition = pos;
            segRect.sizeDelta = new Vector2(110, 50);

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(segGo.transform, false);
            TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{OPTIONS[i].Emoji}\n{OPTIONS[i].Title}";
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            RectTransform txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.sizeDelta = new Vector2(105, 46);

            segmentBackgrounds.Add(segImg);
            segmentTexts.Add(tmp);
        }

        // Toast Container (top-left below HUD clock)
        toastContainer = new GameObject("PingToastContainer");
        toastContainer.transform.SetParent(transform, false);
        Image toastBg = toastContainer.AddComponent<Image>();
        toastBg.color = new Color(0.05f, 0.08f, 0.14f, 0.90f);
        RectTransform tRect = toastContainer.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 0.85f);
        tRect.anchorMax = new Vector2(0f, 0.85f);
        tRect.pivot = new Vector2(0f, 1f);
        tRect.anchoredPosition = new Vector2(20, -10);
        tRect.sizeDelta = new Vector2(340, 48);

        GameObject tTextGo = new GameObject("ToastText");
        tTextGo.transform.SetParent(toastContainer.transform, false);
        toastText = tTextGo.AddComponent<TextMeshProUGUI>();
        toastText.text = "";
        toastText.fontSize = 16;
        toastText.alignment = TextAlignmentOptions.MidlineLeft;
        toastText.fontStyle = FontStyles.Bold;
        RectTransform ttRect = tTextGo.GetComponent<RectTransform>();
        ttRect.anchorMin = Vector2.zero;
        ttRect.anchorMax = Vector2.one;
        ttRect.sizeDelta = new Vector2(-20, 0);
        ttRect.anchoredPosition = new Vector2(10, 0);

        toastContainer.SetActive(false);
    }
}

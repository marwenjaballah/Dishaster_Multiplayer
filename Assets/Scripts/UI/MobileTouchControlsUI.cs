using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Cross-platform mobile touch control system for Android & iOS.
    /// Provides:
    /// 1. Floating Dynamic Virtual Joystick with configurable deadzone & smooth curvature.
    /// 2. Primary Action Button (Interact / Pick / Drop / Deliver).
    /// 3. Secondary Action Button (Chop / Scooter Mount / Extinguisher / Tool).
    /// 4. Tactical Co-op Ping Button (Emote Wheel).
    /// 5. Solo Chef Hot-Swap Button (Tab assist for 1-player mode).
    /// 6. Mobile Pause Button.
    /// 7. Screen Safe-Area notch & cutout adaptation.
    /// 8. Subtle tactile touch haptics.
    /// 
    /// Automatically activates ONLY on mobile platforms (Android/iOS) or when forced in Editor.
    /// </summary>
    public class MobileTouchControlsUI : MonoBehaviour
    {
        public static MobileTouchControlsUI Instance { get; private set; }

        [Header("Platform Settings")]
        [Tooltip("If true, controls will also be displayed in Unity Editor for testing touch simulation.")]
        [SerializeField] private bool forceShowInEditor = false;

        [Header("Joystick Settings")]
        [SerializeField] private float joystickRadius = 80f;
        [SerializeField] private float joystickDeadzone = 0.15f;
        [SerializeField] private float restingAlpha = 0.4f;
        [SerializeField] private float activeAlpha = 0.95f;

        [Header("Tactile Haptics")]
        [SerializeField] private bool enableHaptics = true;

        // UI hierarchy references
        private Canvas _canvas;
        private CanvasGroup _joystickCanvasGroup;
        private RectTransform _safeAreaRect;
        private RectTransform _joystickBaseRect;
        private RectTransform _joystickKnobRect;
        private RectTransform _touchZoneLeft;
        private GameObject _btnSwapChefGo;

        private Vector2 _joystickOrigin;
        private int _joystickFingerId = -1;
        private bool _isJoystickActive;
        private Rect _lastSafeArea = Rect.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Determine if we should show mobile controls
            bool isMobilePlatform = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
            bool shouldEnable = isMobilePlatform || (Application.isEditor && forceShowInEditor);

            if (!shouldEnable)
            {
                gameObject.SetActive(false);
                return;
            }

            // Optimize mobile runtime settings
            #if ENABLE_LEGACY_INPUT_MANAGER
            Input.multiTouchEnabled = true;
            #endif
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
            if (isMobilePlatform)
            {
                Application.targetFrameRate = 60;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            EnsureUIHierarchy();
            ApplySafeArea();
        }

        private void Start()
        {
            if (_joystickCanvasGroup != null)
            {
                _joystickCanvasGroup.alpha = restingAlpha;
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            if (_canvas != null)
            {
                _canvas.enabled = ShouldShowControls();
            }
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (_canvas != null)
            {
                _canvas.enabled = ShouldShowControls();
            }
            ApplySafeArea();
        }

        /// <summary>
        /// Adjusts safe-area anchors to avoid phone camera cutouts, notches, and home indicator bars.
        /// </summary>
        private void ApplySafeArea()
        {
            if (_safeAreaRect == null) return;

            Rect safeArea = Screen.safeArea;
            if (safeArea == _lastSafeArea) return;
            _lastSafeArea = safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _safeAreaRect.anchorMin = anchorMin;
            _safeAreaRect.anchorMax = anchorMax;
            _safeAreaRect.offsetMin = Vector2.zero;
            _safeAreaRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Returns true only if the currently active scene is a gameplay scene (GameScene, GameSceneTableService, etc.)
        /// </summary>
        private bool IsInGameplayScene()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return sceneName == Loader.Scene.GameScene.ToString() ||
                   sceneName == Loader.Scene.GameSceneTableService.ToString() ||
                   sceneName.Contains("GameScene");
        }

        private bool ShouldShowControls()
        {
            if (!IsInGameplayScene()) return false;

            if (KitchenGameManager.Instance != null)
            {
                if (KitchenGameManager.Instance.IsGameOver()) return false;
                if (KitchenGameManager.Instance.IsLocalGamePaused()) return false;
                if (KitchenGameManager.Instance.IsWaitingToStart()) return false;

                return KitchenGameManager.Instance.IsGamePlaying() || KitchenGameManager.Instance.IsCountdownToStartActive();
            }

            return false;
        }

        private void Update()
        {
            ApplySafeArea();

            bool show = ShouldShowControls();
            if (_canvas != null && _canvas.enabled != show)
            {
                _canvas.enabled = show;
                if (!show)
                {
                    ResetJoystickState();
                }
            }

            // Dynamically show/hide the Swap Chef button in solo assist mode
            if (_btnSwapChefGo != null && show)
            {
                bool isSolo = SoloChefHotSwapManager.Instance != null && SoloChefHotSwapManager.Instance.IsSoloMode();
                if (_btnSwapChefGo.activeSelf != isSolo)
                {
                    _btnSwapChefGo.SetActive(isSolo);
                }
            }
        }

        private void ResetJoystickState()
        {
            _isJoystickActive = false;
            _joystickFingerId = -1;
            if (_joystickKnobRect != null) _joystickKnobRect.anchoredPosition = Vector2.zero;
            if (GameInput.Instance != null)
            {
                GameInput.Instance.SetMobileMovementVector(Vector2.zero);
                GameInput.Instance.SetMobileInteractPressed(false);
                GameInput.Instance.SetMobileInteractAlternatePressed(false);
            }
        }

        private void TriggerHapticFeedback()
        {
            if (!enableHaptics) return;
            #if UNITY_ANDROID || UNITY_IOS
            try
            {
                Handheld.Vibrate();
            }
            catch {}
            #endif
        }

        // ── Touch Handling (Virtual Joystick) ─────────────────────────────────

        public void OnJoystickPointerDown(BaseEventData eventData)
        {
            if (eventData is PointerEventData pointerEvent)
            {
                _joystickFingerId = pointerEvent.pointerId;
                _isJoystickActive = true;

                // Move joystick base under player's touch
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _touchZoneLeft, pointerEvent.position, pointerEvent.pressEventCamera, out Vector2 localPoint);

                if (_joystickBaseRect != null)
                {
                    _joystickBaseRect.anchoredPosition = localPoint;
                }

                _joystickOrigin = localPoint;

                if (_joystickCanvasGroup != null)
                {
                    _joystickCanvasGroup.alpha = activeAlpha;
                }

                UpdateJoystickHandle(pointerEvent);
            }
        }

        public void OnJoystickDrag(BaseEventData eventData)
        {
            if (eventData is PointerEventData pointerEvent)
            {
                if (_isJoystickActive && pointerEvent.pointerId == _joystickFingerId)
                {
                    UpdateJoystickHandle(pointerEvent);
                }
            }
        }

        public void OnJoystickPointerUp(BaseEventData eventData)
        {
            if (eventData is PointerEventData pointerEvent)
            {
                if (pointerEvent.pointerId == _joystickFingerId)
                {
                    _isJoystickActive = false;
                    _joystickFingerId = -1;

                    if (_joystickKnobRect != null)
                    {
                        _joystickKnobRect.anchoredPosition = Vector2.zero;
                    }

                    if (GameInput.Instance != null)
                    {
                        GameInput.Instance.SetMobileMovementVector(Vector2.zero);
                    }

                    if (_joystickCanvasGroup != null)
                    {
                        _joystickCanvasGroup.alpha = restingAlpha;
                    }
                }
            }
        }

        private void UpdateJoystickHandle(PointerEventData pointerEvent)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystickBaseRect, pointerEvent.position, pointerEvent.pressEventCamera, out Vector2 localPoint);

            Vector2 clamped = Vector2.ClampMagnitude(localPoint, joystickRadius);

            if (_joystickKnobRect != null)
            {
                _joystickKnobRect.anchoredPosition = clamped;
            }

            // Deadzone and smooth remapping calculation
            float magnitude = clamped.magnitude / joystickRadius;
            Vector2 normalizedDir = Vector2.zero;

            if (magnitude >= joystickDeadzone)
            {
                float remapped = (magnitude - joystickDeadzone) / (1f - joystickDeadzone);
                normalizedDir = clamped.normalized * Mathf.Clamp01(remapped);
            }

            if (GameInput.Instance != null)
            {
                GameInput.Instance.SetMobileMovementVector(normalizedDir);
            }
        }

        // ── Action Buttons ────────────────────────────────────────────────────

        public void OnInteractPointerDown()
        {
            TriggerHapticFeedback();
            if (GameInput.Instance != null)
            {
                GameInput.Instance.SetMobileInteractPressed(true);
                GameInput.Instance.TriggerMobileInteract();
            }
        }

        public void OnInteractPointerUp()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.SetMobileInteractPressed(false);
            }
        }

        public void OnInteractAlternatePointerDown()
        {
            TriggerHapticFeedback();
            if (GameInput.Instance != null)
            {
                GameInput.Instance.SetMobileInteractAlternatePressed(true);
                GameInput.Instance.TriggerMobileInteractAlternate();
            }
        }

        public void OnInteractAlternatePointerUp()
        {
            if (GameInput.Instance != null)
            {
                GameInput.Instance.SetMobileInteractAlternatePressed(false);
            }
        }

        public void OnPauseButtonClicked()
        {
            TriggerHapticFeedback();
            if (GameInput.Instance != null)
            {
                GameInput.Instance.TriggerMobilePause();
            }
        }

        // ── Procedural HUD Construction ───────────────────────────────────────

        private void EnsureUIHierarchy()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 999;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Safe Area Root Container
            var safeAreaGo = new GameObject("SafeAreaContainer", typeof(RectTransform));
            safeAreaGo.transform.SetParent(transform, false);
            _safeAreaRect = safeAreaGo.GetComponent<RectTransform>();
            _safeAreaRect.anchorMin = Vector2.zero;
            _safeAreaRect.anchorMax = Vector2.one;
            _safeAreaRect.offsetMin = Vector2.zero;
            _safeAreaRect.offsetMax = Vector2.zero;

            // 1. Touch Zone (Left 45% of screen for floating joystick)
            var touchZoneGo = new GameObject("TouchZone_Left", typeof(RectTransform), typeof(Image));
            touchZoneGo.transform.SetParent(_safeAreaRect, false);
            _touchZoneLeft = touchZoneGo.GetComponent<RectTransform>();
            _touchZoneLeft.anchorMin = new Vector2(0f, 0f);
            _touchZoneLeft.anchorMax = new Vector2(0.45f, 0.85f);
            _touchZoneLeft.offsetMin = Vector2.zero;
            _touchZoneLeft.offsetMax = Vector2.zero;

            var touchZoneImg = touchZoneGo.GetComponent<Image>();
            touchZoneImg.color = new Color(0f, 0f, 0f, 0.001f);

            var trigger = touchZoneGo.AddComponent<EventTrigger>();
            AddEventTriggerEntry(trigger, EventTriggerType.PointerDown, OnJoystickPointerDown);
            AddEventTriggerEntry(trigger, EventTriggerType.Drag, OnJoystickDrag);
            AddEventTriggerEntry(trigger, EventTriggerType.PointerUp, OnJoystickPointerUp);

            // 2. Joystick Visual Base & Knob
            var baseGo = new GameObject("Joystick_Base", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            baseGo.transform.SetParent(_touchZoneLeft, false);
            _joystickBaseRect = baseGo.GetComponent<RectTransform>();
            _joystickBaseRect.sizeDelta = new Vector2(joystickRadius * 2.2f, joystickRadius * 2.2f);
            _joystickBaseRect.anchoredPosition = new Vector2(180f, 180f);

            _joystickCanvasGroup = baseGo.GetComponent<CanvasGroup>();
            _joystickCanvasGroup.blocksRaycasts = false;

            var baseImg = baseGo.GetComponent<Image>();
            baseImg.color = new Color(0.10f, 0.14f, 0.22f, 0.65f);
            baseImg.raycastTarget = false;

            // Base border ring
            var borderGo = new GameObject("BorderRing", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(baseGo.transform, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.sizeDelta = _joystickBaseRect.sizeDelta;
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.color = new Color(0.2f, 0.85f, 1f, 0.45f);
            borderImg.raycastTarget = false;

            // Joystick Knob (Handle)
            var knobGo = new GameObject("Joystick_Knob", typeof(RectTransform), typeof(Image));
            knobGo.transform.SetParent(baseGo.transform, false);
            _joystickKnobRect = knobGo.GetComponent<RectTransform>();
            _joystickKnobRect.sizeDelta = new Vector2(joystickRadius * 0.95f, joystickRadius * 0.95f);
            _joystickKnobRect.anchoredPosition = Vector2.zero;

            var knobImg = knobGo.GetComponent<Image>();
            knobImg.color = new Color(0.25f, 0.88f, 1f, 0.90f);
            knobImg.raycastTarget = false;

            // 3. Action Buttons Container (Bottom-Right)
            var actionContainer = new GameObject("ActionButtons_Right", typeof(RectTransform));
            actionContainer.transform.SetParent(_safeAreaRect, false);
            var actRect = actionContainer.GetComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.65f, 0f);
            actRect.anchorMax = new Vector2(1f, 0.55f);
            actRect.offsetMin = Vector2.zero;
            actRect.offsetMax = Vector2.zero;

            // Primary Button: INTERACT [E]
            CreateActionButton(
                parent: actionContainer.transform,
                name: "Btn_Interact",
                label: "INTERACT\n[E]",
                anchoredPos: new Vector2(-150f, 160f),
                size: new Vector2(130f, 130f),
                bgColor: new Color(0.12f, 0.75f, 0.45f, 0.85f),
                accentColor: new Color(0.4f, 1f, 0.7f, 1f),
                onDown: OnInteractPointerDown,
                onUp: OnInteractPointerUp
            );

            // Secondary Button: ACTION / SCOOTER [F]
            CreateActionButton(
                parent: actionContainer.transform,
                name: "Btn_ActionAlternate",
                label: "ACTION\n[F]",
                anchoredPos: new Vector2(-310f, 110f),
                size: new Vector2(110f, 110f),
                bgColor: new Color(0.95f, 0.55f, 0.12f, 0.85f),
                accentColor: new Color(1f, 0.8f, 0.3f, 1f),
                onDown: OnInteractAlternatePointerDown,
                onUp: OnInteractAlternatePointerUp
            );

            // Tactical Co-op Ping Button: PING [Q]
            CreateActionButton(
                parent: actionContainer.transform,
                name: "Btn_Ping",
                label: "PING\n[Q]",
                anchoredPos: new Vector2(-150f, 310f),
                size: new Vector2(90f, 90f),
                bgColor: new Color(0.15f, 0.45f, 0.92f, 0.85f),
                accentColor: new Color(0.4f, 0.75f, 1f, 1f),
                onDown: () => {
                    TriggerHapticFeedback();
                    CoopPingWheelUI.Instance?.OpenWheel();
                },
                onUp: () => CoopPingWheelUI.Instance?.CommitPingAndClose()
            );

            // Solo Mode Assist: SWAP CHEF [TAB] Button
            _btnSwapChefGo = CreateActionButton(
                parent: actionContainer.transform,
                name: "Btn_SwapChef",
                label: "SWAP\nCHEF",
                anchoredPos: new Vector2(-310f, 250f),
                size: new Vector2(95f, 95f),
                bgColor: new Color(0.95f, 0.75f, 0.15f, 0.90f), // Golden Yellow
                accentColor: new Color(1f, 0.95f, 0.5f, 1f),
                onDown: () => {
                    TriggerHapticFeedback();
                    SoloChefHotSwapManager.Instance?.SwapActiveChef();
                },
                onUp: () => {}
            );
            _btnSwapChefGo.SetActive(false); // Shown conditionally in Update() if solo mode

            // Mobile Pause Button (Top-Right)
            CreateMobilePauseButton(_safeAreaRect);
        }

        private GameObject CreateActionButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size,
                                              Color bgColor, Color accentColor, Action onDown, Action onUp)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(EventTrigger));
            btnGo.transform.SetParent(parent, false);

            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            // Outer Glow/Border
            var borderGo = new GameObject("GlowBorder", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(btnGo.transform, false);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.sizeDelta = size * 1.06f;
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.color = accentColor;
            borderImg.raycastTarget = false;

            // Label Text
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = size;
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            // Hook Touch Press / Release
            var trigger = btnGo.GetComponent<EventTrigger>();
            AddEventTriggerEntry(trigger, EventTriggerType.PointerDown, (e) =>
            {
                rect.localScale = new Vector3(0.92f, 0.92f, 1f);
                img.color = Color.Lerp(bgColor, Color.white, 0.3f);
                onDown?.Invoke();
            });

            AddEventTriggerEntry(trigger, EventTriggerType.PointerUp, (e) =>
            {
                rect.localScale = Vector3.one;
                img.color = bgColor;
                onUp?.Invoke();
            });

            return btnGo;
        }

        private void CreateMobilePauseButton(Transform parent)
        {
            var pauseGo = new GameObject("Btn_MobilePause", typeof(RectTransform), typeof(Image), typeof(Button));
            pauseGo.transform.SetParent(parent, false);

            var rect = pauseGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            rect.sizeDelta = new Vector2(65f, 65f);

            var img = pauseGo.GetComponent<Image>();
            img.color = new Color(0.12f, 0.15f, 0.22f, 0.85f);

            var textGo = new GameObject("PauseIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(pauseGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(65f, 65f);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "❚❚";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.95f, 1f, 0.95f);
            tmp.raycastTarget = false;

            var btn = pauseGo.GetComponent<Button>();
            btn.onClick.AddListener(OnPauseButtonClicked);
        }

        private void AddEventTriggerEntry(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener((data) => callback?.Invoke(data));
            trigger.triggers.Add(entry);
        }
    }
}

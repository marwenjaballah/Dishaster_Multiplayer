using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a floating [E] or [F] key prompt visual above interactive stations.
/// When approaching: displays the key prompt (e.g. [E] or [F]).
/// When holding the key: hides the key prompt and shows a sleek circular ring progress bar filling smoothly clockwise over time (default 3s).
/// If released early: progress resets to 0 and the key prompt reappears.
/// When completed: the UI hides completely.
/// When moving away and returning: the key prompt reappears as normal.
/// </summary>
public class InteractKeyPromptUI : MonoBehaviour {

    public event Action OnHoldCompleted;

    [Header("References")]
    [SerializeField] private BaseCounter baseCounter;
    [SerializeField] private GameObject visualContainer;
    [SerializeField] private TextMeshProUGUI promptKeyText;
    [SerializeField] private Image radialFillImage;
    [SerializeField] private Image trackRingImage;

    [Header("Settings")]
    [SerializeField] private string promptKey = "E";
    [SerializeField] private float hoverHeight = 2.2f;
    [SerializeField] private bool animateBobbing = true;

    [Header("Hold Settings")]
    [SerializeField] private bool requireHold = false;
    [SerializeField] private float holdDuration = 3.0f;

    private Vector3 initialLocalPos;
    private bool isSelected = false;
    private bool isHoldCompleted = false;
    private float currentHoldProgress = 0f;
    private Func<bool> canShowPredicate;

    private static Sprite cachedRingSprite;
    private static Sprite cachedCircleSprite;

    private void Awake() {
        if (baseCounter == null) {
            baseCounter = GetComponentInParent<BaseCounter>();
        }

        FindReferences();

        initialLocalPos = transform.localPosition;
        if (initialLocalPos.y < 0.5f) {
            initialLocalPos = new Vector3(0, hoverHeight, 0);
            transform.localPosition = initialLocalPos;
        }
    }

    private void FindReferences() {
        if (visualContainer == null || !visualContainer) {
            visualContainer = transform.Find("VisualContainer")?.gameObject;
        }
        if (visualContainer == null) {
            visualContainer = gameObject;
        }

        // Disable any legacy spinner rectangle
        Transform spinner = visualContainer.transform.Find("Spinner");
        if (spinner != null) {
            spinner.gameObject.SetActive(false);
        }

        if (promptKeyText == null || !promptKeyText) {
            promptKeyText = visualContainer.transform.Find("KeyText")?.GetComponent<TextMeshProUGUI>();
            if (promptKeyText == null) {
                promptKeyText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (radialFillImage == null || !radialFillImage) {
            radialFillImage = visualContainer.transform.Find("RadialFill")?.GetComponent<Image>();
        }

        // Configure circular ring fill
        Sprite ringSprite = GetRingSprite();
        if (radialFillImage != null) {
            radialFillImage.sprite = ringSprite;
            radialFillImage.type = Image.Type.Filled;
            radialFillImage.fillMethod = Image.FillMethod.Radial360;
            radialFillImage.fillOrigin = (int)Image.Origin360.Top;
            radialFillImage.fillClockwise = true;
            radialFillImage.color = new Color(0.2f, 0.85f, 1f, 1f); // Vibrant cyan
            radialFillImage.fillAmount = 0f;
        }

        // Setup background track ring if not present
        Transform trackTr = visualContainer.transform.Find("TrackRing");
        if (trackTr != null) {
            trackRingImage = trackTr.GetComponent<Image>();
        } else {
            GameObject trackGo = new GameObject("TrackRing");
            trackGo.transform.SetParent(visualContainer.transform, false);
            trackGo.transform.SetAsFirstSibling();
            RectTransform trRect = trackGo.AddComponent<RectTransform>();
            trRect.sizeDelta = new Vector2(70, 70);
            trRect.anchoredPosition = Vector2.zero;

            trackRingImage = trackGo.AddComponent<Image>();
            trackRingImage.sprite = ringSprite;
            trackRingImage.color = new Color(0.15f, 0.18f, 0.25f, 0.55f); // Subtle dark track
        }
    }

    private void Start() {
        FindReferences();

        if (promptKeyText != null) {
            promptKeyText.text = promptKey;
        }

        if (radialFillImage != null) {
            radialFillImage.fillAmount = 0f;
        }

        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
        } else {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }

        UpdateVisualState(isHolding: false);
        Hide();
    }

    private void OnDestroy() {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
        }
        Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
    }

    private void Player_OnAnyPlayerSpawned(object sender, EventArgs e) {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
            Player.LocalInstance.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
        }
    }

    private void Player_OnSelectedCounterChanged(object sender, Player.OnSelectedCounterChangedEventArgs e) {
        if (e.selectedCounter == baseCounter && baseCounter != null) {
            isSelected = true;
            isHoldCompleted = false;
            currentHoldProgress = 0f;

            if (CanShowPrompt()) {
                UpdateVisualState(isHolding: false);
                Show();
            } else {
                Hide();
            }
        } else {
            isSelected = false;
            isHoldCompleted = false;
            currentHoldProgress = 0f;
            UpdateVisualState(isHolding: false);
            Hide();
        }
    }

    public void SetCanShowPredicate(Func<bool> predicate) {
        canShowPredicate = predicate;
    }

    private bool CanShowPrompt() {
        if (canShowPredicate != null) {
            return canShowPredicate();
        }
        return true;
    }

    private void Update() {
        if (!isSelected) return;

        if (isHoldCompleted || !CanShowPrompt()) {
            if (visualContainer != null && visualContainer.activeSelf) {
                Hide();
            }
            return;
        }

        if (visualContainer != null && !visualContainer.activeSelf) {
            UpdateVisualState(isHolding: false);
            Show();
        }

        if (requireHold) {
            HandleHoldInput();
        }
    }

    private void HandleHoldInput() {
        bool isKeyPressed = false;
        bool isFKey = string.Equals(promptKey, "F", StringComparison.OrdinalIgnoreCase);

        if (GameInput.Instance != null) {
            if (isFKey) {
                isKeyPressed = GameInput.Instance.IsInteractAlternatePressed();
            } else {
                isKeyPressed = GameInput.Instance.IsInteractPressed();
            }
        }

        // Fallback for direct keyboard input
        if (!isKeyPressed) {
            if (isFKey) {
                isKeyPressed = Input.GetKey(KeyCode.F);
            } else {
                isKeyPressed = Input.GetKey(KeyCode.E);
            }
        }

        if (isKeyPressed) {
            // Holding key: switch visual to circular progress ring
            if (currentHoldProgress == 0f) {
                UpdateVisualState(isHolding: true);
            }

            currentHoldProgress += Time.deltaTime / Mathf.Max(0.1f, holdDuration);

            if (radialFillImage != null) {
                radialFillImage.fillAmount = Mathf.Clamp01(currentHoldProgress);
            }

            if (currentHoldProgress >= 1f) {
                isHoldCompleted = true;
                currentHoldProgress = 1f;
                if (radialFillImage != null) radialFillImage.fillAmount = 1f;
                OnHoldCompleted?.Invoke();
                Hide(); // Hide completely upon completion
            }
        } else {
            // Key released early: reset progress and switch visual back to key prompt
            if (currentHoldProgress > 0f) {
                currentHoldProgress = 0f;
                if (radialFillImage != null) {
                    radialFillImage.fillAmount = 0f;
                }
                UpdateVisualState(isHolding: false);
            }
        }
    }

    private void UpdateVisualState(bool isHolding) {
        if (promptKeyText != null) {
            promptKeyText.gameObject.SetActive(!isHolding);
            if (!isHolding) {
                promptKeyText.text = promptKey;
            }
        }

        if (radialFillImage != null) {
            radialFillImage.gameObject.SetActive(isHolding);
            if (!isHolding) {
                radialFillImage.fillAmount = 0f;
            }
        }

        if (trackRingImage != null) {
            trackRingImage.gameObject.SetActive(isHolding);
        }
    }

    private void LateUpdate() {
        if (visualContainer != null && visualContainer.activeSelf) {
            // Billboarding towards main camera without mirroring
            if (Camera.main != null) {
                transform.forward = Camera.main.transform.forward;
            }

            // Subtle bobbing animation
            if (animateBobbing) {
                float bobOffset = Mathf.Sin(Time.time * 5f) * 0.04f;
                transform.localPosition = initialLocalPos + new Vector3(0, bobOffset, 0);
            }
        }
    }

    public void ConfigureHold(bool isHold, float duration, string key) {
        requireHold = isHold;
        holdDuration = duration;
        SetPromptKey(key);
    }

    public void SetPromptKey(string key) {
        promptKey = key;
        if (promptKeyText != null) {
            promptKeyText.text = key;
        }
    }

    public void SetHoldProgress(float normalized) {
        currentHoldProgress = normalized;
        if (radialFillImage != null) {
            radialFillImage.fillAmount = Mathf.Clamp01(normalized);
        }
    }

    public void Show() {
        UpdateVisualState(isHolding: false);
        if (visualContainer != null) {
            visualContainer.SetActive(true);
        }
    }

    public void Hide() {
        if (visualContainer != null) {
            visualContainer.SetActive(false);
        }
    }

    private static Sprite GetRingSprite() {
        if (cachedRingSprite != null) return cachedRingSprite;

        // Try load from asset
        cachedRingSprite = Resources.Load<Sprite>("UIRadialCircleRing");
#if UNITY_EDITOR
        if (cachedRingSprite == null) {
            cachedRingSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Textures/UIRadialCircleRing.png");
        }
#endif
        if (cachedRingSprite != null) return cachedRingSprite;

        // Procedural smooth ring fallback
        int size = 128;
        Texture2D ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        ringTex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = (size - 4) * 0.5f;
        float innerRadius = outerRadius * 0.72f;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist >= innerRadius && dist <= outerRadius) {
                    float edgeDist = Mathf.Min(dist - innerRadius, outerRadius - dist);
                    float alpha = Mathf.Clamp01(edgeDist / 1.2f);
                    ringTex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                } else {
                    ringTex.SetPixel(x, y, Color.clear);
                }
            }
        }
        ringTex.Apply();
        cachedRingSprite = Sprite.Create(ringTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return cachedRingSprite;
    }
}

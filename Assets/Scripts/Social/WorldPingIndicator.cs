using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A 3D in-world animated beacon that billboards toward the camera when a co-op ping is triggered.
/// </summary>
public class WorldPingIndicator : MonoBehaviour {

    private const float LIFETIME_SECONDS = 4.0f;
    private const float FADE_START_SECONDS = 3.2f;

    private float spawnTime;
    private Vector3 basePosition;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI iconText;
    private TextMeshProUGUI senderText;
    private Image backgroundBadge;

    public void Initialize(CoopPingManager.PingType pingType, string senderName, Color playerColor) {
        spawnTime = Time.time;
        basePosition = transform.position;

        // Create in-world billboard canvas
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 100);
        rect.localScale = Vector3.one * 0.012f;

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Badge background
        GameObject bgGo = new GameObject("BadgeBackground");
        bgGo.transform.SetParent(canvas.transform, false);
        backgroundBadge = bgGo.AddComponent<Image>();
        backgroundBadge.color = new Color(0.08f, 0.11f, 0.15f, 0.88f);
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Icon text (emoji / symbol)
        GameObject iconGo = new GameObject("IconText");
        iconGo.transform.SetParent(canvas.transform, false);
        iconText = iconGo.AddComponent<TextMeshProUGUI>();
        iconText.text = $"{CoopPingManager.GetPingEmoji(pingType)} {CoopPingManager.GetPingDescription(pingType)}";
        iconText.fontSize = 22;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = pingType == CoopPingManager.PingType.EmergencyFire ? new Color(1f, 0.3f, 0.2f) : Color.white;
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.35f);
        iconRect.anchorMax = new Vector2(1, 1);
        iconRect.sizeDelta = Vector2.zero;

        // Sender text badge
        GameObject senderGo = new GameObject("SenderText");
        senderGo.transform.SetParent(canvas.transform, false);
        senderText = senderGo.AddComponent<TextMeshProUGUI>();
        senderText.text = senderName;
        senderText.fontSize = 16;
        senderText.alignment = TextAlignmentOptions.Center;
        senderText.color = playerColor;
        RectTransform senderRect = senderGo.GetComponent<RectTransform>();
        senderRect.anchorMin = new Vector2(0, 0);
        senderRect.anchorMax = new Vector2(1, 0.35f);
        senderRect.sizeDelta = Vector2.zero;
    }

    private void Update() {
        // Face camera
        if (Camera.main != null) {
            transform.rotation = Camera.main.transform.rotation;
        }

        // Bobbing floating animation
        float elapsed = Time.time - spawnTime;
        float bob = Mathf.Sin(elapsed * 5f) * 0.08f;
        transform.position = basePosition + Vector3.up * bob;

        // Fade out
        if (elapsed > FADE_START_SECONDS) {
            float fadeProgress = (elapsed - FADE_START_SECONDS) / (LIFETIME_SECONDS - FADE_START_SECONDS);
            if (canvasGroup != null) {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeProgress);
            }
        }

        if (elapsed >= LIFETIME_SECONDS) {
            Destroy(gameObject);
        }
    }
}

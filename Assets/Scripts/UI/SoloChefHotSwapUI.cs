using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On-screen HUD badge showing current active chef and hot-swap prompt ([TAB] / [Y]).
/// Only visible in Solo Play mode.
/// </summary>
public class SoloChefHotSwapUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI chefTitleText;
    [SerializeField] private TextMeshProUGUI hotkeyPromptText;
    [SerializeField] private Image activeChefIcon;

    [Header("Styling")]
    [SerializeField] private Color chefAColor = new Color(0.2f, 0.6f, 1f);   // Blue
    [SerializeField] private Color chefBColor = new Color(0.95f, 0.75f, 0.1f); // Gold/Yellow

    private void Start()
    {
        SoloChefHotSwapManager.OnActiveChefChanged += SoloChefHotSwapManager_OnActiveChefChanged;

        if (container == null)
        {
            CreateProceduralUI();
        }

        UpdateVisuals(0);
    }

    private void OnDestroy()
    {
        SoloChefHotSwapManager.OnActiveChefChanged -= SoloChefHotSwapManager_OnActiveChefChanged;
    }

    private void CreateProceduralUI()
    {
        // Procedurally construct a sleek floating badge at top-left
        GameObject canvasGo = new GameObject("SoloChefHotSwapCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        container = new GameObject("HotSwapBadge");
        container.transform.SetParent(canvasGo.transform, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(25f, -90f);
        rect.sizeDelta = new Vector2(240f, 50f);

        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.11f, 0.16f, 0.88f); // Dark translucent slate

        // Chef Title Text
        GameObject textGo = new GameObject("ChefTitleText");
        textGo.transform.SetParent(container.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15f, 5f);
        textRect.offsetMax = new Vector2(-15f, -5f);

        chefTitleText = textGo.AddComponent<TextMeshProUGUI>();
        chefTitleText.fontSize = 18;
        chefTitleText.fontStyle = FontStyles.Bold;
        chefTitleText.alignment = TextAlignmentOptions.MidlineLeft;
        chefTitleText.text = "[TAB] Chef 1 (Kitchen)";
    }

    private void Update()
    {
        if (container == null) return;

        bool isSolo = SoloChefHotSwapManager.Instance != null && SoloChefHotSwapManager.Instance.IsSoloMode();
        if (container.activeSelf != isSolo)
        {
            container.SetActive(isSolo);
        }
    }

    private void SoloChefHotSwapManager_OnActiveChefChanged(object sender, int activeChefIndex)
    {
        UpdateVisuals(activeChefIndex);
    }

    private void UpdateVisuals(int activeChefIndex)
    {
        if (chefTitleText == null) return;

        if (activeChefIndex == 0)
        {
            chefTitleText.text = "<color=#38BDF8>[TAB]</color> 👨‍🍳 Chef 1 (Kitchen)";
        }
        else
        {
            chefTitleText.text = "<color=#FACC15>[TAB]</color> 🛵 Chef 2 (Courier)";
        }
    }
}

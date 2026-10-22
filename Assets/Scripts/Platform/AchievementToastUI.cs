using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Platform
{
    /// <summary>
    /// Displays an authentic Steam-styled achievement notification toast in the bottom-right corner.
    /// Automatically listens to PlatformSteamManager.OnAnyAchievementUnlocked.
    /// </summary>
    public class AchievementToastUI : MonoBehaviour
    {
        private GameObject _toastPanel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descText;
        private Coroutine _displayRoutine;

        private void Awake()
        {
            BuildToastHierarchy();
            if (_toastPanel != null) _toastPanel.SetActive(false);
        }

        private void Start()
        {
            PlatformSteamManager.OnAnyAchievementUnlocked += PlatformSteamManager_OnAnyAchievementUnlocked;
        }

        private void OnDestroy()
        {
            PlatformSteamManager.OnAnyAchievementUnlocked -= PlatformSteamManager_OnAnyAchievementUnlocked;
        }

        private void PlatformSteamManager_OnAnyAchievementUnlocked(object sender, PlatformSteamManager.AchievementUnlockedEventArgs e)
        {
            if (_displayRoutine != null) StopCoroutine(_displayRoutine);
            _displayRoutine = StartCoroutine(ShowToastRoutine(e.title, e.description));
        }

        private IEnumerator ShowToastRoutine(string title, string desc)
        {
            if (_titleText != null) _titleText.text = title;
            if (_descText != null) _descText.text = desc;
            if (_toastPanel != null) _toastPanel.SetActive(true);

            // Play celebratory audio chime
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCountdownSound();
            }

            yield return new WaitForSeconds(4.5f);

            if (_toastPanel != null) _toastPanel.SetActive(false);
            _displayRoutine = null;
        }

        private void BuildToastHierarchy()
        {
            // Canvas
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 99;
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Panel
            _toastPanel = new GameObject("AchievementToastPanel");
            _toastPanel.transform.SetParent(transform, false);
            RectTransform panelRect = _toastPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-25f, 25f);
            panelRect.sizeDelta = new Vector2(380f, 95f);

            Image panelImg = _toastPanel.AddComponent<Image>();
            panelImg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            // Left Gold Badge Icon
            GameObject iconGo = new GameObject("TrophyBadge");
            iconGo.transform.SetParent(_toastPanel.transform, false);
            RectTransform iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(14f, 0f);
            iconRect.sizeDelta = new Vector2(58f, 58f);

            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(1f, 0.78f, 0.15f); // Gold

            // Header "ACHIEVEMENT UNLOCKED"
            GameObject headerGo = new GameObject("HeaderLabel");
            headerGo.transform.SetParent(_toastPanel.transform, false);
            RectTransform headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(85f, -12f);
            headerRect.sizeDelta = new Vector2(-95f, 20f);

            TextMeshProUGUI headerText = headerGo.AddComponent<TextMeshProUGUI>();
            headerText.text = "ACHIEVEMENT UNLOCKED";
            headerText.fontSize = 13f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = new Color(0.35f, 0.75f, 1f); // Steam Cyan

            // Title
            GameObject titleGo = new GameObject("TitleLabel");
            titleGo.transform.SetParent(_toastPanel.transform, false);
            RectTransform titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(85f, -32f);
            titleRect.sizeDelta = new Vector2(-95f, 24f);

            _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.text = "Title";
            _titleText.fontSize = 17f;
            _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = Color.white;

            // Description
            GameObject descGo = new GameObject("DescLabel");
            descGo.transform.SetParent(_toastPanel.transform, false);
            RectTransform descRect = descGo.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0f);
            descRect.pivot = new Vector2(0f, 0f);
            descRect.anchoredPosition = new Vector2(85f, 12f);
            descRect.sizeDelta = new Vector2(-95f, 26f);

            _descText = descGo.AddComponent<TextMeshProUGUI>();
            _descText.text = "Description";
            _descText.fontSize = 12f;
            _descText.color = new Color(0.75f, 0.78f, 0.82f);
        }
    }
}

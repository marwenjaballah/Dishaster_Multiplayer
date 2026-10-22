using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Campaign Level Selection & City Venue Browser UI.
    /// Displays unlocked restaurants, star ratings, daily rent requirements, and courier distance metrics.
    /// Allows the host to switch between Level 1 (Sunny Side Diner), Level 2 (Downtown Burger Bar),
    /// and Level 3 (Boardwalk Grill).
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        public static LevelSelectUI Instance { get; private set; }

        public static void ResetStaticData()
        {
            Instance = null;
        }

        private Canvas _canvas;
        private GameObject _panel;
        private List<LevelCardVisual> _levelCards = new List<LevelCardVisual>();

        private class LevelCardVisual
        {
            public int levelIndex;
            public Image backgroundCard;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI subtitleText;
            public TextMeshProUGUI statsText;
            public TextMeshProUGUI starsText;
            public Button selectButton;
            public TextMeshProUGUI buttonLabel;
            public GameObject activeBadge;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
        }

        private void Start()
        {
            LevelSelectionManager.OnAnyLevelChanged += LevelSelectionManager_OnAnyLevelChanged;
            Save.GameSaveManager.OnAnySaveDataLoaded += GameSaveManager_OnAnySaveDataLoaded;
            Save.GameSaveManager.OnAnyGameSaved += GameSaveManager_OnAnyGameSaved;

            Hide();
        }

        private void OnDestroy()
        {
            LevelSelectionManager.OnAnyLevelChanged -= LevelSelectionManager_OnAnyLevelChanged;
            Save.GameSaveManager.OnAnySaveDataLoaded -= GameSaveManager_OnAnySaveDataLoaded;
            Save.GameSaveManager.OnAnyGameSaved -= GameSaveManager_OnAnyGameSaved;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // Toggle level select UI with [L] key in editor or solo play
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame)
            {
                if (IsVisible())
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }
        }

        private void LevelSelectionManager_OnAnyLevelChanged(object sender, LevelSelectionManager.OnLevelChangedEventArgs e)
        {
            RefreshUI();
        }

        private void GameSaveManager_OnAnySaveDataLoaded(object sender, Save.GameSaveManager.OnSaveDataLoadedEventArgs e)
        {
            RefreshUI();
        }

        private void GameSaveManager_OnAnyGameSaved(object sender, EventArgs e)
        {
            RefreshUI();
        }

        public void Show()
        {
            if (_canvas != null)
            {
                _canvas.enabled = true;
            }
            if (_panel != null)
            {
                _panel.SetActive(true);
            }
            RefreshUI();
        }

        public void Hide()
        {
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public bool IsVisible() => _canvas != null && _canvas.enabled;

        public void RefreshUI()
        {
            if (LevelSelectionManager.Instance == null) return;

            int activeIndex = LevelSelectionManager.Instance.ActiveLevelIndex;

            for (int i = 0; i < _levelCards.Count; i++)
            {
                LevelCardVisual card = _levelCards[i];
                LevelProfileSO profile = LevelSelectionManager.Instance.GetLevelProfile(i);
                if (profile == null) continue;

                bool isUnlocked = true;
                int bestStars = 0;
                int bestRevenue = 0;

                if (Save.GameSaveManager.Instance != null && Save.GameSaveManager.Instance.CurrentSaveData != null)
                {
                    isUnlocked = Save.GameSaveManager.Instance.IsLevelUnlocked(i);
                    bestStars = Save.GameSaveManager.Instance.GetLevelStars(i);
                    var prog = Save.GameSaveManager.Instance.CurrentSaveData.levelProgress.Find(l => l.levelIndex == i);
                    if (prog != null)
                    {
                        bestRevenue = prog.bestDailyRevenue;
                    }
                }

                bool isActive = (i == activeIndex);

                // Update text
                card.titleText.text = profile.levelName.ToUpper();
                card.subtitleText.text = profile.description;

                string starIcons = "";
                for (int s = 0; s < 3; s++)
                {
                    starIcons += s < bestStars ? "★ " : "☆ ";
                }
                card.starsText.text = $"CAREER RATING: <color=#FFD700>{starIcons.Trim()}</color>";

                card.statsText.text = $"• Daily Rent: <b>${profile.baseDailyRent}</b>\n" +
                                      $"• Courier Distance: <b>{profile.deliveryDistanceScale:0.0}x</b>\n" +
                                      $"• Best Shift Profit: <b>${bestRevenue}</b>\n" +
                                      $"• Target Revenue: <b>${profile.targetRevenueGoal}</b>";

                if (isActive)
                {
                    card.activeBadge.SetActive(true);
                    card.buttonLabel.text = "CURRENT VENUE";
                    card.selectButton.interactable = false;
                    card.backgroundCard.color = new Color(0.12f, 0.22f, 0.32f, 0.95f);
                }
                else if (!isUnlocked)
                {
                    card.activeBadge.SetActive(false);
                    card.buttonLabel.text = i == 1 ? "LOCKED (1★ Diner)" : "LOCKED (2★ Burger Bar)";
                    card.selectButton.interactable = false;
                    card.backgroundCard.color = new Color(0.12f, 0.12f, 0.15f, 0.70f);
                }
                else
                {
                    card.activeBadge.SetActive(false);
                    card.buttonLabel.text = "SELECT VENUE";
                    card.selectButton.interactable = true;
                    card.backgroundCard.color = new Color(0.15f, 0.18f, 0.25f, 0.90f);
                }
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
                _canvas.sortingOrder = 950;
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

            // Dark Backdrop
            _panel = new GameObject("LevelSelect_Backdrop", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImg = _panel.GetComponent<Image>();
            panelImg.color = new Color(0.05f, 0.07f, 0.12f, 0.88f); // Frosted Glass

            // Header Container
            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerGo.transform.SetParent(_panel.transform, false);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -60f);
            headerRect.sizeDelta = new Vector2(1000f, 60f);

            var headerTmp = headerGo.GetComponent<TextMeshProUGUI>();
            headerTmp.text = "CITY RESTAURANT VENUES";
            headerTmp.fontSize = 38;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = new Color(0.95f, 0.98f, 1f);

            // Subtitle
            var subGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGo.transform.SetParent(_panel.transform, false);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -125f);
            subRect.sizeDelta = new Vector2(1000f, 40f);

            var subTmp = subGo.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Select your kitchen venue, master delivery logistics, and expand your franchise.";
            subTmp.fontSize = 20;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.70f, 0.80f, 0.95f);

            // Cards Horizontal Layout Container
            var cardsGo = new GameObject("CardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardsGo.transform.SetParent(_panel.transform, false);
            var cardsRect = cardsGo.GetComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRect.pivot = new Vector2(0.5f, 0.5f);
            cardsRect.anchoredPosition = new Vector2(0f, -20f);
            cardsRect.sizeDelta = new Vector2(1450f, 550f);

            var hlg = cardsGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 35;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            _levelCards.Clear();
            for (int i = 0; i < 3; i++)
            {
                CreateLevelCard(cardsGo.transform, i);
            }

            // Close Button
            var closeGo = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(_panel.transform, false);
            var closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 45f);
            closeRect.sizeDelta = new Vector2(260f, 60f);

            var closeImg = closeGo.GetComponent<Image>();
            closeImg.color = new Color(0.25f, 0.30f, 0.40f, 0.90f);

            var closeTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTextRect = closeTextGo.GetComponent<RectTransform>();
            closeTextRect.sizeDelta = new Vector2(260f, 60f);

            var closeTmp = closeTextGo.GetComponent<TextMeshProUGUI>();
            closeTmp.text = "CLOSE [ESC / L]";
            closeTmp.fontSize = 20;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;

            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
        }

        private void CreateLevelCard(Transform parent, int index)
        {
            var cardGo = new GameObject($"Card_Level_{index}", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);

            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(430f, 530f);

            var cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0.15f, 0.18f, 0.25f, 0.90f);

            // Active Badge
            var badgeGo = new GameObject("ActiveBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(cardGo.transform, false);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, 18f);
            badgeRect.sizeDelta = new Vector2(200f, 36f);

            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.color = new Color(0.12f, 0.75f, 0.45f, 1f); // Vibrant Emerald

            var badgeTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextGo.transform.SetParent(badgeGo.transform, false);
            var badgeTextRect = badgeTextGo.GetComponent<RectTransform>();
            badgeTextRect.sizeDelta = new Vector2(200f, 36f);
            var badgeTmp = badgeTextGo.GetComponent<TextMeshProUGUI>();
            badgeTmp.text = "ACTIVE VENUE";
            badgeTmp.fontSize = 15;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = Color.white;

            // Title Text
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(cardGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -40f);
            titleRect.sizeDelta = new Vector2(390f, 50f);

            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = $"LEVEL {index + 1}";
            titleTmp.fontSize = 24;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            // Subtitle Description
            var descGo = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(cardGo.transform, false);
            var descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.5f, 1f);
            descRect.anchorMax = new Vector2(0.5f, 1f);
            descRect.pivot = new Vector2(0.5f, 1f);
            descRect.anchoredPosition = new Vector2(0f, -95f);
            descRect.sizeDelta = new Vector2(380f, 70f);

            var descTmp = descGo.GetComponent<TextMeshProUGUI>();
            descTmp.text = "Description";
            descTmp.fontSize = 16;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.color = new Color(0.75f, 0.85f, 0.95f);

            // Stars
            var starsGo = new GameObject("Stars", typeof(RectTransform), typeof(TextMeshProUGUI));
            starsGo.transform.SetParent(cardGo.transform, false);
            var starsRect = starsGo.GetComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.5f, 1f);
            starsRect.anchorMax = new Vector2(0.5f, 1f);
            starsRect.pivot = new Vector2(0.5f, 1f);
            starsRect.anchoredPosition = new Vector2(0f, -170f);
            starsRect.sizeDelta = new Vector2(380f, 35f);

            var starsTmp = starsGo.GetComponent<TextMeshProUGUI>();
            starsTmp.text = "CAREER RATING: ☆ ☆ ☆";
            starsTmp.fontSize = 17;
            starsTmp.fontStyle = FontStyles.Bold;
            starsTmp.alignment = TextAlignmentOptions.Center;
            starsTmp.color = new Color(1f, 0.85f, 0.3f);

            // Stats Block
            var statsGo = new GameObject("Stats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsGo.transform.SetParent(cardGo.transform, false);
            var statsRect = statsGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 1f);
            statsRect.anchorMax = new Vector2(0.5f, 1f);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.anchoredPosition = new Vector2(0f, -220f);
            statsRect.sizeDelta = new Vector2(380f, 150f);

            var statsTmp = statsGo.GetComponent<TextMeshProUGUI>();
            statsTmp.text = "Stats";
            statsTmp.fontSize = 16;
            statsTmp.alignment = TextAlignmentOptions.TopLeft;
            statsTmp.color = new Color(0.9f, 0.95f, 1f);

            // Select Button
            var btnGo = new GameObject("Btn_Select", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(cardGo.transform, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, 30f);
            btnRect.sizeDelta = new Vector2(350f, 55f);

            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.2f, 0.65f, 0.95f, 0.90f);

            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.sizeDelta = new Vector2(350f, 55f);

            var btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>();
            btnTmp.text = "SELECT VENUE";
            btnTmp.fontSize = 18;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;

            int capturedIndex = index;
            var button = btnGo.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (LevelSelectionManager.Instance != null)
                {
                    LevelSelectionManager.Instance.RequestSelectLevelRpc(capturedIndex);
                }
            });

            _levelCards.Add(new LevelCardVisual
            {
                levelIndex = index,
                backgroundCard = cardImg,
                titleText = titleTmp,
                subtitleText = descTmp,
                statsText = statsTmp,
                starsText = starsTmp,
                selectButton = button,
                buttonLabel = btnTmp,
                activeBadge = badgeGo
            });
        }
    }
}

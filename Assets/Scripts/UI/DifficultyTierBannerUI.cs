using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Displays a banner on the screen when a new difficulty shift/tier begins.
    /// </summary>
    public class DifficultyTierBannerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform bannerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image accentBar;

        [Header("Tier Colors")]
        [SerializeField] private Color tier1Color = new Color(0.2f, 0.85f, 1f);   // Cyan
        [SerializeField] private Color tier2Color = new Color(1f, 0.75f, 0.15f);  // Gold
        [SerializeField] private Color tier3Color = new Color(1f, 0.4f, 0.15f);   // Orange
        [SerializeField] private Color tier4Color = new Color(1f, 0.15f, 0.35f);  // Neon Red

        private Coroutine _bannerCoroutine;

        private void Start()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            if (Difficulty.DifficultyManager.Instance != null)
            {
                Difficulty.DifficultyManager.Instance.OnDifficultyTierChanged += OnDifficultyTierChanged;
            }
        }

        private void OnDestroy()
        {
            if (Difficulty.DifficultyManager.Instance != null)
            {
                Difficulty.DifficultyManager.Instance.OnDifficultyTierChanged -= OnDifficultyTierChanged;
            }
        }

        private void OnDifficultyTierChanged(object sender, Difficulty.DifficultyManager.OnDifficultyTierChangedEventArgs e)
        {
            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(ShowBannerSequence(e));
        }

        private IEnumerator ShowBannerSequence(Difficulty.DifficultyManager.OnDifficultyTierChangedEventArgs e)
        {
            if (titleText != null) titleText.text = e.tierTitle.ToUpper();
            if (descriptionText != null) descriptionText.text = e.tierDescription;

            Color accentColor = e.newTier switch
            {
                Difficulty.DifficultyTier.Tier1_MorningPrep => tier1Color,
                Difficulty.DifficultyTier.Tier2_LunchRush   => tier2Color,
                Difficulty.DifficultyTier.Tier3_DinnerSurge => tier3Color,
                Difficulty.DifficultyTier.Tier4_Overdrive   => tier4Color,
                _ => tier1Color
            };

            if (accentBar != null) accentBar.color = accentColor;
            if (titleText != null) titleText.color = accentColor;

            // Fade in & scale up
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float progress = t / 0.35f;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                if (bannerRect != null)
                {
                    float scale = Mathf.Lerp(0.85f, 1f, Mathf.Sin(progress * Mathf.PI * 0.5f));
                    bannerRect.localScale = new Vector3(scale, scale, 1f);
                }
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (bannerRect != null) bannerRect.localScale = Vector3.one;

            // Hold display
            yield return new WaitForSeconds(3.5f);

            // Fade out
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float progress = t / 0.4f;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            _bannerCoroutine = null;
        }
    }
}

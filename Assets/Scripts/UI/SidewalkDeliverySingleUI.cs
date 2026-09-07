using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Displays a single sidewalk delivery order card in the DeliveryManagerUI layout.
    /// Shows the recipe name, ingredient icons, live countdown timer, and live distance to the customer.
    /// </summary>
    public class SidewalkDeliverySingleUI : MonoBehaviour
    {
        [Header("Recipe")]
        [SerializeField] private TextMeshProUGUI recipeNameText;
        [SerializeField] private Transform iconContainer;
        [SerializeField] private Transform iconTemplate;

        [Header("Delivery Status")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Image progressBarImage;
        [SerializeField] private Image accentBadgeImage;

        [Header("Color States")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.85f, 1f); // Cyan
        [SerializeField] private Color warnColor = new Color(1f, 0.75f, 0.15f);  // Gold / Orange
        [SerializeField] private Color urgentColor = new Color(1f, 0.25f, 0.25f); // Red

        private DeliveryCustomerManager.SidewalkDeliveryData _data;
        private RecipeSO _recipeSO;
        private bool _isInitialized;

        private void Awake()
        {
            if (iconTemplate != null) iconTemplate.gameObject.SetActive(false);
        }

        public void SetDeliveryData(DeliveryCustomerManager.SidewalkDeliveryData data, RecipeSO recipeSO)
        {
            _data = data;
            _recipeSO = recipeSO;
            _isInitialized = true;

            if (recipeNameText != null && _recipeSO != null)
            {
                recipeNameText.text = _recipeSO.recipeName;
            }

            if (iconContainer != null && iconTemplate != null && _recipeSO != null)
            {
                foreach (Transform child in iconContainer)
                {
                    if (child == iconTemplate) continue;
                    Destroy(child.gameObject);
                }

                foreach (KitchenObjectSO kitchenObjectSO in _recipeSO.kitchenObjectSOList)
                {
                    Transform iconTransform = Instantiate(iconTemplate, iconContainer);
                    iconTransform.gameObject.SetActive(true);
                    var img = iconTransform.GetComponent<Image>();
                    if (img != null) img.sprite = kitchenObjectSO.sprite;
                }
            }

            UpdateLiveVisuals();
        }

        private void Update()
        {
            if (!_isInitialized) return;
            UpdateLiveVisuals();
        }

        private void UpdateLiveVisuals()
        {
            float elapsed = Time.time - _data.orderStartTime;
            float remaining = Mathf.Max(0f, _data.orderDuration - elapsed);

            // Timer display
            if (timerText != null)
            {
                int secs = Mathf.CeilToInt(remaining);
                timerText.text = $"{secs}s";

                if (remaining <= 20f)
                    timerText.color = urgentColor;
                else if (remaining <= 45f)
                    timerText.color = warnColor;
                else
                    timerText.color = Color.white;
            }

            // Progress Bar
            if (progressBarImage != null && _data.orderDuration > 0f)
            {
                float fill = Mathf.Clamp01(remaining / _data.orderDuration);
                progressBarImage.fillAmount = fill;
                progressBarImage.color = remaining <= 20f ? urgentColor : (remaining <= 45f ? warnColor : normalColor);
            }

            // Distance calculation to local player
            if (distanceText != null)
            {
                if (Player.LocalInstance != null)
                {
                    float dist = Vector3.Distance(Player.LocalInstance.transform.position, _data.worldPosition);
                    distanceText.text = $"{Mathf.RoundToInt(dist)}m";
                }
                else
                {
                    distanceText.text = "";
                }
            }
        }
    }
}

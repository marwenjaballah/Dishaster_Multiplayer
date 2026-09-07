using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryManagerSingleUI : MonoBehaviour {

    [Header("Recipe")]
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Transform iconTemplate;

    [Header("Table Service & Timer")]
    [SerializeField] private TextMeshProUGUI tableNumberText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image progressBarImage;
    [SerializeField] private Image accentBadgeImage;

    [Header("Color Urgency")]
    [SerializeField] private Color normalColor = new Color(1f, 0.75f, 0.15f); // Warm Gold
    [SerializeField] private Color warnColor   = new Color(1f, 0.45f, 0.15f); // Orange
    [SerializeField] private Color urgentColor = new Color(1f, 0.2f, 0.2f);   // Red

    private CustomerTable _table;
    private RecipeSO _recipeSO;
    private TableOrder _tableOrder;
    private bool _hasTable;

    private void Awake() {
        if (iconTemplate != null) iconTemplate.gameObject.SetActive(false);
    }

    public void SetTableOrder(TableOrder tableOrder, RecipeSO recipeSO, CustomerTable table) {
        _tableOrder = tableOrder;
        _recipeSO = recipeSO;
        _table = table;
        _hasTable = table != null;

        if (recipeNameText != null && _recipeSO != null) {
            recipeNameText.text = _recipeSO.recipeName;
        }

        if (tableNumberText != null) {
            if (_hasTable) {
                tableNumberText.gameObject.SetActive(true);
                tableNumberText.text = $"{_table.GetDisplayNumber()}";
            } else {
                tableNumberText.gameObject.SetActive(false);
            }
        }

        PopulateIngredients();
        UpdateLiveVisuals();
    }

    public void SetRecipeSO(RecipeSO recipeSO) {
        _recipeSO = recipeSO;
        _hasTable = false;
        _table = null;

        if (recipeNameText != null && _recipeSO != null) {
            recipeNameText.text = _recipeSO.recipeName;
        }

        if (tableNumberText != null) {
            tableNumberText.gameObject.SetActive(false);
        }

        if (timerText != null) {
            timerText.gameObject.SetActive(false);
        }

        if (progressBarImage != null) {
            progressBarImage.gameObject.SetActive(false);
        }

        PopulateIngredients();
    }

    private void PopulateIngredients() {
        if (iconContainer == null || iconTemplate == null || _recipeSO == null) return;

        foreach (Transform child in iconContainer) {
            if (child == iconTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (KitchenObjectSO kitchenObjectSO in _recipeSO.kitchenObjectSOList) {
            Transform iconTransform = Instantiate(iconTemplate, iconContainer);
            iconTransform.gameObject.SetActive(true);
            var img = iconTransform.GetComponent<Image>();
            if (img != null) img.sprite = kitchenObjectSO.sprite;
        }
    }

    private void Update() {
        if (!_hasTable || _table == null) return;
        UpdateLiveVisuals();
    }

    private void UpdateLiveVisuals() {
        if (_table == null) return;

        float remaining = _table.GetRemainingTime();
        float duration = _table.GetOrderTimeoutDuration();
        float normalized = _table.GetPatienceNormalized();

        // Timer text
        if (timerText != null) {
            timerText.gameObject.SetActive(true);
            int secs = Mathf.CeilToInt(remaining);
            timerText.text = $"{secs}s";

            if (remaining <= 15f)
                timerText.color = urgentColor;
            else if (remaining <= 30f)
                timerText.color = warnColor;
            else
                timerText.color = Color.white;
        }

        // Progress bar
        if (progressBarImage != null) {
            progressBarImage.gameObject.SetActive(true);
            progressBarImage.fillAmount = normalized;
            progressBarImage.color = remaining <= 15f ? urgentColor : (remaining <= 30f ? warnColor : normalColor);
        }
    }
}

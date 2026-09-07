using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// HUD overlay displayed whenever the local player is holding a Delivery Bag.
    /// Shows the 3 plate storage slots with dish names, ingredient icons, and capacity.
    /// </summary>
    public class DeliveryBagUI : MonoBehaviour
    {
        [System.Serializable]
        public class BagSlotUI
        {
            public GameObject root;
            public GameObject activeContent;
            public GameObject emptyContent;
            public TextMeshProUGUI dishNameText;
            public Transform iconContainer;
            public Image iconTemplate;
        }

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private RecipeListSO recipeListSO;
        [SerializeField] private BagSlotUI[] slots;

        [Header("Settings")]
        [SerializeField] private float fadeSpeed = 8f;

        private DeliveryBagKitchenObject _currentBag;

        private void Start()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            // Hide templates
            if (slots != null)
            {
                foreach (var slot in slots)
                {
                    if (slot.iconTemplate != null) slot.iconTemplate.gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (Player.LocalInstance == null)
            {
                SetVisible(false);
                return;
            }

            // Check if local player is holding a DeliveryBagKitchenObject
            if (Player.LocalInstance.HasKitchenObject() && Player.LocalInstance.GetKitchenObject() is DeliveryBagKitchenObject bag)
            {
                _currentBag = bag;
                SetVisible(true);
                UpdateBagVisuals();
            }
            else
            {
                _currentBag = null;
                SetVisible(false);
            }
        }

        private void UpdateBagVisuals()
        {
            if (_currentBag == null) return;

            var dishes = _currentBag.GetStoredDishes();
            int count = dishes != null ? dishes.Count : 0;

            if (headerText != null)
            {
                headerText.text = $"DELIVERY BAG ({count}/3)";
            }

            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;

                if (i < count)
                {
                    // Slot is filled
                    if (slot.emptyContent != null) slot.emptyContent.SetActive(false);
                    if (slot.activeContent != null) slot.activeContent.SetActive(true);

                    var dishIngredients = dishes[i];
                    RecipeSO matchingRecipe = FindMatchingRecipe(dishIngredients);

                    if (slot.dishNameText != null)
                    {
                        slot.dishNameText.text = matchingRecipe != null ? matchingRecipe.recipeName : "Packed Dish";
                    }

                    // Render ingredient icons
                    if (slot.iconContainer != null && slot.iconTemplate != null)
                    {
                        // Clean previous icons
                        foreach (Transform child in slot.iconContainer)
                        {
                            if (child == slot.iconTemplate.transform) continue;
                            Destroy(child.gameObject);
                        }

                        // Add new icons
                        foreach (var ingredient in dishIngredients)
                        {
                            if (ingredient == null || ingredient.sprite == null) continue;
                            var icon = Instantiate(slot.iconTemplate, slot.iconContainer);
                            icon.gameObject.SetActive(true);
                            icon.sprite = ingredient.sprite;
                        }
                    }
                }
                else
                {
                    // Slot is empty
                    if (slot.activeContent != null) slot.activeContent.SetActive(false);
                    if (slot.emptyContent != null) slot.emptyContent.SetActive(true);
                }
            }
        }

        private RecipeSO FindMatchingRecipe(List<KitchenObjectSO> ingredients)
        {
            if (recipeListSO == null || ingredients == null) return null;

            foreach (var recipe in recipeListSO.recipeSOList)
            {
                if (recipe == null) continue;
                if (recipe.kitchenObjectSOList.Count != ingredients.Count) continue;

                bool allMatch = true;
                foreach (var ing in recipe.kitchenObjectSOList)
                {
                    if (!ingredients.Contains(ing))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch) return recipe;
            }

            return null;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            float target = visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, Time.deltaTime * fadeSpeed);
        }
    }
}

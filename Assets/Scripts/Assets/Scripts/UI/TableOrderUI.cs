using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays order information above a customer table as a world-space UI billboard.
/// Shows table number, recipe ingredients, and optional urgency indicator.
/// </summary>
public class TableOrderUI : MonoBehaviour {

    [Header("References")]
    [SerializeField] private TextMeshProUGUI tableNumberText;
    [SerializeField] private GameObject orderPanel;           // Panel shown when order is active
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Transform iconTemplate;
    [SerializeField] private Image timerBar;                     // Optional urgency bar (Phase 3)

    [Header("Colors")]
    [SerializeField] private Color orderActiveColor = Color.white;
    [SerializeField] private Color orderInactiveColor = Color.gray;

    [Header("Orientation")]
    [SerializeField] private bool faceCamera = true;             // Billboard effect

    private CustomerTable parentTable;
    private Camera mainCamera;


    private void Awake() {
        // Initialize template
        if (iconTemplate != null) {
            iconTemplate.gameObject.SetActive(false);
        }

        // Get parent table
        parentTable = GetComponentInParent<CustomerTable>();
        mainCamera = Camera.main;

        if (tableNumberText == null) {
            Debug.LogWarning("[TableOrderUI] 'tableNumberText' field is not assigned in the Inspector!");
        }
        if (orderPanel == null) {
            Debug.LogWarning("[TableOrderUI] 'orderPanel' field is not assigned in the Inspector!");
        }
    }

    private void Start() {
        // Initial update
        UpdateTableNumberDisplay();
        HideOrder();
    }

    private void LateUpdate() {
        // Billboard effect - always face camera
        if (faceCamera) {
            if (mainCamera == null) {
                mainCamera = Camera.main;
            }
            if (mainCamera != null) {
                transform.LookAt(transform.position + mainCamera.transform.forward);
            }
        }
    }

    // ===== PUBLIC METHODS =====

    // Called by CustomerTable when order is assigned
    public void ShowOrder(RecipeSO recipe, int tableNumber) {
        if (orderPanel != null) {
            orderPanel.gameObject.SetActive(true);
        }

        // Update recipe name
        if (recipeNameText != null && recipe != null) {
            recipeNameText.text = recipe.recipeName;
        }

        // Clear existing icons
        ClearIcons();

        // Add ingredient icons
        if (recipe != null && iconTemplate != null) {
            foreach (KitchenObjectSO ingredient in recipe.kitchenObjectSOList) {
                Transform iconTransform = Instantiate(iconTemplate, iconContainer);
                iconTransform.gameObject.SetActive(true);

                Image img = iconTransform.GetComponent<Image>();
                if (img != null && ingredient != null) {
                    img.sprite = ingredient.sprite;
                }
            }
        }

        // Update table number display and ensure its parent object is active & on top
        if (tableNumberText != null) {
            tableNumberText.gameObject.SetActive(true);
            if (tableNumberText.transform.parent != null) {
                tableNumberText.transform.parent.gameObject.SetActive(true);
                tableNumberText.transform.parent.SetAsLastSibling(); // Bring to front!
            }
            UpdateTableNumberDisplay();
        }

        // Set panel color to active
        SetPanelColor(orderActiveColor);
    }

    // Called when order is completed or cleared
    public void HideOrder() {
        if (orderPanel != null) {
            orderPanel.gameObject.SetActive(false);
        }

        // Make sure table number text AND its parent container (TableId bar) stay active & on top
        if (tableNumberText != null) {
            tableNumberText.gameObject.SetActive(true);
            if (tableNumberText.transform.parent != null) {
                tableNumberText.transform.parent.gameObject.SetActive(true);
                tableNumberText.transform.parent.SetAsLastSibling(); // Bring to front!
            }
            UpdateTableNumberDisplay();
        }

        // Set panel color to inactive
        SetPanelColor(orderInactiveColor);
    }

    // Update the table number text
    public void UpdateTableNumber(int newNumber) {
        if (tableNumberText != null) {
            tableNumberText.text = newNumber.ToString();
        }
    }

    [Header("Delivery Result Popup (Per Table)")]
    [SerializeField] private GameObject deliveryResultPanel;
    [SerializeField] private Image resultBackgroundImage;
    [SerializeField] private Image resultIconImage;
    [SerializeField] private TextMeshProUGUI resultMessageText;
    [SerializeField] private Color resultSuccessColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
    [SerializeField] private Color resultFailedColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Sprite resultSuccessSprite;
    [SerializeField] private Sprite resultFailedSprite;

    private const string POPUP = "Popup";
    private Coroutine resultPopupCoroutine;

    // Update timer bar (used in Phase 3 for urgency)
    public void UpdateTimerBar(float normalizedTime) {
        if (timerBar != null) {
            timerBar.fillAmount = Mathf.Clamp01(normalizedTime);

            // Color changes: green -> yellow -> red
            Color timerColor = Color.Lerp(Color.green, Color.red, 1f - normalizedTime);
            timerBar.color = timerColor;
        }
    }

    public void ShowDeliveryResult(bool success) {
        if (deliveryResultPanel == null) return;

        deliveryResultPanel.SetActive(true);

        // Look for Animator on panel or any of its children (handles DeliveryResultUI_LookAtCamera wrapper)
        Animator animator = deliveryResultPanel.GetComponentInChildren<Animator>(true);
        if (animator != null) {
            animator.gameObject.SetActive(true);
            animator.SetTrigger(POPUP);
        } else {
            Debug.LogWarning("[TableOrderUI] Could not find Animator component on deliveryResultPanel or its children!");
        }

        if (resultBackgroundImage != null) {
            resultBackgroundImage.color = success ? resultSuccessColor : resultFailedColor;
        }
        if (resultIconImage != null && (success ? resultSuccessSprite != null : resultFailedSprite != null)) {
            resultIconImage.sprite = success ? resultSuccessSprite : resultFailedSprite;
        }
        if (resultMessageText != null) {
            resultMessageText.text = success ? "DELIVERY\nSUCCESS" : "DELIVERY\nFAILED";
        }

        if (resultPopupCoroutine != null) {
            StopCoroutine(resultPopupCoroutine);
        }
        resultPopupCoroutine = StartCoroutine(HideDeliveryResultRoutine(1.5f));
    }

    private IEnumerator HideDeliveryResultRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        if (deliveryResultPanel != null) {
            deliveryResultPanel.SetActive(false);
        }
    }

    // ===== PRIVATE METHODS =====

    private void ClearIcons() {
        if (iconContainer == null) return;

        foreach (Transform child in iconContainer) {
            if (child == iconTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void UpdateTableNumberDisplay() {
        if (tableNumberText != null && parentTable != null) {
            tableNumberText.text = parentTable.GetDisplayNumber().ToString();
        }
    }

    private void SetPanelColor(Color color) {
        if (orderPanel != null) {
            Image panelImage = orderPanel.GetComponent<Image>();
            if (panelImage != null) {
                panelImage.color = color;
            }
        }
    }
}
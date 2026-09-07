using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// HUD navigation overlay placed at the top of the screen.
    /// Rotates a directional arrow towards the nearest active delivery customer
    /// and displays the target meal name and distance in meters.
    /// </summary>
    public class DeliveryNavigationUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform arrowRectTransform;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image arrowImage;

        [Header("Settings")]
        [SerializeField] private float fadeSpeed = 6f;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.9f, 1f, 1f); // Cyan
        [SerializeField] private Color nearColor = new Color(0.2f, 1f, 0.4f, 1f);   // Green

        private void Start()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (Player.LocalInstance == null || Camera.main == null)
            {
                SetVisible(false);
                return;
            }

            DeliveryCustomer targetCustomer = FindClosestActiveCustomer();
            if (targetCustomer == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            UpdateNavigation(targetCustomer);
        }

        private DeliveryCustomer FindClosestActiveCustomer()
        {
            if (DeliveryCustomerManager.Instance == null) return null;

            List<DeliveryCustomer> activeList = DeliveryCustomerManager.Instance.GetActiveCustomers();
            if (activeList == null || activeList.Count == 0) return null;

            Vector3 playerPos = Player.LocalInstance.transform.position;
            DeliveryCustomer closest = null;
            float minDistSqr = float.MaxValue;

            foreach (var cust in activeList)
            {
                if (cust == null || !cust.IsActive()) continue;

                float distSqr = (cust.transform.position - playerPos).sqrMagnitude;
                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closest = cust;
                }
            }

            return closest;
        }

        private void UpdateNavigation(DeliveryCustomer customer)
        {
            Vector3 playerPos = Player.LocalInstance.transform.position;
            Vector3 targetPos = customer.transform.position;

            Vector3 toTarget = targetPos - playerPos;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            // Compute angle relative to camera view
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0f;

            if (camForward.sqrMagnitude > 0.001f && toTarget.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.SignedAngle(camForward.normalized, toTarget.normalized, Vector3.up);
                if (arrowRectTransform != null)
                {
                    arrowRectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
                }
            }

            // Update text label
            if (distanceText != null)
            {
                string recipeName = customer.GetRecipe() != null ? customer.GetRecipe().recipeName : "Customer";
                distanceText.text = $"{recipeName} • {Mathf.RoundToInt(distance)}m";
            }

            // Proximity pulse
            if (arrowImage != null)
            {
                bool isNear = distance <= 6f;
                arrowImage.color = isNear ? nearColor : normalColor;

                if (isNear)
                {
                    float scale = 1f + Mathf.PingPong(Time.time * 3f, 0.25f);
                    arrowRectTransform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    arrowRectTransform.localScale = Vector3.one;
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            float targetAlpha = visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }
}

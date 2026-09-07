using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Manager UI for sidewalk / home delivery orders.
    /// Replicates the DeliveryManagerUI layout directly underneath the kitchen/table orders,
    /// displaying active outdoor deliveries with ingredients, timers, and destination distance.
    /// </summary>
    public class SidewalkDeliveryManagerUI : MonoBehaviour
    {
        [Header("UI Structure")]
        [SerializeField] private Transform container;
        [SerializeField] private Transform recipeTemplate;
        [SerializeField] private GameObject headerObject;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private bool hideWhenEmpty = false;

        private void Awake()
        {
            if (recipeTemplate != null)
            {
                recipeTemplate.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            SubscribeEvents();
            UpdateVisual();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            UpdateVisual();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (DeliveryCustomerManager.Instance != null)
            {
                DeliveryCustomerManager.Instance.OnDeliveryOrdersChanged -= DeliveryCustomerManager_OnDeliveryOrdersChanged;
                DeliveryCustomerManager.Instance.OnDeliveryOrdersChanged += DeliveryCustomerManager_OnDeliveryOrdersChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (DeliveryCustomerManager.Instance != null)
            {
                DeliveryCustomerManager.Instance.OnDeliveryOrdersChanged -= DeliveryCustomerManager_OnDeliveryOrdersChanged;
            }
        }

        private void DeliveryCustomerManager_OnDeliveryOrdersChanged(object sender, System.EventArgs e)
        {
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (container == null || recipeTemplate == null) return;

            // Clear old dynamic card instances
            foreach (Transform child in container)
            {
                if (child == recipeTemplate) continue;
                Destroy(child.gameObject);
            }

            if (DeliveryCustomerManager.Instance == null)
            {
                if (headerObject != null && hideWhenEmpty) headerObject.SetActive(false);
                return;
            }

            var deliveryList = DeliveryCustomerManager.Instance.GetActiveDeliveryDataList();
            int count = deliveryList != null ? deliveryList.Count : 0;

            if (headerObject != null && hideWhenEmpty)
            {
                headerObject.SetActive(count > 0);
            }

            if (count == 0) return;

            for (int i = 0; i < deliveryList.Count; i++)
            {
                var data = deliveryList[i];
                RecipeSO recipeSO = DeliveryCustomerManager.Instance.GetRecipeSO(data.recipeSOIndex);
                if (recipeSO == null) continue;

                Transform cardTransform = Instantiate(recipeTemplate, container);
                cardTransform.gameObject.SetActive(true);

                var singleUI = cardTransform.GetComponent<SidewalkDeliverySingleUI>();
                if (singleUI != null)
                {
                    singleUI.SetDeliveryData(data, recipeSO);
                }
            }
        }
    }
}

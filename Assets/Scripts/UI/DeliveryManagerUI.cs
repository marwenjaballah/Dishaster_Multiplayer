using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManagerUI : MonoBehaviour {

    [SerializeField] private Transform container;
    [SerializeField] private Transform recipeTemplate;

    private void Awake() {
        if (recipeTemplate != null) {
            recipeTemplate.gameObject.SetActive(false);
        }
    }

    private void Start() {
        if (DeliveryManager.Instance != null) {
            DeliveryManager.Instance.OnRecipeSpawned += DeliveryManager_OnRecipeSpawned;
            DeliveryManager.Instance.OnRecipeCompleted += DeliveryManager_OnRecipeCompleted;
        }

        if (TableManager.Instance != null) {
            TableManager.Instance.OnActiveOrdersListChanged += TableManager_OnActiveOrdersListChanged;
        }

        UpdateVisual();
    }

    private void OnDestroy() {
        if (DeliveryManager.Instance != null) {
            DeliveryManager.Instance.OnRecipeSpawned -= DeliveryManager_OnRecipeSpawned;
            DeliveryManager.Instance.OnRecipeCompleted -= DeliveryManager_OnRecipeCompleted;
        }

        if (TableManager.Instance != null) {
            TableManager.Instance.OnActiveOrdersListChanged -= TableManager_OnActiveOrdersListChanged;
        }
    }

    private void TableManager_OnActiveOrdersListChanged(object sender, EventArgs e) {
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeCompleted(object sender, EventArgs e) {
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeSpawned(object sender, EventArgs e) {
        UpdateVisual();
    }

    private void UpdateVisual() {
        if (container == null || recipeTemplate == null) return;

        foreach (Transform child in container) {
            if (child == recipeTemplate) continue;
            Destroy(child.gameObject);
        }

        // 1. Table Service Mode / TableManager Orders
        if (TableManager.Instance != null && TableManager.Instance.GetActiveOrders() != null && TableManager.Instance.GetActiveOrders().Count > 0) {
            var activeOrders = TableManager.Instance.GetActiveOrders();
            var recipeListSO = TableManager.Instance.GetRecipeListSO();

            for (int i = 0; i < activeOrders.Count; i++) {
                TableOrder tableOrder = activeOrders[i];
                CustomerTable table = TableManager.Instance.GetTableById(tableOrder.tableId.ToString());
                RecipeSO recipeSO = null;
                if (recipeListSO != null && tableOrder.recipeSOIndex >= 0 && tableOrder.recipeSOIndex < recipeListSO.recipeSOList.Count) {
                    recipeSO = recipeListSO.recipeSOList[tableOrder.recipeSOIndex];
                }

                if (recipeSO == null) continue;

                Transform recipeTransform = Instantiate(recipeTemplate, container);
                recipeTransform.gameObject.SetActive(true);

                var singleUI = recipeTransform.GetComponent<DeliveryManagerSingleUI>();
                if (singleUI != null) {
                    singleUI.SetTableOrder(tableOrder, recipeSO, table);
                }
            }
            return;
        }

        // 2. Classic Delivery Mode Fallback
        if (DeliveryManager.Instance != null && DeliveryManager.Instance.GetWaitingRecipeSOList() != null) {
            foreach (RecipeSO recipeSO in DeliveryManager.Instance.GetWaitingRecipeSOList()) {
                Transform recipeTransform = Instantiate(recipeTemplate, container);
                recipeTransform.gameObject.SetActive(true);

                var singleUI = recipeTransform.GetComponent<DeliveryManagerSingleUI>();
                if (singleUI != null) {
                    singleUI.SetRecipeSO(recipeSO);
                }
            }
        }
    }
}

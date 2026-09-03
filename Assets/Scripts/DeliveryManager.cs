using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour {

    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;


    public static DeliveryManager Instance { get; private set; }


    [SerializeField] private RecipeListSO recipeListSO;


    private List<RecipeSO> waitingRecipeSOList;
    private float spawnRecipeTimer = 4f;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;
    private int successfulRecipesAmount;


    private void Awake() {
        Instance = this;

        waitingRecipeSOList = new List<RecipeSO>();
    }

    private void Update() {
        if (!IsServer) {
            return;
        }

        // Classic mode only – TableManager handles orders in Table Service mode
        if (KitchenGameMultiplayer.tableServiceMode) {
            return;
        }

        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f) {
            spawnRecipeTimer = spawnRecipeTimerMax;

            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax) {
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);

                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }

    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex) {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];

        waitingRecipeSOList.Add(waitingRecipeSO);

        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public void DeliverRecipe(PlateKitchenObject plateKitchenObject) {
        for (int i = 0; i < waitingRecipeSOList.Count; i++) {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count) {
                // Has the same number of ingredients
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                    // Cycling through all ingredients in the Recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList()) {
                        // Cycling through all ingredients in the Plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                            // Ingredient matches!
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound) {
                        // This Recipe ingredient was not found on the Plate
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe) {
                    // Player delivered the correct recipe!
                    DeliverCorrectRecipeServerRpc(i);
                    return;
                }
            }
        }

        // No matches found!
        // Player did not deliver a correct recipe
        DeliverIncorrectRecipeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverIncorrectRecipeServerRpc() {
        DeliverIncorrectRecipeClientRpc();
    }

    [ClientRpc]
    private void DeliverIncorrectRecipeClientRpc() {
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverCorrectRecipeServerRpc(int waitingRecipeSOListIndex) {
        DeliverCorrectRecipeClientRpc(waitingRecipeSOListIndex);
    }

    [ClientRpc]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOListIndex) {
        successfulRecipesAmount++;

        waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);

        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSO> GetWaitingRecipeSOList() {
        return waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount() {
        return successfulRecipesAmount;
    }

    // ===== TABLE SERVICE MODE NOTIFICATIONS =====
    // Called by TableManager (server-side). ClientRpcs so events fire on all clients
    // – sound, delivery result popup, and score all work automatically.

    [ClientRpc]
    public void NotifyTableServiceRecipeSpawnedClientRpc(int recipeSOIndex) {
        if (recipeListSO != null && recipeSOIndex >= 0 && recipeSOIndex < recipeListSO.recipeSOList.Count) {
            RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[recipeSOIndex];
            waitingRecipeSOList.Add(waitingRecipeSO);
        }
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    [ClientRpc]
    public void NotifyTableServiceSuccessClientRpc(string tableId, int recipeSOIndex) {
        successfulRecipesAmount++;
        Debug.Log($"[DeliveryManager] Table service SUCCESS RPC received for tableId: {tableId}");

        if (recipeListSO != null && recipeSOIndex >= 0 && recipeSOIndex < recipeListSO.recipeSOList.Count) {
            RecipeSO completedRecipeSO = recipeListSO.recipeSOList[recipeSOIndex];
            waitingRecipeSOList.Remove(completedRecipeSO);
        }

        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);

        if (TableManager.Instance != null) {
            CustomerTable table = TableManager.Instance.GetTableById(tableId);
            if (table != null) {
                table.ShowDeliveryResult(true);
            }
        }
    }

    [ClientRpc]
    public void NotifyTableServiceFailedClientRpc(string tableId, int recipeSOIndex) {
        Debug.Log($"[DeliveryManager] Table service FAILED RPC received for tableId: {tableId}");

        if (recipeListSO != null && recipeSOIndex >= 0 && recipeSOIndex < recipeListSO.recipeSOList.Count) {
            RecipeSO failedRecipeSO = recipeListSO.recipeSOList[recipeSOIndex];
            waitingRecipeSOList.Remove(failedRecipeSO);
        }

        OnRecipeFailed?.Invoke(this, EventArgs.Empty);

        if (TableManager.Instance != null) {
            CustomerTable table = TableManager.Instance.GetTableById(tableId);
            if (table != null) {
                table.ShowDeliveryResult(false);
            }
        }
    }

}

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A special transportable Delivery Bag KitchenObject that can hold up to 3 plated meals.
/// </summary>
public class DeliveryBagKitchenObject : KitchenObject
{
    public const int MAX_PLATES = 3;

    public event EventHandler OnBagContentsChanged;

    // List of dishes stored in the bag. Each dish is a list of KitchenObjectSO ingredients.
    private readonly List<List<KitchenObjectSO>> _storedDishes = new();

    public int StoredCount => _storedDishes.Count;
    public bool CanAddPlate => _storedDishes.Count < MAX_PLATES;

    /// <summary>
    /// Attempts to add a PlateKitchenObject to the bag.
    /// </summary>
    public bool TryAddPlate(PlateKitchenObject plate)
    {
        if (!CanAddPlate) return false;
        if (plate == null) return false;

        var ingredients = plate.GetKitchenObjectSOList();
        if (ingredients == null || ingredients.Count == 0) return false;

        // Convert ingredients to indices for RPC
        int[] indices = new int[ingredients.Count];
        for (int i = 0; i < ingredients.Count; i++)
        {
            indices[i] = KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(ingredients[i]);
        }

        AddDishServerRpc(indices);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddDishServerRpc(int[] ingredientIndices)
    {
        AddDishClientRpc(ingredientIndices);
    }

    [ClientRpc]
    private void AddDishClientRpc(int[] ingredientIndices)
    {
        var dish = new List<KitchenObjectSO>();
        foreach (int idx in ingredientIndices)
        {
            var so = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(idx);
            if (so != null) dish.Add(so);
        }

        _storedDishes.Add(dish);
        OnBagContentsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Checks if the bag contains a dish matching the given recipe.
    /// </summary>
    public bool HasMatchingDish(RecipeSO recipe)
    {
        return FindMatchingDishIndex(recipe) >= 0;
    }

    public int FindMatchingDishIndex(RecipeSO recipe)
    {
        if (recipe == null) return -1;
        var recipeIngredients = recipe.kitchenObjectSOList;

        for (int i = 0; i < _storedDishes.Count; i++)
        {
            var dish = _storedDishes[i];
            if (dish.Count != recipeIngredients.Count) continue;

            bool allMatch = true;
            foreach (var ing in recipeIngredients)
            {
                if (!dish.Contains(ing))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch) return i;
        }

        return -1;
    }

    /// <summary>
    /// Removes and consumes the matching dish from the bag (server-authoritative).
    /// </summary>
    public bool TryConsumeMatchingDish(RecipeSO recipe)
    {
        int index = FindMatchingDishIndex(recipe);
        if (index < 0) return false;

        RemoveDishServerRpc(index);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveDishServerRpc(int dishIndex)
    {
        RemoveDishClientRpc(dishIndex);
    }

    [ClientRpc]
    private void RemoveDishClientRpc(int dishIndex)
    {
        if (dishIndex >= 0 && dishIndex < _storedDishes.Count)
        {
            _storedDishes.RemoveAt(dishIndex);
            OnBagContentsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Header("Backpack Offsets")]
    [SerializeField] private Vector3 playerBackOffset = new Vector3(0f, 0.85f, -0.78f);
    [SerializeField] private Vector3 playerBackRotation = Vector3.zero;

    private FollowTransform _followTransform;

    protected override void Awake()
    {
        base.Awake();
        _followTransform = GetComponent<FollowTransform>();
    }

    private void LateUpdate()
    {
        if (GetKitchenObjectParent() is Player player)
        {
            // Disable FollowTransform so it doesn't snap to the front hand hold point
            if (_followTransform == null) _followTransform = GetComponent<FollowTransform>();
            if (_followTransform != null && _followTransform.enabled)
            {
                _followTransform.enabled = false;
            }

            // Position and align the delivery bag cleanly on the back surface of the player
            Vector3 worldOffset = player.transform.rotation * playerBackOffset;
            transform.position = player.transform.position + worldOffset;
            transform.rotation = player.transform.rotation * Quaternion.Euler(playerBackRotation);
        }
        else
        {
            // Re-enable FollowTransform when placed on a counter or table
            if (_followTransform != null && !_followTransform.enabled)
            {
                _followTransform.enabled = true;
            }
        }
    }

    public List<List<KitchenObjectSO>> GetStoredDishes() => _storedDishes;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class RecipeSO : ScriptableObject {


    public List<KitchenObjectSO> kitchenObjectSOList;
    public string recipeName;

    [Header("Combo Settings")]
    public bool isComboMeal;
    public int comboBonusPayout = 20;
    public float comboTimeBonus = 20f;
}
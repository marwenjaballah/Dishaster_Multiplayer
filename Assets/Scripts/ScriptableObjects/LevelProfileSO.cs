using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration profile defining restaurant level parameters, rent difficulty curves,
/// courier distance scaling, customer patience, and signature recipes.
/// </summary>
[CreateAssetMenu(fileName = "LevelProfile", menuName = "Dishaster/Level Profile")]
public class LevelProfileSO : ScriptableObject
{
    public string levelName = "The Sunny Side Diner";
    [TextArea(2, 4)]
    public string description = "Cozy suburban diner with a traditional counter pass and sidewalk patio.";
    public int baseDailyRent = 150;
    public int targetRevenueGoal = 600;
    public float deliveryDistanceScale = 1.0f;
    public float customerPatienceMultiplier = 1.0f;
    public List<RecipeSO> availableRecipes = new List<RecipeSO>();
    public Color themeAccentColor = new Color(0.95f, 0.65f, 0.15f);
}

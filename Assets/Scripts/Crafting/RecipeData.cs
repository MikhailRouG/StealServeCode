using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Ingredient
{
    public ItemData Item;
    [Min(1)] public int Amount = 1;
}

/// <summary>
/// Recipe: which items (and how many) are spent to cook the result dish. Cooked by <see cref="TruckKitchen"/>.
/// Create via Assets > Create > Crafting > Recipe Data.
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [SerializeField] private DishData _result;
    [SerializeField, Min(1)] private int _resultAmount = 1;
    [SerializeField] private List<Ingredient> _ingredients = new List<Ingredient>();

    public DishData Result => _result;
    public int ResultAmount => _resultAmount;
    public IReadOnlyList<Ingredient> Ingredients => _ingredients;

    /// <summary>Name shown in the UI. Falls back to the asset name if there is no result item.</summary>
    public string DisplayName => _result != null ? _result.ItemName : name;

    public bool CanCraft(CarStorage storage)
    {
        if (storage == null || _result == null)
            return false;

        foreach (Ingredient ingredient in _ingredients)
        {
            if (ingredient.Item != null && storage.GetCount(ingredient.Item) < ingredient.Amount)
                return false;
        }

        return true;
    }

    /// <summary>Spends the ingredients from the storage. Returns false (spending nothing) if there aren't enough.</summary>
    public bool TrySpendIngredients(CarStorage storage)
    {
        if (!CanCraft(storage))
            return false;

        foreach (Ingredient ingredient in _ingredients)
        {
            if (ingredient.Item != null)
                storage.Remove(ingredient.Item, ingredient.Amount);
        }

        return true;
    }
}

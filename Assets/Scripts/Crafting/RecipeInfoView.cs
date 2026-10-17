using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// How an item is cooked, shown on the storage tab of <see cref="CraftingWindow"/>:
/// item name and icon, and the ingredients of its recipe with "have/need" counters.
/// </summary>
public class RecipeInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _icon;
    [SerializeField] private IngredientView _ingredientPrefab;
    [SerializeField] private Transform _ingredientsContainer;
    [Tooltip("Optional caption above the ingredients, hidden when there is no recipe.")]
    [SerializeField] private GameObject _ingredientsLabel;
    [Tooltip("Optional label shown when the item has no recipe.")]
    [SerializeField] private GameObject _noRecipeLabel;

    private readonly List<IngredientView> _ingredients = new List<IngredientView>();

    /// <param name="recipe">Recipe that makes <paramref name="item"/>, or null if it can't be cooked.</param>
    public void Show(ItemData item, RecipeData recipe, CarStorage storage)
    {
        _nameText.text = item != null ? item.ItemName : string.Empty;
        _icon.sprite = item != null ? item.Icon : null;
        _icon.enabled = _icon.sprite != null;

        if (_ingredientsLabel != null)
            _ingredientsLabel.SetActive(recipe != null);
        if (_noRecipeLabel != null)
            _noRecipeLabel.SetActive(item != null && recipe == null);

        int count = recipe != null ? recipe.Ingredients.Count : 0;

        while (_ingredients.Count < count)
            _ingredients.Add(Instantiate(_ingredientPrefab, _ingredientsContainer));

        for (int i = 0; i < _ingredients.Count; i++)
        {
            bool visible = i < count;
            _ingredients[i].gameObject.SetActive(visible);
            if (!visible)
                continue;

            Ingredient ingredient = recipe.Ingredients[i];
            int have = storage != null && ingredient.Item != null ? storage.GetCount(ingredient.Item) : 0;
            _ingredients[i].Init(ingredient, have);
        }
    }
}

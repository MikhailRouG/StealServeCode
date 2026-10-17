using UnityEngine;

/// <summary>
/// Cooked food: the result of a <see cref="RecipeData"/>. Once cooked (see <see cref="TruckKitchen"/>)
/// it goes straight onto the <see cref="TruckShowcase"/>, where clients buy it.
/// Create via Assets > Create > Items > Dish Data.
/// </summary>
[CreateAssetMenu(fileName = "NewDish", menuName = "Items/Dish Data")]
public class DishData : ItemData
{
    [Header("Cooking")]
    [Tooltip("Seconds it takes to cook the dish.")]
    [SerializeField, Min(0f)] private float _cookTime = 5f;

    [Header("Sale")]
    [Tooltip("Money the player gets when a client buys the dish.")]
    [SerializeField, Min(0)] private int _price = 10;
    [Tooltip("How much clients want the dish: the higher, the more often they choose it.")]
    [SerializeField, Range(0, 100)] private int _interest = 0;

    public float CookTime => _cookTime;
    public int Price => _price;
    public int Interest => _interest;
}

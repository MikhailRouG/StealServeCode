using System;
using UnityEngine;

/// <summary>
/// Put on the <see cref="HubZone"/>: cooks one recipe at a time. Ingredients are taken from the hub's storage
/// when cooking starts; after the dish's <see cref="DishData.CookTime"/> the dishes go straight onto the
/// <see cref="TruckShowcase"/>. If the showcase is full, the ready dishes wait until a point frees up.
/// Started from the <see cref="CraftingWindow"/>.
/// </summary>
[RequireComponent(typeof(HubZone))]
[RequireComponent(typeof(TruckShowcase))]
public class TruckKitchen : MonoBehaviour
{
    private HubZone _hubZone;
    private TruckShowcase _showcase;
    private float _readyTime;
    private int _dishesLeft;

    /// <summary>The recipe being cooked, or null.</summary>
    public RecipeData Current { get; private set; }
    public bool IsCooking => Current != null;
    /// <summary>Seconds until the current dish is ready; 0 once it is waiting for a free showcase point.</summary>
    public float TimeLeft => IsCooking ? Mathf.Max(0f, _readyTime - Time.time) : 0f;
    /// <summary>True while ready dishes wait for a free showcase point.</summary>
    public bool IsWaitingForShowcase => IsCooking && TimeLeft <= 0f;
    public CarStorage Storage => _hubZone.Storage;

    /// <summary>Raised when cooking starts or finishes.</summary>
    public event Action Changed;

    private void Awake()
    {
        _hubZone = GetComponent<HubZone>();
        _showcase = GetComponent<TruckShowcase>();
    }

    private void Update()
    {
        if (!IsCooking || Time.time < _readyTime)
            return;

        while (_dishesLeft > 0 && _showcase.TryPlace(Current.Result))
            _dishesLeft--;

        if (_dishesLeft > 0)
            return;

        Current = null;
        Changed?.Invoke();
    }

    public bool CanCook(RecipeData recipe) => !IsCooking && recipe != null && recipe.CanCraft(Storage);

    /// <summary>Spends the ingredients and starts cooking. Returns false if busy or not enough ingredients.</summary>
    public bool TryCook(RecipeData recipe)
    {
        if (!CanCook(recipe) || !recipe.TrySpendIngredients(Storage))
            return false;

        Current = recipe;
        _dishesLeft = recipe.ResultAmount;
        _readyTime = Time.time + recipe.Result.CookTime;
        Changed?.Invoke();
        return true;
    }
}

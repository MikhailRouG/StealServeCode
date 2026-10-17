using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One cell of the recipe grid: shows the result icon and highlights itself when selected.
/// </summary>
[RequireComponent(typeof(Button))]
public class RecipeSlotView : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [Tooltip("Optional frame shown only while the slot is selected.")]
    [SerializeField] private GameObject _selectedFrame;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _selectedColor = new Color32(0xF7, 0xD3, 0x9A, 0xFF);

    private Button _button;

    public RecipeData Recipe { get; private set; }

    /// <summary>Raised with this slot when it is clicked.</summary>
    public event Action<RecipeSlotView> Clicked;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    public void Init(RecipeData recipe)
    {
        Recipe = recipe;
        ItemData result = recipe != null ? recipe.Result : null;

        _icon.sprite = result != null ? result.Icon : null;
        _icon.enabled = _icon.sprite != null;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (_background != null)
            _background.color = selected ? _selectedColor : _normalColor;

        if (_selectedFrame != null)
            _selectedFrame.SetActive(selected);
    }

    private void OnClick()
    {
        Clicked?.Invoke(this);
    }
}

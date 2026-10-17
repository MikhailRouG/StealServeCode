using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ingredient of the selected recipe: icon and "have/need" counter, green if enough, red if not.
/// </summary>
public class IngredientView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Color _enoughColor = new Color32(0x3E, 0x8E, 0x5A, 0xFF);
    [SerializeField] private Color _notEnoughColor = new Color32(0xD9, 0x4F, 0x3D, 0xFF);

    public void Init(Ingredient ingredient, int have)
    {
        _icon.sprite = ingredient.Item != null ? ingredient.Item.Icon : null;
        _icon.enabled = _icon.sprite != null;

        _countText.text = $"{have}/{ingredient.Amount}";
        _countText.color = have >= ingredient.Amount ? _enoughColor : _notEnoughColor;
    }
}

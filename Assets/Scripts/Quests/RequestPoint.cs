using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Request point: asks the player for the items of a <see cref="ContractData"/> (e.g. 15 apples).
/// When the player enters the trigger, takes as many requested items from the inventory as they have,
/// up to the remaining amount. On completion pays the reward and turns itself off.
/// Shows the item icon and a "delivered / required" counter.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RequestPoint : MonoBehaviour
{
    [SerializeField] private ContractData _contract;

    [Header("Display")]
    [Tooltip("UI Image for the icon. The sprite is taken from the contract's item.")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _countText;

    public int DeliveredAmount { get; private set; }
    public int RemainingAmount => _contract.RequiredAmount - DeliveredAmount;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {

        if (_iconImage != null && _contract != null && _contract.RequiredItem != null)
            _iconImage.sprite = _contract.RequiredItem.Icon;

        UpdateText();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
            TakeItems(player);
    }

    private void TakeItems(Player player)
    {
        if (_contract == null || _contract.RequiredItem == null)
            return;

        int amount = 0;
        while (amount < RemainingAmount && player.Inventory.Remove(_contract.RequiredItem))
            amount++;

        if (amount == 0)
            return;

        DeliveredAmount += amount;
        UpdateText();

        if (RemainingAmount <= 0)
        {
            player.Wallet.Add(_contract.Reward);
            player.Level.AddExperience(_contract.Experience);
            gameObject.SetActive(false);
        }
    }

    private void UpdateText()
    {
        if (_countText != null && _contract != null)
            _countText.text = $"{DeliveredAmount} / {_contract.RequiredAmount}";
    }

}

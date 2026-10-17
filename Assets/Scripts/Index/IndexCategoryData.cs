using UnityEngine;

/// <summary>
/// One page of the <see cref="IndexWindow"/> (e.g. "Forest"): its items and the reward for discovering all of them.
/// An item counts as discovered once it has been delivered to the hub (see <see cref="PlayerDiscoveries"/>).
/// Create via Assets > Create > Index > Category.
/// </summary>
[CreateAssetMenu(fileName = "NewIndexCategory", menuName = "Index/Category")]
public class IndexCategoryData : ScriptableObject
{
    [SerializeField] private string _title = "Forest";
    [Tooltip("Picture on the left of the page. If empty, the color is shown.")]
    [SerializeField] private Sprite _picture;
    [SerializeField] private Color _color = new Color32(0x3C, 0x9A, 0x3C, 0xFF);
    [SerializeField] private ItemData[] _items;

    [Header("Reward for discovering every item")]
    [SerializeField, Min(0)] private int _rewardMoney = 100;
    [SerializeField, Min(0)] private int _rewardSpeedStat = 420;

    public string Title => _title;
    public Sprite Picture => _picture;
    public Color Color => _color;
    public ItemData[] Items => _items ?? System.Array.Empty<ItemData>();
    public int RewardMoney => _rewardMoney;
    public int RewardSpeedStat => _rewardSpeedStat;

    public int CountDiscovered(PlayerDiscoveries discoveries)
    {
        int count = 0;
        foreach (ItemData item in Items)
        {
            if (item != null && discoveries.HasDelivered(item))
                count++;
        }

        return count;
    }

    public bool IsComplete(PlayerDiscoveries discoveries) => Items.Length > 0 && CountDiscovered(discoveries) == Items.Length;
}

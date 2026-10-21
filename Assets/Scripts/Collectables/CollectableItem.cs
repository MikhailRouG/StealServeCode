using R3;
using TMPro;
using UnityEngine;

/// <summary>
/// Item capsule: a pickup that shows the model and level ("Lvl N") of its <see cref="ItemData"/> and disappears when picked up.
/// The item models themselves are plain visuals; this capsule provides the collider and pickup logic.
/// Needs a Collider with "Is Trigger" enabled.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectableItem : MonoBehaviour, ICollectable
{
    [SerializeField] private ItemData _data;
    [SerializeField] private GameObject _pickupEffect;
    [Tooltip("Shows the item's level as \"Lvl N\".")]
    [SerializeField] private TMP_Text _levelText;
    [Tooltip("How many times the item model is scaled up inside the capsule (1 = the model's own scale).")]
    [SerializeField, Min(0.01f)] private float _modelScale = 1f;

    private readonly Subject<CollectableItem> _collected = new Subject<CollectableItem>();
    private bool _isCollected;
    private ItemData _shownData;
    private GameObject _model;

    public ItemData Data => _data;
    public bool IsAvailable => !_isCollected;

    /// <summary>If false, the item is only deactivated on pickup so it can be reused (e.g. by a spawner).</summary>
    public bool DestroyOnCollect { get; set; } = true;

    /// <summary>Fires every time the item is picked up. Completes when the item is destroyed.</summary>
    public Observable<CollectableItem> Collected => _collected;

    private void OnEnable()
    {
        _isCollected = false;
    }

    private void OnDestroy()
    {
        _collected.Dispose();
    }

    private void Start()
    {
        RefreshModel();
        RefreshLevelText();
    }

    /// <summary>Sets the item this capsule holds and shows its model and level.</summary>
    public void SetData(ItemData data)
    {
        _data = data;
        RefreshModel();
        RefreshLevelText();
    }

    public void OnCollected()
    {
        _isCollected = true;

        if (_pickupEffect != null)
            Instantiate(_pickupEffect, transform.position, Quaternion.identity);

        _collected.OnNext(this);

        if (DestroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void RefreshLevelText()
    {
        if (_levelText != null)
            _levelText.text = _data != null ? $"Lvl {_data.Level}" : string.Empty;
    }

    private void RefreshModel()
    {
        if (_model != null && _data == _shownData)
            return;

        if (_model != null)
            Destroy(_model);

        _model = null;
        _shownData = _data;

        if (_data == null || _data.Model == null)
            return;

        GameObject prefab = _data.Model;
        _model = Instantiate(prefab, transform, false);
        _model.name = prefab.name;

        Transform model = _model.transform;
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = prefab.transform.localScale * _modelScale;
    }
}

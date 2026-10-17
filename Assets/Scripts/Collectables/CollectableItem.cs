using R3;
using UnityEngine;

/// <summary>
/// Item capsule: a pickup that shows the model of its <see cref="ItemData"/> and disappears when picked up.
/// The item models themselves are plain visuals; this capsule provides the collider and pickup logic.
/// Needs a Collider with "Is Trigger" enabled.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectableItem : MonoBehaviour, ICollectable
{
    [SerializeField] private ItemData _data;
    [SerializeField] private GameObject _pickupEffect;

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
    }

    /// <summary>Sets the item this capsule holds and shows its model.</summary>
    public void SetData(ItemData data)
    {
        _data = data;
        RefreshModel();
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
        model.localScale = prefab.transform.localScale;
    }
}

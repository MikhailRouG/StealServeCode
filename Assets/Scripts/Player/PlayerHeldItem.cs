using UnityEngine;

/// <summary>
/// Shows the last picked-up item from the <see cref="Player"/> inventory in the hand.
/// The model is placed under the hand socket, so it follows the animation.
/// </summary>
[RequireComponent(typeof(Player))]
public class PlayerHeldItem : MonoBehaviour
{
    [Tooltip("Empty object under the hand bone. The item is placed at its position and rotation.")]
    [SerializeField] private Transform _handSocket;
    [Tooltip("Item size in the hand relative to its prefab size (the rig's own scale is compensated).")]
    [SerializeField, Min(0.01f)] private float _scale = 1f;

    private Player _player;
    private ItemData _heldItem;
    private GameObject _heldObject;

    private void Awake()
    {
        _player = GetComponent<Player>();

        if (_handSocket == null)
        {
            Debug.LogError($"{nameof(PlayerHeldItem)}: hand socket is not set.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        _player.Inventory.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        _player.Inventory.Changed -= Refresh;
    }

    private void Refresh()
    {
        ItemData item = _player.Inventory.LastItem;
        if (item == _heldItem)
            return;

        Hide();
        if (item != null && item.Model != null)
            Show(item);
    }

    private void Show(ItemData item)
    {
        GameObject prefab = item.Model;
        _heldItem = item;
        _heldObject = Instantiate(prefab, _handSocket, false);
        _heldObject.name = prefab.name + " (Held)";

        Transform held = _heldObject.transform;
        held.localPosition = Vector3.zero;
        held.localRotation = Quaternion.identity;

        // Keep the item's world size independent of the bone scale.
        float socketScale = _handSocket.lossyScale.x;
        held.localScale = prefab.transform.localScale * (_scale / socketScale);
    }

    private void Hide()
    {
        if (_heldObject != null)
            Destroy(_heldObject);

        _heldObject = null;
        _heldItem = null;
    }
}

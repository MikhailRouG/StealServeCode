using R3;
using UnityEngine;
using Zenject;

/// <summary>
/// An area of the map (e.g. the forest), bounded by this object's collider seen from above.
/// Only describes itself; the <see cref="ZoneTracker"/> decides whether the player is in it and whether it is active.
/// Active = the player is near: things in the zone (see <see cref="SurfaceItemSpawner"/>) only run while it is active.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Zone : MonoBehaviour
{
    [Tooltip("When zones overlap, the player is in the one with the higher priority.")]
    [SerializeField] private int _priority;
    [Tooltip("The zone turns on when the player comes this close to its edge, meters.")]
    [SerializeField, Min(0f)] private float _enterMargin = 5f;
    [Tooltip("The zone turns off when the player goes farther than this from its edge, meters. "
        + "Keep it above Enter Margin so the zone doesn't flicker on the border.")]
    [SerializeField, Min(0f)] private float _exitMargin = 8f;

    [Header("Banner")]
    [Tooltip("Shown in the HUD when the player enters the zone (see ZoneBanner), e.g. \"Forest\". Empty = no banner.")]
    [SerializeField] private string _title;
    [Tooltip("Line under the title, e.g. \"Recommended lvl 1-5\".")]
    [SerializeField, TextArea] private string _subtitle;

    private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
    private Collider _collider;
    private ZoneTracker _tracker;

    public int Priority => _priority;
    public float EnterMargin => _enterMargin;
    public float ExitMargin => Mathf.Max(_exitMargin, _enterMargin);
    public string Title => _title;
    public string Subtitle => _subtitle;

    /// <summary>True while the player is near the zone. False until the tracker's first check.</summary>
    public ReadOnlyReactiveProperty<bool> IsActive => _isActive;

    [Inject]
    private void Construct(ZoneTracker tracker)
    {
        _collider = GetComponent<Collider>();
        _tracker = tracker;
        _tracker.Register(this);
    }

    private void OnDestroy()
    {
        _tracker?.Unregister(this);
        _isActive.Dispose();
    }

    /// <summary>Whether the point is inside the zone seen from above, with the bounds grown by <paramref name="margin"/>.</summary>
    public bool Contains(Vector3 position, float margin = 0f)
    {
        Bounds bounds = _collider.bounds;
        return position.x >= bounds.min.x - margin && position.x <= bounds.max.x + margin
            && position.z >= bounds.min.z - margin && position.z <= bounds.max.z + margin;
    }

    /// <summary>Set by the <see cref="ZoneTracker"/> only.</summary>
    internal void SetIsActive(bool active)
    {
        _isActive.Value = active;
    }

    private void OnDrawGizmosSelected()
    {
        Collider surface = _collider != null ? _collider : GetComponent<Collider>();
        if (surface == null)
            return;

        Bounds bounds = surface.bounds;
        DrawMargin(bounds, _enterMargin, new Color(0.3f, 0.8f, 1f, 1f));
        DrawMargin(bounds, ExitMargin, new Color(0.3f, 0.8f, 1f, 0.35f));
    }

    private static void DrawMargin(Bounds bounds, float margin, Color color)
    {
        Gizmos.color = color;
        Vector3 size = new Vector3(bounds.size.x + margin * 2f, 0.05f, bounds.size.z + margin * 2f);
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), size);
    }
}

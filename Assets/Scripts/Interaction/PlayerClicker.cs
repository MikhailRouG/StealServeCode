using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

/// <summary>
/// Put on a scene object (e.g. SceneManager): on left click casts a ray from the camera through the cursor
/// and calls <see cref="IClickable.OnClicked"/> on the first clickable object it hits.
/// Clicks over UI are ignored. Triggers are hit too, but only clickable triggers stop the ray.
/// With Draw Debug Ray on, the last click ray and hit point are shown in the Scene view
/// (and in the Game view when Gizmos are on): green = clickable, red = something else, yellow = nothing.
/// </summary>
public class PlayerClicker : MonoBehaviour
{
    [Tooltip("Camera the ray is cast from. If empty, Camera.main is used.")]
    [SerializeField] private Camera _camera;
    [SerializeField, Min(1f)] private float _maxDistance = 100f;
    [SerializeField] private LayerMask _layers = Physics.DefaultRaycastLayers;

    [Header("Debug")]
    [SerializeField] private bool _drawDebugRay = true;
    [Tooltip("How long the last click ray stays visible, seconds.")]
    [SerializeField, Min(0f)] private float _debugRayDuration = 2f;
    [SerializeField, Min(0f)] private float _debugHitRadius = 0.15f;

    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private Player _player;

    private Ray _lastRay;
    private Vector3 _lastEnd;
    private bool _lastHasHit;
    private Color _lastColor;
    private float _lastClickTime = float.NegativeInfinity;

    [Inject]
    private void Construct(Player player)
    {
        _player = player;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        // A dead player is turned off and can't click on anything.
        if (!_player.gameObject.activeInHierarchy)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (TryGetClicked(mouse.position.ReadValue(), out IClickable clickable))
            clickable.OnClicked(_player);
    }

    private bool TryGetClicked(Vector2 screenPosition, out IClickable clicked)
    {
        clicked = null;

        Camera camera = _camera != null ? _camera : Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        int count = Physics.RaycastNonAlloc(ray, _hits, _maxDistance, _layers, QueryTriggerInteraction.Collide);
        float closest = float.MaxValue;
        bool hasHit = false;
        Vector3 hitPoint = ray.GetPoint(_maxDistance);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];
            if (hit.distance >= closest || hit.collider.transform.IsChildOf(_player.transform))
                continue;

            IClickable clickable = hit.collider.GetComponentInParent<IClickable>();
            if (clickable != null && !clickable.IsClickArea(hit.collider))
                clickable = null;

            // Plain triggers (pickup zones, request points) don't block the click.
            if (clickable == null && hit.collider.isTrigger)
                continue;

            closest = hit.distance;
            clicked = clickable;
            hasHit = true;
            hitPoint = hit.point;
        }

        if (_drawDebugRay)
            DrawDebugRay(ray, hitPoint, hasHit, clicked != null);

        return clicked != null;
    }

    private void DrawDebugRay(Ray ray, Vector3 end, bool hasHit, bool isClickable)
    {
        _lastRay = ray;
        _lastEnd = end;
        _lastHasHit = hasHit;
        _lastColor = !hasHit ? Color.yellow : (isClickable ? Color.green : Color.red);
        _lastClickTime = Time.time;

        Debug.DrawLine(ray.origin, end, _lastColor, _debugRayDuration);
    }

    private void OnDrawGizmos()
    {
        if (!_drawDebugRay || !_lastHasHit || Time.time - _lastClickTime > _debugRayDuration)
            return;

        Gizmos.color = _lastColor;
        Gizmos.DrawLine(_lastRay.origin, _lastEnd);
        Gizmos.DrawSphere(_lastEnd, _debugHitRadius);
    }
}

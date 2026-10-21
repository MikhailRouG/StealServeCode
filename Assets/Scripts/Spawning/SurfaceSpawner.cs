using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Places a set of objects on the top surface of this object's collider on Start.
/// A removed object (collected, killed...) reappears at another random point on the surface.
/// Subclasses decide when it comes back by calling <see cref="Respawn"/>.
/// The objects live in a container at the scene root, so they can all be turned off at once
/// (<see cref="SetSpawnedActive"/>) without touching their own active state, which means "removed".
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class SurfaceSpawner<T> : MonoBehaviour where T : Component
{
    [Header("Placement")]
    [Tooltip("Height above the surface.")]
    [SerializeField] private float _heightOffset = 0.5f;
    [Tooltip("Minimum distance between spawned objects.")]
    [FormerlySerializedAs("_minDistanceBetweenItems")]
    [SerializeField, Min(0f)] private float _minDistanceBetweenObjects = 1f;
    [Tooltip("Distance from the edges of the surface where objects won't appear.")]
    [SerializeField, Min(0f)] private float _edgePadding = 0.5f;
    [SerializeField] private bool _alignToSurfaceNormal = false;

    private const int MaxPlacementAttempts = 15;

    private Collider _surface;
    private Transform _container;
    private readonly List<T> _spawned = new List<T>();
    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

    protected Collider Surface => _surface;
    protected float HeightOffset => _heightOffset;

    /// <summary>Subscriptions to the spawned objects' events, disposed together with the spawner.</summary>
    protected CompositeDisposable Subscriptions => _subscriptions;
    protected virtual Color GizmoColor => new Color(0.2f, 1f, 0.4f, 0.4f);

    /// <summary>Creates the initial objects with <see cref="Spawn"/>. Called once on Start.</summary>
    protected abstract void SpawnAll();

    /// <summary>
    /// Called right after an object is instantiated: configure it and subscribe to its events.
    /// Add the subscriptions to <see cref="Subscriptions"/>.
    /// </summary>
    protected abstract void OnSpawned(T obj);

    /// <summary>Called right after a removed object reappears at a new point.</summary>
    protected virtual void OnRespawned(T obj) { }

    private void Awake()
    {
        _surface = GetComponent<Collider>();

        // At the scene root, so the objects don't inherit the surface's scale.
        _container = new GameObject($"{name} (Spawned)").transform;
    }

    // Dependencies are injected before Awake, so subclasses have them all here.
    protected virtual void Start()
    {
        SpawnAll();
    }

    private void OnDestroy()
    {
        _subscriptions.Dispose();

        if (_container != null)
            Destroy(_container.gameObject);
    }

    /// <summary>
    /// Turns all spawned objects off or back on. Removed objects stay off either way,
    /// and objects respawned while off appear once turned back on.
    /// </summary>
    protected void SetSpawnedActive(bool active)
    {
        _container.gameObject.SetActive(active);
    }

    /// <summary>Brings the object back at a new point right away. The object should already be deactivated.</summary>
    protected void Respawn(T obj)
    {
        if (obj == null)
            return;

        GetSpawnPose(obj, out Vector3 position, out Quaternion rotation);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.gameObject.SetActive(true);
        OnRespawned(obj);
    }

    /// <summary>Instantiates the prefab at a random free point on the surface.</summary>
    protected T Spawn(T prefab)
    {
        // Instantiate directly at the spawn point, so physics components (e.g. CharacterController) start in the right place.
        GetSpawnPose(null, out Vector3 position, out Quaternion rotation);
        T obj = Instantiate(prefab, position, rotation, _container);
        _spawned.Add(obj);
        OnSpawned(obj);
        return obj;
    }

    private void GetSpawnPose(T self, out Vector3 position, out Quaternion rotation)
    {
        position = transform.position;
        Vector3 normal = Vector3.up;

        // Try to find a free spot; if none is found, use the last valid surface point anyway.
        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            if (!TryGetSurfacePoint(out Vector3 point, out Vector3 hitNormal))
                continue;

            position = point + hitNormal * _heightOffset;
            normal = hitNormal;

            if (IsFarEnoughFromOthers(position, self))
                break;
        }

        Vector3 up = _alignToSurfaceNormal ? normal : Vector3.up;
        rotation = Quaternion.FromToRotation(Vector3.up, up);
    }

    /// <summary>Random point on top of the surface, away from the edges.</summary>
    protected bool TryGetSurfacePoint(out Vector3 point, out Vector3 normal)
    {
        Bounds bounds = _surface.bounds;
        float padX = Mathf.Min(_edgePadding, bounds.extents.x);
        float padZ = Mathf.Min(_edgePadding, bounds.extents.z);

        Vector3 origin = new Vector3(
            Random.Range(bounds.min.x + padX, bounds.max.x - padX),
            bounds.max.y + 1f,
            Random.Range(bounds.min.z + padZ, bounds.max.z - padZ));

        // Raycast only against our own collider, so other objects on top don't interfere.
        var ray = new Ray(origin, Vector3.down);
        if (_surface.Raycast(ray, out RaycastHit hit, bounds.size.y + 2f))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        point = default;
        normal = Vector3.up;
        return false;
    }

    private bool IsFarEnoughFromOthers(Vector3 position, T self)
    {
        float minSqr = _minDistanceBetweenObjects * _minDistanceBetweenObjects;
        foreach (T obj in _spawned)
        {
            if (obj == null || obj == self || !obj.gameObject.activeSelf)
                continue;

            if ((obj.transform.position - position).sqrMagnitude < minSqr)
                return false;
        }
        return true;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Collider surface = _surface != null ? _surface : GetComponent<Collider>();
        if (surface == null)
            return;

        Gizmos.color = GizmoColor;
        Bounds bounds = surface.bounds;
        Vector3 size = new Vector3(
            Mathf.Max(0f, bounds.size.x - _edgePadding * 2f),
            0.05f,
            Mathf.Max(0f, bounds.size.z - _edgePadding * 2f));
        Gizmos.DrawCube(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z), size);
    }
}

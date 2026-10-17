using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Editor-time scatter tool: places prefabs inside a box zone onto the ground below it.
/// Spawned objects are regular scene objects saved with the scene; nothing runs at runtime.
/// </summary>
public class ZoneScatter : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public GameObject Prefab;
        [Min(0f)] public float Weight = 1f;
    }

    [SerializeField] private List<Entry> _prefabs = new List<Entry>();
    [SerializeField, Min(0)] private int _count = 20;
    [SerializeField] private int _seed;

    [Header("Zone")]
    [Tooltip("Box size in meters, centered on this object (its scale is ignored). Rays are cast from the top face downwards.")]
    [SerializeField] private Vector3 _size = new Vector3(10f, 5f, 10f);
    [SerializeField] private LayerMask _groundMask = ~0;

    [Header("Placement")]
    [Tooltip("Minimum distance between spawned objects.")]
    [SerializeField, Min(0f)] private float _minDistance = 1f;
    [SerializeField] private bool _alignToSurfaceNormal;
    [SerializeField] private bool _randomYRotation = true;
    [Tooltip("Random uniform scale multiplier (min, max).")]
    [SerializeField] private Vector2 _scaleRange = Vector2.one;

    [Tooltip("Parent for spawned objects. Created at the scene root if empty. Avoid scaled parents.")]
    [SerializeField] private Transform _container;

    private const int AttemptsPerObject = 30;

    public int Seed { get => _seed; set => _seed = value; }

#if UNITY_EDITOR
    /// <summary>Removes previously spawned objects and scatters new ones. Undoable as a single step.</summary>
    public void Generate()
    {
        if (!_prefabs.Exists(e => e.Prefab != null && e.Weight > 0f))
        {
            Debug.LogWarning($"{name}: no prefabs assigned to scatter.", this);
            return;
        }

        if (!IsContainerValid())
            return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        Clear();
        // In edit mode physics may still use stale collider positions.
        Physics.SyncTransforms();

        Transform parent = GetOrCreateContainer();
        var rng = new System.Random(_seed);
        var placed = new List<Vector3>();
        var spawned = new List<Transform>();
        float minSqr = _minDistance * _minDistance;
        Matrix4x4 zone = ZoneMatrix;
        Vector3 down = -transform.up;
        int groundHits = 0;

        for (int attempt = 0; attempt < _count * AttemptsPerObject && placed.Count < _count; attempt++)
        {
            Vector3 local = new Vector3(
                ((float)rng.NextDouble() - 0.5f) * _size.x,
                _size.y * 0.5f,
                ((float)rng.NextDouble() - 0.5f) * _size.z);
            Vector3 origin = zone.MultiplyPoint3x4(local);

            if (!Physics.Raycast(origin, down, out RaycastHit hit, _size.y, _groundMask, QueryTriggerInteraction.Ignore))
                continue;
            groundHits++;
            // Don't stack objects on top of the ones we just placed.
            if (spawned.Exists(s => hit.transform.IsChildOf(s)))
                continue;
            if (placed.Exists(p => (p - hit.point).sqrMagnitude < minSqr))
                continue;

            GameObject prefab = PickPrefab(rng);
            if (prefab == null)
                break;

            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Vector3 up = _alignToSurfaceNormal ? hit.normal : Vector3.up;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, up);
            if (_randomYRotation)
                rotation *= Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            obj.transform.SetPositionAndRotation(hit.point, rotation);
            obj.transform.localScale *= Mathf.Lerp(_scaleRange.x, _scaleRange.y, (float)rng.NextDouble());

            Undo.RegisterCreatedObjectUndo(obj, "Scatter Objects");
            placed.Add(hit.point);
            spawned.Add(obj.transform);
        }

        if (groundHits == 0)
            Debug.LogWarning($"{name}: no ground found inside the zone. Make sure the zone box intersects a collider on Ground Mask.", this);
        else if (placed.Count < _count)
            Debug.LogWarning($"{name}: placed {placed.Count}/{_count}, not enough free space.", this);

        Undo.CollapseUndoOperations(group);
    }

    /// <summary>Destroys all objects in the container. Undoable.</summary>
    public void Clear()
    {
        if (_container == null || !IsContainerValid())
            return;

        for (int i = _container.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(_container.GetChild(i).gameObject);
    }

    // Clear() destroys every child of the container, so it must not be this object or one of its parents.
    private bool IsContainerValid()
    {
        if (_container == null || !transform.IsChildOf(_container))
            return true;

        Debug.LogError($"{name}: Container must be a separate empty object, not this object or its parent. Clear the field to create one automatically.", this);
        return false;
    }

    private Transform GetOrCreateContainer()
    {
        if (_container != null)
            return _container;

        // Created at the scene root, so spawned objects don't inherit this object's scale.
        var go = new GameObject($"{name} (Spawned)");
        SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        Undo.RegisterCreatedObjectUndo(go, "Scatter Objects");
        Undo.RecordObject(this, "Scatter Objects");
        _container = go.transform;
        return _container;
    }

    private GameObject PickPrefab(System.Random rng)
    {
        float total = 0f;
        foreach (Entry e in _prefabs)
        {
            if (e.Prefab != null)
                total += e.Weight;
        }
        if (total <= 0f)
            return null;

        float roll = (float)rng.NextDouble() * total;
        foreach (Entry e in _prefabs)
        {
            if (e.Prefab == null)
                continue;
            roll -= e.Weight;
            if (roll <= 0f)
                return e.Prefab;
        }
        return null;
    }
#endif

    // Zone ignores the object's scale, so Size is always in meters (the scatter can live on a scaled ground object).
    private Matrix4x4 ZoneMatrix => Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = ZoneMatrix;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.15f);
        Gizmos.DrawCube(Vector3.zero, _size);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, _size);
    }
}

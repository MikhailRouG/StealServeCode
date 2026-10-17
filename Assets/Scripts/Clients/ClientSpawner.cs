using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a pool of clients created when <see cref="Init"/> is called (by <see cref="Bootstrap"/>).
/// Every interval a free client appears at point A or B and walks with a random speed along X
/// to the other point's X, where it turns off and goes back to the pool.
/// Each spawned client gets a random interest (see <see cref="ClientSales"/>).
/// The points are offsets from the spawner and drawn as gizmos.
/// </summary>
public class ClientSpawner : MonoBehaviour
{
    [SerializeField] private Client _clientPrefab;
    [SerializeField, Min(1)] private int _poolSize = 10;
    [Tooltip("Seconds between clients.")]
    [SerializeField, Min(0.1f)] private float _spawnInterval = 2f;
    [Tooltip("Client speed, random between X and Y.")]
    [SerializeField] private Vector2 _speed = new Vector2(1f, 2f);
    [Tooltip("Client interest, random between X and Y (inclusive). New on every spawn.")]
    [SerializeField] private Vector2Int _interest = new Vector2Int(0, 100);

    [Header("Points (offset from the spawner)")]
    [SerializeField] private Vector3 _pointA = new Vector3(-5f, 0f, 0f);
    [SerializeField] private Vector3 _pointB = new Vector3(5f, 0f, 0f);

    private readonly List<Client> _pool = new List<Client>();
    private bool _isInitialized;

    /// <summary>All pooled clients; the walking ones are active.</summary>
    public IReadOnlyList<Client> Clients => _pool;

    /// <summary>Raised with the client after it appears and gets its interest.</summary>
    public event System.Action<Client> Spawned;

    private Vector3 PointA => transform.position + _pointA;
    private Vector3 PointB => transform.position + _pointB;

    /// <summary>Creates the pool and starts spawning clients. Called once by <see cref="Bootstrap"/>.</summary>
    public void Init()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;

        if (_clientPrefab == null)
        {
            Debug.LogError($"{nameof(ClientSpawner)}: client prefab is not set.", this);
            return;
        }

        for (int i = 0; i < _poolSize; i++)
        {
            Client client = Instantiate(_clientPrefab, transform);
            client.gameObject.SetActive(false);
            _pool.Add(client);
        }

        StartCoroutine(SpawnClients());
    }

    private IEnumerator SpawnClients()
    {
        var wait = new WaitForSeconds(_spawnInterval);

        while (true)
        {
            Client client = _pool.Find(pooled => !pooled.gameObject.activeSelf);
            if (client != null)
                Spawn(client);

            yield return wait;
        }
    }

    private void Spawn(Client client)
    {
        bool fromA = Random.value < 0.5f;
        Vector3 start = fromA ? PointA : PointB;
        Vector3 end = fromA ? PointB : PointA;

        client.transform.position = start;
        client.gameObject.SetActive(true);
        client.Init(end.x, Random.Range(_speed.x, _speed.y), Random.Range(_interest.x, _interest.y + 1));
        Spawned?.Invoke(client);
    }

    private void OnDrawGizmos()
    {
        Vector3 pointA = PointA;
        Vector3 pointB = PointB;

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 1f);
        Gizmos.DrawWireSphere(pointA, 0.3f);
        Gizmos.DrawWireSphere(pointB, 0.3f);

        // Paths along X from each point to the other point's X.
        Gizmos.DrawLine(pointA, new Vector3(pointB.x, pointA.y, pointA.z));
        Gizmos.DrawLine(pointB, new Vector3(pointA.x, pointB.y, pointB.z));
    }
}

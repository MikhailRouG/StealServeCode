using R3;
using UnityEngine;

/// <summary>
/// Basic monster. Call <see cref="Die"/> to kill it.
/// </summary>
public class Monster : MonoBehaviour
{
    [SerializeField] private GameObject _deathEffect;

    private readonly Subject<Monster> _died = new Subject<Monster>();
    private bool _isDead;

    public bool IsDead => _isDead;

    public bool DestroyOnDeath { get; set; } = true;

    /// <summary>Fires every time the monster dies. Completes when the monster is destroyed.</summary>
    public Observable<Monster> Died => _died;

    private void OnEnable()
    {
        _isDead = false;
    }

    private void OnDestroy()
    {
        _died.Dispose();
    }

    public void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        if (_deathEffect != null)
            Instantiate(_deathEffect, transform.position, Quaternion.identity);

        _died.OnNext(this);

        if (DestroyOnDeath)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}

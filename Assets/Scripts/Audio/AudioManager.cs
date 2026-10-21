using R3;
using UnityEngine;
using Zenject;

/// <summary>
/// Plays the background music. Main music loops all the time; while a guard monster chases the player
/// it crossfades to the chase music and back once the chase is over (see <see cref="ChaseTracker"/>).
/// The main music is only muted during a chase, so it continues from the same place; the chase music starts from the beginning.
/// Put one on an empty object in the scene; the chase state is injected.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip _mainMusic;
    [SerializeField] private AudioClip _chaseMusic;
    [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.6f;
    [Tooltip("Seconds of the crossfade between the main and the chase music.")]
    [SerializeField, Min(0f)] private float _fadeDuration = 1f;

    private AudioSource _mainSource;
    private AudioSource _chaseSource;
    private bool _isChase;
    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

    private void Awake()
    {
        _mainSource = CreateMusicSource(_mainMusic);
        _chaseSource = CreateMusicSource(_chaseMusic);

        _mainSource.volume = _musicVolume;
        if (_mainMusic != null)
            _mainSource.Play();
    }

    private void OnDestroy()
    {
        _subscriptions.Dispose();
    }

    [Inject]
    private void Construct(ChaseTracker chaseTracker)
    {
        chaseTracker.IsPlayerChased
            .Skip(1)
            .Subscribe(OnPlayerChasedChanged)
            .AddTo(_subscriptions);
    }

    private void OnPlayerChasedChanged(bool isChased)
    {
        _isChase = isChased;
        if (_isChase && _chaseMusic != null && !_chaseSource.isPlaying)
            _chaseSource.Play();
    }

    private void Update()
    {
        float step = _fadeDuration > 0f ? _musicVolume * Time.unscaledDeltaTime / _fadeDuration : _musicVolume;
        bool chaseAudible = _isChase && _chaseMusic != null;

        _mainSource.volume = Mathf.MoveTowards(_mainSource.volume, chaseAudible ? 0f : _musicVolume, step);
        _chaseSource.volume = Mathf.MoveTowards(_chaseSource.volume, chaseAudible ? _musicVolume : 0f, step);

        // The chase music restarts from the beginning next time.
        if (!_isChase && _chaseSource.isPlaying && _chaseSource.volume <= 0f)
            _chaseSource.Stop();
    }

    private AudioSource CreateMusicSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }
}

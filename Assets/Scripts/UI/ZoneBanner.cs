using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

/// <summary>
/// HUD banner with the name of the zone the player has just entered (e.g. "Forest" / "Recommended lvl 1-5"):
/// slides in from above, stays for a while and fades out. The text is set on the <see cref="Zone"/>;
/// zones without a title show nothing. Gets the <see cref="ZoneTracker"/> through injection (see <see cref="GameInstaller"/>).
/// The tween is built once and restarted for every new zone.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ZoneBanner : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _subtitleText;

    [Header("Animation")]
    [Tooltip("The banner slides down from this far above its place, pixels.")]
    [SerializeField] private float _slideDistance = 60f;
    [SerializeField, Min(0.01f)] private float _fadeDuration = 0.35f;
    [Tooltip("How long the banner stays fully shown, seconds.")]
    [SerializeField, Min(0f)] private float _holdDuration = 2.5f;

    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
    private RectTransform _rect;
    private CanvasGroup _group;
    private Tween _tween;

    private void Awake()
    {
        Build();
    }

    private void OnDestroy()
    {
        _subscriptions.Dispose();
    }

    [Inject]
    private void Construct(ZoneTracker tracker)
    {
        tracker.CurrentZone
            .Subscribe(OnZoneChanged)
            .AddTo(_subscriptions);
    }

    // Leaving a zone keeps the banner as it is: it only shows where the player has come.
    private void OnZoneChanged(Zone zone)
    {
        if (zone == null || string.IsNullOrEmpty(zone.Title))
            return;

        Build();
        _titleText.text = zone.Title;
        if (_subtitleText != null)
        {
            _subtitleText.text = zone.Subtitle;
            _subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(zone.Subtitle));
        }

        _tween.Restart();
    }

    // Injection runs before Awake, so the tween is built by whichever comes first.
    private void Build()
    {
        if (_tween != null)
            return;

        _rect = (RectTransform)transform;
        _group = GetComponent<CanvasGroup>();
        if (_group == null)
            _group = gameObject.AddComponent<CanvasGroup>();

        _group.interactable = false;
        _group.blocksRaycasts = false;

        Vector2 shownPosition = _rect.anchoredPosition;
        _rect.anchoredPosition = shownPosition + Vector2.up * _slideDistance;
        _group.alpha = 0f;

        _tween = DOTween.Sequence()
            .Append(_rect.DOAnchorPos(shownPosition, _fadeDuration).SetEase(Ease.OutCubic))
            .Join(_group.DOFade(1f, _fadeDuration))
            .AppendInterval(_holdDuration)
            .Append(_group.DOFade(0f, _fadeDuration))
            .SetAutoKill(false)
            .SetUpdate(true)
            .SetLink(gameObject)
            .Pause();
    }
}

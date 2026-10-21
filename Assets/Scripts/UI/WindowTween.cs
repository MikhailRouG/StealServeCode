using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Show / hide animation of a UI window made with DOTween: a popup (grows and fades in) or a slide in from the left.
/// The tween is built once, on the first <see cref="Show"/>, and then only played forward (show) and backwards (hide),
/// so opening and closing the window doesn't create new tweens. The target is activated before showing
/// and deactivated once hidden. Call <see cref="Init"/> in the window's Awake.
/// </summary>
[Serializable]
public class WindowTween
{
    public enum Style
    {
        Popup,
        SlideFromLeft
    }

    [SerializeField] private Style _style;
    [SerializeField, Min(0.01f)] private float _duration = 0.25f;
    [SerializeField] private Ease _ease = Ease.OutBack;
    [Tooltip("Popup: the share of the size the window grows from.")]
    [SerializeField, Range(0f, 1f)] private float _popupScale = 0.8f;

    private GameObject _target;
    private RectTransform _rect;
    private CanvasGroup _group;
    private Vector3 _shownScale;
    private Vector2 _shownPosition;
    private Tween _tween;
    private Action _hidden;

    public WindowTween()
    {
    }

    public WindowTween(Style style)
    {
        _style = style;
        if (style == Style.SlideFromLeft)
        {
            _duration = 0.3f;
            _ease = Ease.OutCubic;
        }
    }

    /// <summary>True from <see cref="Show"/> until <see cref="Hide"/> (also while the hide animation plays).</summary>
    public bool IsShown { get; private set; }

    /// <summary>Remembers the target's shown scale and position; call it while the target is not animated yet.</summary>
    public void Init(GameObject target)
    {
        _target = target;
        _rect = (RectTransform)target.transform;
        _group = target.GetComponent<CanvasGroup>();
        if (_group == null)
            _group = target.AddComponent<CanvasGroup>();

        _shownScale = _rect.localScale;
        _shownPosition = _rect.anchoredPosition;
        IsShown = target.activeSelf;
    }

    public void Show()
    {
        _hidden = null;
        IsShown = true;
        _target.SetActive(true);
        _group.interactable = true;
        _group.blocksRaycasts = true;

        if (_tween == null)
            Build();

        _tween.PlayForward();
    }

    /// <summary>Plays the show animation backwards, then deactivates the target and calls <paramref name="hidden"/>.</summary>
    public void Hide(Action hidden = null)
    {
        IsShown = false;
        _hidden = hidden;
        _group.interactable = false;
        _group.blocksRaycasts = false;

        // Nothing to play back: never shown or already at the hidden state.
        if (_tween == null || !_target.activeInHierarchy || _tween.Elapsed() <= 0f)
        {
            _tween?.Rewind();
            Deactivate();
            return;
        }

        _tween.PlayBackwards();
    }

    private void Build()
    {
        Sequence sequence = DOTween.Sequence();

        if (_style == Style.Popup)
        {
            _rect.localScale = _shownScale * _popupScale;
            sequence.Join(_rect.DOScale(_shownScale, _duration).SetEase(_ease));
        }
        else
        {
            // Starts fully behind the left edge of the parent (or of the window itself if it is wider).
            float width = _rect.rect.width;
            if (_rect.parent is RectTransform parent)
                width = Mathf.Max(width, parent.rect.width);

            _rect.anchoredPosition = _shownPosition + Vector2.left * width;
            sequence.Join(_rect.DOAnchorPos(_shownPosition, _duration).SetEase(_ease));
        }

        _group.alpha = 0f;
        sequence.Join(_group.DOFade(1f, _duration * 0.6f));

        _tween = sequence
            .SetAutoKill(false)
            .SetUpdate(true)
            .SetLink(_target)
            .OnRewind(OnRewound)
            .Pause();
    }

    private void OnRewound()
    {
        if (!IsShown)
            Deactivate();
    }

    private void Deactivate()
    {
        _target.SetActive(false);

        Action hidden = _hidden;
        _hidden = null;
        hidden?.Invoke();
    }
}

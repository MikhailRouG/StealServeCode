using TMPro;
using UnityEngine;

/// <summary>
/// Player HUD: shows the player's money, level, experience and speed stat and updates them when they change.
/// Initialized by <see cref="Bootstrap"/>.
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _moneyText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _experienceText;
    [SerializeField] private TMP_Text _speedText;

    private Wallet _wallet;
    private PlayerLevel _level;
    private PlayerMove _move;

    private void OnDestroy()
    {
        Unsubscribe();
    }

    /// <summary>Starts showing the player's money, level and speed. Called once by <see cref="Bootstrap"/>.</summary>
    public void Init(Wallet wallet, PlayerLevel level, PlayerMove move)
    {
        Unsubscribe();

        _wallet = wallet;
        _level = level;
        _move = move;

        if (_move != null)
        {
            _move.SpeedStatChanged += OnSpeedStatChanged;
            OnSpeedStatChanged(_move.CurrentSpeedStat);
        }

        if (_wallet != null)
        {
            _wallet.Changed += OnMoneyChanged;
            OnMoneyChanged(_wallet.Money);
        }

        if (_level != null)
        {
            _level.ExperienceChanged += OnExperienceChanged;
            OnExperienceChanged(_level.Experience, _level.ExperienceToNextLevel);
        }
    }

    private void Unsubscribe()
    {
        if (_wallet != null)
            _wallet.Changed -= OnMoneyChanged;
        if (_level != null)
            _level.ExperienceChanged -= OnExperienceChanged;
        if (_move != null)
            _move.SpeedStatChanged -= OnSpeedStatChanged;
    }

    private void OnSpeedStatChanged(float speedStat)
    {
        if (_speedText != null)
            _speedText.text = Mathf.RoundToInt(speedStat).ToString();
    }

    private void OnMoneyChanged(int money)
    {
        if (_moneyText != null)
            _moneyText.text = money.ToString();
    }

    private void OnExperienceChanged(int experience, int experienceToNextLevel)
    {
        if (_levelText != null)
            _levelText.text = $"Lv. {_level.Level}";

        if (_experienceText != null)
            _experienceText.text = $"{experience}/{experienceToNextLevel}";
    }
}

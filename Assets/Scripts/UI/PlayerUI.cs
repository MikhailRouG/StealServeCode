using TMPro;
using UnityEngine;
using Zenject;

/// <summary>
/// Player HUD: shows the player's money, level, experience and speed stat and updates them when they change.
/// Gets the player's models through injection (see <see cref="GameInstaller"/>).
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _moneyText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _experienceText;
    [SerializeField] private TMP_Text _speedText;

    private Wallet _wallet;
    private PlayerLevel _level;
    private PlayerStats _stats;

    private void OnDestroy()
    {
        Unsubscribe();
    }

    [Inject]
    private void Construct(Wallet wallet, PlayerLevel level, PlayerStats stats)
    {
        Unsubscribe();

        _wallet = wallet;
        _level = level;
        _stats = stats;

        if (_stats != null)
        {
            _stats.SpeedStatChanged += OnSpeedStatChanged;
            OnSpeedStatChanged(_stats.CurrentSpeedStat);
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
        if (_stats != null)
            _stats.SpeedStatChanged -= OnSpeedStatChanged;
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
            _levelText.text = $" {_level.Level}";

        if (_experienceText != null)
            _experienceText.text = $"{experience}/{experienceToNextLevel}";
    }
}

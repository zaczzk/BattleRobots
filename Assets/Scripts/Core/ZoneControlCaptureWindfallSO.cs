using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureWindfall", order = 143)]
    public sealed class ZoneControlCaptureWindfallSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _requiredStreak  = 3;
        [SerializeField, Min(0)] private int _bonusPerWindfall = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onWindfall;

        private int _currentStreak;
        private int _windfallCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int RequiredStreak    => _requiredStreak;
        public int BonusPerWindfall  => _bonusPerWindfall;
        public int CurrentStreak     => _currentStreak;
        public int WindfallCount     => _windfallCount;
        public int TotalBonusAwarded => _totalBonusAwarded;

        public void RecordPlayerCapture()
        {
            _currentStreak++;
            if (_currentStreak < _requiredStreak)
                return;

            _windfallCount++;
            _totalBonusAwarded += _bonusPerWindfall;
            _currentStreak      = 0;
            _onWindfall?.Raise();
        }

        public void RecordBotCapture()
        {
            _currentStreak = 0;
        }

        public void Reset()
        {
            _currentStreak     = 0;
            _windfallCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

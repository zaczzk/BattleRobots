using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureComboFinisher", order = 174)]
    public sealed class ZoneControlCaptureComboFinisherSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _comboTarget = 4;
        [SerializeField, Min(0)] private int _comboBonus  = 175;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onComboFinished;

        private int _currentCombo;
        private int _completedCombos;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ComboTarget       => _comboTarget;
        public int   ComboBonus        => _comboBonus;
        public int   CurrentCombo      => _currentCombo;
        public int   CompletedCombos   => _completedCombos;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ComboProgress     => Mathf.Clamp01((float)_currentCombo / Mathf.Max(1, _comboTarget));

        public void RecordPlayerCapture()
        {
            _currentCombo++;
            if (_currentCombo >= _comboTarget)
            {
                _completedCombos++;
                _totalBonusAwarded += _comboBonus;
                _currentCombo       = 0;
                _onComboFinished?.Raise();
            }
        }

        public void RecordBotCapture()
        {
            _currentCombo = 0;
        }

        public void Reset()
        {
            _currentCombo      = 0;
            _completedCombos   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOptic", order = 374)]
    public sealed class ZoneControlCaptureOpticSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _focusNeeded    = 7;
        [SerializeField, Min(1)] private int _blurPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerFocus  = 2350;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOpticFocused;

        private int _focus;
        private int _focusCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FocusNeeded        => _focusNeeded;
        public int   BlurPerBot         => _blurPerBot;
        public int   BonusPerFocus      => _bonusPerFocus;
        public int   Focus              => _focus;
        public int   FocusCount         => _focusCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float FocusProgress      => _focusNeeded > 0
            ? Mathf.Clamp01(_focus / (float)_focusNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _focus = Mathf.Min(_focus + 1, _focusNeeded);
            if (_focus >= _focusNeeded)
            {
                int bonus = _bonusPerFocus;
                _focusCount++;
                _totalBonusAwarded += bonus;
                _focus              = 0;
                _onOpticFocused?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _focus = Mathf.Max(0, _focus - _blurPerBot);
        }

        public void Reset()
        {
            _focus             = 0;
            _focusCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInftyCat", order = 455)]
    public sealed class ZoneControlCaptureInftyCatSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _homotopiesNeeded  = 5;
        [SerializeField, Min(1)] private int _coherencePerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerCompose   = 3565;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInftyCatComposed;

        private int _homotopies;
        private int _composeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HomotopiesNeeded  => _homotopiesNeeded;
        public int   CoherencePerBot   => _coherencePerBot;
        public int   BonusPerCompose   => _bonusPerCompose;
        public int   Homotopies        => _homotopies;
        public int   ComposeCount      => _composeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float HomotopyProgress  => _homotopiesNeeded > 0
            ? Mathf.Clamp01(_homotopies / (float)_homotopiesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _homotopies = Mathf.Min(_homotopies + 1, _homotopiesNeeded);
            if (_homotopies >= _homotopiesNeeded)
            {
                int bonus = _bonusPerCompose;
                _composeCount++;
                _totalBonusAwarded += bonus;
                _homotopies         = 0;
                _onInftyCatComposed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _homotopies = Mathf.Max(0, _homotopies - _coherencePerBot);
        }

        public void Reset()
        {
            _homotopies        = 0;
            _composeCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCobordism", order = 453)]
    public sealed class ZoneControlCaptureCobordismSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _boundariesNeeded = 5;
        [SerializeField, Min(1)] private int _puncturePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerCobordism = 3535;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCobordismComplete;

        private int _boundaries;
        private int _cobordismCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BoundariesNeeded   => _boundariesNeeded;
        public int   PuncturePerBot     => _puncturePerBot;
        public int   BonusPerCobordism  => _bonusPerCobordism;
        public int   Boundaries         => _boundaries;
        public int   CobordismCount     => _cobordismCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float CobordismProgress  => _boundariesNeeded > 0
            ? Mathf.Clamp01(_boundaries / (float)_boundariesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _boundaries = Mathf.Min(_boundaries + 1, _boundariesNeeded);
            if (_boundaries >= _boundariesNeeded)
            {
                int bonus = _bonusPerCobordism;
                _cobordismCount++;
                _totalBonusAwarded += bonus;
                _boundaries         = 0;
                _onCobordismComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _boundaries = Mathf.Max(0, _boundaries - _puncturePerBot);
        }

        public void Reset()
        {
            _boundaries        = 0;
            _cobordismCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

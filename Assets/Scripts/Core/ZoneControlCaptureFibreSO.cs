using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFibre", order = 450)]
    public sealed class ZoneControlCaptureFibreSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _fibresNeeded       = 6;
        [SerializeField, Min(1)] private int _trivializePerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerProjection = 3490;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFibreProjected;

        private int _fibres;
        private int _projectionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FibresNeeded       => _fibresNeeded;
        public int   TrivializePerBot   => _trivializePerBot;
        public int   BonusPerProjection => _bonusPerProjection;
        public int   Fibres             => _fibres;
        public int   ProjectionCount    => _projectionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float FibreProgress      => _fibresNeeded > 0
            ? Mathf.Clamp01(_fibres / (float)_fibresNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _fibres = Mathf.Min(_fibres + 1, _fibresNeeded);
            if (_fibres >= _fibresNeeded)
            {
                int bonus = _bonusPerProjection;
                _projectionCount++;
                _totalBonusAwarded += bonus;
                _fibres             = 0;
                _onFibreProjected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _fibres = Mathf.Max(0, _fibres - _trivializePerBot);
        }

        public void Reset()
        {
            _fibres            = 0;
            _projectionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureComonad", order = 368)]
    public sealed class ZoneControlCaptureComonadSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _contextsNeeded  = 6;
        [SerializeField, Min(1)] private int _collapsePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerExtract = 2260;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onComonadExtracted;

        private int _contexts;
        private int _extractCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ContextsNeeded     => _contextsNeeded;
        public int   CollapsePerBot     => _collapsePerBot;
        public int   BonusPerExtract    => _bonusPerExtract;
        public int   Contexts           => _contexts;
        public int   ExtractCount       => _extractCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ContextProgress    => _contextsNeeded > 0
            ? Mathf.Clamp01(_contexts / (float)_contextsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _contexts = Mathf.Min(_contexts + 1, _contextsNeeded);
            if (_contexts >= _contextsNeeded)
            {
                int bonus = _bonusPerExtract;
                _extractCount++;
                _totalBonusAwarded += bonus;
                _contexts           = 0;
                _onComonadExtracted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _contexts = Mathf.Max(0, _contexts - _collapsePerBot);
        }

        public void Reset()
        {
            _contexts          = 0;
            _extractCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

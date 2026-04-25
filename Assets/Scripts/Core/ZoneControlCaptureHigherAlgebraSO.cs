using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHigherAlgebra", order = 499)]
    public sealed class ZoneControlCaptureHigherAlgebraSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _structureMapsNeeded    = 5;
        [SerializeField, Min(1)] private int _coherenceFailuresPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerComposition     = 4225;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHigherAlgebraComposed;

        private int _structureMaps;
        private int _compositionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StructureMapsNeeded    => _structureMapsNeeded;
        public int   CoherenceFailuresPerBot => _coherenceFailuresPerBot;
        public int   BonusPerComposition     => _bonusPerComposition;
        public int   StructureMaps           => _structureMaps;
        public int   CompositionCount        => _compositionCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float StructureMapProgress => _structureMapsNeeded > 0
            ? Mathf.Clamp01(_structureMaps / (float)_structureMapsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _structureMaps = Mathf.Min(_structureMaps + 1, _structureMapsNeeded);
            if (_structureMaps >= _structureMapsNeeded)
            {
                int bonus = _bonusPerComposition;
                _compositionCount++;
                _totalBonusAwarded += bonus;
                _structureMaps      = 0;
                _onHigherAlgebraComposed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _structureMaps = Mathf.Max(0, _structureMaps - _coherenceFailuresPerBot);
        }

        public void Reset()
        {
            _structureMaps     = 0;
            _compositionCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

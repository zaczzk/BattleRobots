using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRepresentable", order = 417)]
    public sealed class ZoneControlCaptureRepresentableSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _setsNeeded            = 5;
        [SerializeField, Min(1)] private int _prunePerBot            = 1;
        [SerializeField, Min(0)] private int _bonusPerRepresentation = 2995;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRepresentableRepresented;

        private int _sets;
        private int _representationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SetsNeeded             => _setsNeeded;
        public int   PrunePerBot            => _prunePerBot;
        public int   BonusPerRepresentation => _bonusPerRepresentation;
        public int   Sets                   => _sets;
        public int   RepresentationCount    => _representationCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float SetProgress            => _setsNeeded > 0
            ? Mathf.Clamp01(_sets / (float)_setsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sets = Mathf.Min(_sets + 1, _setsNeeded);
            if (_sets >= _setsNeeded)
            {
                int bonus = _bonusPerRepresentation;
                _representationCount++;
                _totalBonusAwarded += bonus;
                _sets               = 0;
                _onRepresentableRepresented?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sets = Mathf.Max(0, _sets - _prunePerBot);
        }

        public void Reset()
        {
            _sets                = 0;
            _representationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

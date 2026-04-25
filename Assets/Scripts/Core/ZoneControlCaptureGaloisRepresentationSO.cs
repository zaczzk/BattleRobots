using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGaloisRepresentation", order = 508)]
    public sealed class ZoneControlCaptureGaloisRepresentationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _representationsNeeded      = 6;
        [SerializeField, Min(1)] private int _frobeniusObstructionsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerRealization         = 4360;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGaloisRepresentationRealized;

        private int _representations;
        private int _realizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RepresentationsNeeded       => _representationsNeeded;
        public int   FrobeniusObstructionsPerBot => _frobeniusObstructionsPerBot;
        public int   BonusPerRealization         => _bonusPerRealization;
        public int   Representations             => _representations;
        public int   RealizationCount            => _realizationCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float RepresentationProgress => _representationsNeeded > 0
            ? Mathf.Clamp01(_representations / (float)_representationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _representations = Mathf.Min(_representations + 1, _representationsNeeded);
            if (_representations >= _representationsNeeded)
            {
                int bonus = _bonusPerRealization;
                _realizationCount++;
                _totalBonusAwarded += bonus;
                _representations    = 0;
                _onGaloisRepresentationRealized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _representations = Mathf.Max(0, _representations - _frobeniusObstructionsPerBot);
        }

        public void Reset()
        {
            _representations   = 0;
            _realizationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

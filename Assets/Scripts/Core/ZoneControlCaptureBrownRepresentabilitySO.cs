using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBrownRepresentability", order = 506)]
    public sealed class ZoneControlCaptureBrownRepresentabilitySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _representableFunctorsNeeded       = 5;
        [SerializeField, Min(1)] private int _nonRepresentableObstructionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerRepresentation             = 4330;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBrownRepresentabilityRepresented;

        private int _representableFunctors;
        private int _representationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RepresentableFunctorsNeeded        => _representableFunctorsNeeded;
        public int   NonRepresentableObstructionsPerBot => _nonRepresentableObstructionsPerBot;
        public int   BonusPerRepresentation             => _bonusPerRepresentation;
        public int   RepresentableFunctors              => _representableFunctors;
        public int   RepresentationCount                => _representationCount;
        public int   TotalBonusAwarded                  => _totalBonusAwarded;
        public float RepresentableFunctorProgress => _representableFunctorsNeeded > 0
            ? Mathf.Clamp01(_representableFunctors / (float)_representableFunctorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _representableFunctors = Mathf.Min(_representableFunctors + 1, _representableFunctorsNeeded);
            if (_representableFunctors >= _representableFunctorsNeeded)
            {
                int bonus = _bonusPerRepresentation;
                _representationCount++;
                _totalBonusAwarded      += bonus;
                _representableFunctors   = 0;
                _onBrownRepresentabilityRepresented?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _representableFunctors = Mathf.Max(0, _representableFunctors - _nonRepresentableObstructionsPerBot);
        }

        public void Reset()
        {
            _representableFunctors = 0;
            _representationCount   = 0;
            _totalBonusAwarded     = 0;
        }
    }
}

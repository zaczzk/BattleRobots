using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureConstellation", order = 229)]
    public sealed class ZoneControlCaptureConstellationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _starsNeeded            = 6;
        [SerializeField, Min(0)] private int _bonusPerConstellation  = 450;
        [SerializeField, Min(1)] private int _botScatterCount        = 2;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onConstellationFormed;

        private int _activeStars;
        private int _constellationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StarsNeeded           => _starsNeeded;
        public int   BonusPerConstellation => _bonusPerConstellation;
        public int   BotScatterCount       => _botScatterCount;
        public int   ActiveStars           => _activeStars;
        public int   ConstellationCount    => _constellationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float StarProgress          => _starsNeeded > 0
            ? Mathf.Clamp01(_activeStars / (float)_starsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _activeStars++;
            if (_activeStars >= _starsNeeded)
                return FormConstellation();
            return 0;
        }

        public void RecordBotCapture()
        {
            _activeStars = Mathf.Max(0, _activeStars - _botScatterCount);
        }

        private int FormConstellation()
        {
            _constellationCount++;
            _totalBonusAwarded += _bonusPerConstellation;
            _activeStars        = 0;
            _onConstellationFormed?.Raise();
            return _bonusPerConstellation;
        }

        public void Reset()
        {
            _activeStars        = 0;
            _constellationCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpringboard", order = 295)]
    public sealed class ZoneControlCaptureSpringboardSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bouncesNeeded  = 5;
        [SerializeField, Min(1)] private int _dampPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerLaunch = 1165;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpringboardLaunched;

        private int _bounces;
        private int _launchCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BouncesNeeded     => _bouncesNeeded;
        public int   DampPerBot        => _dampPerBot;
        public int   BonusPerLaunch    => _bonusPerLaunch;
        public int   Bounces           => _bounces;
        public int   LaunchCount       => _launchCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BounceProgress    => _bouncesNeeded > 0
            ? Mathf.Clamp01(_bounces / (float)_bouncesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bounces = Mathf.Min(_bounces + 1, _bouncesNeeded);
            if (_bounces >= _bouncesNeeded)
            {
                int bonus = _bonusPerLaunch;
                _launchCount++;
                _totalBonusAwarded += bonus;
                _bounces            = 0;
                _onSpringboardLaunched?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bounces = Mathf.Max(0, _bounces - _dampPerBot);
        }

        public void Reset()
        {
            _bounces           = 0;
            _launchCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePersistentHomology", order = 468)]
    public sealed class ZoneControlCapturePersistentHomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _barsNeeded       = 6;
        [SerializeField, Min(1)] private int _killPerBot        = 2;
        [SerializeField, Min(0)] private int _bonusPerPersist  = 3760;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPersistentHomologyPersisted;

        private int _bars;
        private int _persistCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BarsNeeded          => _barsNeeded;
        public int   KillPerBot          => _killPerBot;
        public int   BonusPerPersist     => _bonusPerPersist;
        public int   Bars                => _bars;
        public int   PersistCount        => _persistCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float BarProgress         => _barsNeeded > 0
            ? Mathf.Clamp01(_bars / (float)_barsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bars = Mathf.Min(_bars + 1, _barsNeeded);
            if (_bars >= _barsNeeded)
            {
                int bonus = _bonusPerPersist;
                _persistCount++;
                _totalBonusAwarded += bonus;
                _bars               = 0;
                _onPersistentHomologyPersisted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bars = Mathf.Max(0, _bars - _killPerBot);
        }

        public void Reset()
        {
            _bars              = 0;
            _persistCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

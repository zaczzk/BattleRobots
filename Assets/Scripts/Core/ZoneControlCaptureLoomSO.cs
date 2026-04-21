using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLoom", order = 280)]
    public sealed class ZoneControlCaptureLoomSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _threadsNeeded = 6;
        [SerializeField, Min(1)] private int _tanglePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerWeave = 940;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLoomWoven;

        private int _threads;
        private int _weaveCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ThreadsNeeded     => _threadsNeeded;
        public int   TanglePerBot      => _tanglePerBot;
        public int   BonusPerWeave     => _bonusPerWeave;
        public int   Threads           => _threads;
        public int   WeaveCount        => _weaveCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ThreadProgress    => _threadsNeeded > 0
            ? Mathf.Clamp01(_threads / (float)_threadsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _threads = Mathf.Min(_threads + 1, _threadsNeeded);
            if (_threads >= _threadsNeeded)
            {
                int bonus = _bonusPerWeave;
                _weaveCount++;
                _totalBonusAwarded += bonus;
                _threads            = 0;
                _onLoomWoven?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _threads = Mathf.Max(0, _threads - _tanglePerBot);
        }

        public void Reset()
        {
            _threads           = 0;
            _weaveCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

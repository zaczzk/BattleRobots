using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureQuake", order = 216)]
    public sealed class ZoneControlCaptureQuakeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesPerQuake = 5;
        [SerializeField, Min(0)] private int _bonusPerQuake    = 300;
        [SerializeField, Min(1)] private int _coolingPerBot    = 1;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onQuake;

        private int _tremorCount;
        private int _quakeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesPerQuake  => _capturesPerQuake;
        public int   BonusPerQuake     => _bonusPerQuake;
        public int   CoolingPerBot     => _coolingPerBot;
        public int   TremorCount       => _tremorCount;
        public int   QuakeCount        => _quakeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TremorProgress    => _capturesPerQuake > 0
            ? Mathf.Clamp01(_tremorCount / (float)_capturesPerQuake)
            : 0f;

        public int RecordPlayerCapture()
        {
            _tremorCount++;
            if (_tremorCount >= _capturesPerQuake)
            {
                Quake();
                return _bonusPerQuake;
            }
            return 0;
        }

        private void Quake()
        {
            _quakeCount++;
            _totalBonusAwarded += _bonusPerQuake;
            _tremorCount        = 0;
            _onQuake?.Raise();
        }

        public void RecordBotCapture()
        {
            _tremorCount = Mathf.Max(0, _tremorCount - _coolingPerBot);
        }

        public void Reset()
        {
            _tremorCount       = 0;
            _quakeCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

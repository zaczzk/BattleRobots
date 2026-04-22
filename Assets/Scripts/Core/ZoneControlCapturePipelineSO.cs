using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePipeline", order = 336)]
    public sealed class ZoneControlCapturePipelineSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stagesNeeded      = 5;
        [SerializeField, Min(1)] private int _flushPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerPipeline  = 1780;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPipelineFlushed;

        private int _stages;
        private int _flushCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StagesNeeded      => _stagesNeeded;
        public int   FlushPerBot       => _flushPerBot;
        public int   BonusPerPipeline  => _bonusPerPipeline;
        public int   Stages            => _stages;
        public int   FlushCount        => _flushCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StageProgress     => _stagesNeeded > 0
            ? Mathf.Clamp01(_stages / (float)_stagesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stages = Mathf.Min(_stages + 1, _stagesNeeded);
            if (_stages >= _stagesNeeded)
            {
                int bonus = _bonusPerPipeline;
                _flushCount++;
                _totalBonusAwarded += bonus;
                _stages             = 0;
                _onPipelineFlushed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stages = Mathf.Max(0, _stages - _flushPerBot);
        }

        public void Reset()
        {
            _stages            = 0;
            _flushCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSequencer", order = 334)]
    public sealed class ZoneControlCaptureSequencerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stepsNeeded       = 8;
        [SerializeField, Min(1)] private int _skipPerBot        = 2;
        [SerializeField, Min(0)] private int _bonusPerSequence  = 1750;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSequencerAdvanced;

        private int _steps;
        private int _sequenceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StepsNeeded       => _stepsNeeded;
        public int   SkipPerBot        => _skipPerBot;
        public int   BonusPerSequence  => _bonusPerSequence;
        public int   Steps             => _steps;
        public int   SequenceCount     => _sequenceCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StepProgress      => _stepsNeeded > 0
            ? Mathf.Clamp01(_steps / (float)_stepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _steps = Mathf.Min(_steps + 1, _stepsNeeded);
            if (_steps >= _stepsNeeded)
            {
                int bonus = _bonusPerSequence;
                _sequenceCount++;
                _totalBonusAwarded += bonus;
                _steps              = 0;
                _onSequencerAdvanced?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _steps = Mathf.Max(0, _steps - _skipPerBot);
        }

        public void Reset()
        {
            _steps             = 0;
            _sequenceCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

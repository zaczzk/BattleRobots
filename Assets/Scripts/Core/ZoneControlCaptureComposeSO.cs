using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCompose", order = 361)]
    public sealed class ZoneControlCaptureComposeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stepsNeeded      = 5;
        [SerializeField, Min(1)] private int _decomposePerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerCompose  = 2155;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onComposeComplete;

        private int _steps;
        private int _composeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StepsNeeded       => _stepsNeeded;
        public int   DecomposePerBot   => _decomposePerBot;
        public int   BonusPerCompose   => _bonusPerCompose;
        public int   Steps             => _steps;
        public int   ComposeCount      => _composeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ComposeProgress   => _stepsNeeded > 0
            ? Mathf.Clamp01(_steps / (float)_stepsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _steps = Mathf.Min(_steps + 1, _stepsNeeded);
            if (_steps >= _stepsNeeded)
            {
                int bonus = _bonusPerCompose;
                _composeCount++;
                _totalBonusAwarded += bonus;
                _steps              = 0;
                _onComposeComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _steps = Mathf.Max(0, _steps - _decomposePerBot);
        }

        public void Reset()
        {
            _steps             = 0;
            _composeCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

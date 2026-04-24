using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHomotopyGroup", order = 483)]
    public sealed class ZoneControlCaptureHomotopyGroupSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _loopsNeeded      = 5;
        [SerializeField, Min(1)] private int _contractPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerCompute  = 3985;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHomotopyGroupComputed;

        private int _loops;
        private int _computeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LoopsNeeded        => _loopsNeeded;
        public int   ContractPerBot     => _contractPerBot;
        public int   BonusPerCompute    => _bonusPerCompute;
        public int   Loops              => _loops;
        public int   ComputeCount       => _computeCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float LoopProgress       => _loopsNeeded > 0
            ? Mathf.Clamp01(_loops / (float)_loopsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _loops = Mathf.Min(_loops + 1, _loopsNeeded);
            if (_loops >= _loopsNeeded)
            {
                int bonus = _bonusPerCompute;
                _computeCount++;
                _totalBonusAwarded += bonus;
                _loops              = 0;
                _onHomotopyGroupComputed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _loops = Mathf.Max(0, _loops - _contractPerBot);
        }

        public void Reset()
        {
            _loops             = 0;
            _computeCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

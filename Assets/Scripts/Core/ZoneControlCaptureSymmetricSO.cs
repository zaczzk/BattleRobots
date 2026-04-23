using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSymmetric", order = 424)]
    public sealed class ZoneControlCaptureSymmetricSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _swapsNeeded      = 6;
        [SerializeField, Min(1)] private int _transposePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerSymmetry = 3100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSymmetrized;

        private int _swaps;
        private int _symmetryCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SwapsNeeded       => _swapsNeeded;
        public int   TransposePerBot   => _transposePerBot;
        public int   BonusPerSymmetry  => _bonusPerSymmetry;
        public int   Swaps             => _swaps;
        public int   SymmetryCount     => _symmetryCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SwapProgress      => _swapsNeeded > 0
            ? Mathf.Clamp01(_swaps / (float)_swapsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _swaps = Mathf.Min(_swaps + 1, _swapsNeeded);
            if (_swaps >= _swapsNeeded)
            {
                int bonus = _bonusPerSymmetry;
                _symmetryCount++;
                _totalBonusAwarded += bonus;
                _swaps              = 0;
                _onSymmetrized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _swaps = Mathf.Max(0, _swaps - _transposePerBot);
        }

        public void Reset()
        {
            _swaps             = 0;
            _symmetryCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureKernel", order = 405)]
    public sealed class ZoneControlCaptureKernelSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _elementsNeeded  = 7;
        [SerializeField, Min(1)] private int _dissolvePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerKernel  = 2815;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onKernelComputed;

        private int _elements;
        private int _kernelCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ElementsNeeded    => _elementsNeeded;
        public int   DissolvePerBot    => _dissolvePerBot;
        public int   BonusPerKernel    => _bonusPerKernel;
        public int   Elements          => _elements;
        public int   KernelCount       => _kernelCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ElementProgress   => _elementsNeeded > 0
            ? Mathf.Clamp01(_elements / (float)_elementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _elements = Mathf.Min(_elements + 1, _elementsNeeded);
            if (_elements >= _elementsNeeded)
            {
                int bonus = _bonusPerKernel;
                _kernelCount++;
                _totalBonusAwarded += bonus;
                _elements           = 0;
                _onKernelComputed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _elements = Mathf.Max(0, _elements - _dissolvePerBot);
        }

        public void Reset()
        {
            _elements          = 0;
            _kernelCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCokernel", order = 406)]
    public sealed class ZoneControlCaptureCokernelSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _vectorsNeeded    = 5;
        [SerializeField, Min(1)] private int _erasePerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerCokernel = 2830;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCokernelProjected;

        private int _vectors;
        private int _cokernelCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VectorsNeeded     => _vectorsNeeded;
        public int   ErasePerBot       => _erasePerBot;
        public int   BonusPerCokernel  => _bonusPerCokernel;
        public int   Vectors           => _vectors;
        public int   CokernelCount     => _cokernelCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float VectorProgress    => _vectorsNeeded > 0
            ? Mathf.Clamp01(_vectors / (float)_vectorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _vectors = Mathf.Min(_vectors + 1, _vectorsNeeded);
            if (_vectors >= _vectorsNeeded)
            {
                int bonus = _bonusPerCokernel;
                _cokernelCount++;
                _totalBonusAwarded += bonus;
                _vectors            = 0;
                _onCokernelProjected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _vectors = Mathf.Max(0, _vectors - _erasePerBot);
        }

        public void Reset()
        {
            _vectors           = 0;
            _cokernelCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

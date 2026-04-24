using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGalois", order = 442)]
    public sealed class ZoneControlCaptureGaloisSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _closuresNeeded  = 5;
        [SerializeField, Min(1)] private int _breakPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerConnect = 3370;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGaloisConnected;

        private int _closures;
        private int _connectionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ClosuresNeeded    => _closuresNeeded;
        public int   BreakPerBot       => _breakPerBot;
        public int   BonusPerConnect   => _bonusPerConnect;
        public int   Closures          => _closures;
        public int   ConnectionCount   => _connectionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ClosureProgress   => _closuresNeeded > 0
            ? Mathf.Clamp01(_closures / (float)_closuresNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _closures = Mathf.Min(_closures + 1, _closuresNeeded);
            if (_closures >= _closuresNeeded)
            {
                int bonus = _bonusPerConnect;
                _connectionCount++;
                _totalBonusAwarded += bonus;
                _closures           = 0;
                _onGaloisConnected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _closures = Mathf.Max(0, _closures - _breakPerBot);
        }

        public void Reset()
        {
            _closures          = 0;
            _connectionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

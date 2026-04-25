using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFunctoriality", order = 501)]
    public sealed class ZoneControlCaptureFunctorialitySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _transfersNeeded           = 7;
        [SerializeField, Min(1)] private int _ramifiedObstructionsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerRealization        = 4255;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFunctorialityRealized;

        private int _transfers;
        private int _realizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TransfersNeeded            => _transfersNeeded;
        public int   RamifiedObstructionsPerBot => _ramifiedObstructionsPerBot;
        public int   BonusPerRealization         => _bonusPerRealization;
        public int   Transfers                   => _transfers;
        public int   RealizationCount            => _realizationCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float TransferProgress => _transfersNeeded > 0
            ? Mathf.Clamp01(_transfers / (float)_transfersNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _transfers = Mathf.Min(_transfers + 1, _transfersNeeded);
            if (_transfers >= _transfersNeeded)
            {
                int bonus = _bonusPerRealization;
                _realizationCount++;
                _totalBonusAwarded += bonus;
                _transfers          = 0;
                _onFunctorialityRealized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _transfers = Mathf.Max(0, _transfers - _ramifiedObstructionsPerBot);
        }

        public void Reset()
        {
            _transfers         = 0;
            _realizationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

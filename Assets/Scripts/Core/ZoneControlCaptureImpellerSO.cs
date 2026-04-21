using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureImpeller", order = 305)]
    public sealed class ZoneControlCaptureImpellerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _vanesNeeded   = 5;
        [SerializeField, Min(1)] private int _slipPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerPump  = 1315;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onImpellerPumped;

        private int _vanes;
        private int _pumpCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VanesNeeded       => _vanesNeeded;
        public int   SlipPerBot        => _slipPerBot;
        public int   BonusPerPump      => _bonusPerPump;
        public int   Vanes             => _vanes;
        public int   PumpCount         => _pumpCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float VaneProgress      => _vanesNeeded > 0
            ? Mathf.Clamp01(_vanes / (float)_vanesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _vanes = Mathf.Min(_vanes + 1, _vanesNeeded);
            if (_vanes >= _vanesNeeded)
            {
                int bonus = _bonusPerPump;
                _pumpCount++;
                _totalBonusAwarded += bonus;
                _vanes              = 0;
                _onImpellerPumped?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _vanes = Mathf.Max(0, _vanes - _slipPerBot);
        }

        public void Reset()
        {
            _vanes             = 0;
            _pumpCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

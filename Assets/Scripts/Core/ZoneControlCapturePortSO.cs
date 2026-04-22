using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePort", order = 346)]
    public sealed class ZoneControlCapturePortSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _portsNeeded  = 5;
        [SerializeField, Min(1)] private int _closePerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerBind = 1930;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPortOpened;

        private int _ports;
        private int _bindCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PortsNeeded       => _portsNeeded;
        public int   ClosePerBot       => _closePerBot;
        public int   BonusPerBind      => _bonusPerBind;
        public int   Ports             => _ports;
        public int   BindCount         => _bindCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PortProgress      => _portsNeeded > 0
            ? Mathf.Clamp01(_ports / (float)_portsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ports = Mathf.Min(_ports + 1, _portsNeeded);
            if (_ports >= _portsNeeded)
            {
                int bonus = _bonusPerBind;
                _bindCount++;
                _totalBonusAwarded += bonus;
                _ports              = 0;
                _onPortOpened?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ports = Mathf.Max(0, _ports - _closePerBot);
        }

        public void Reset()
        {
            _ports             = 0;
            _bindCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

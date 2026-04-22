using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePacket", order = 343)]
    public sealed class ZoneControlCapturePacketSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _payloadsNeeded   = 6;
        [SerializeField, Min(1)] private int _fragmentPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerDelivery = 1885;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPacketDelivered;

        private int _payloads;
        private int _deliveryCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PayloadsNeeded     => _payloadsNeeded;
        public int   FragmentPerBot     => _fragmentPerBot;
        public int   BonusPerDelivery   => _bonusPerDelivery;
        public int   Payloads           => _payloads;
        public int   DeliveryCount      => _deliveryCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float PayloadProgress    => _payloadsNeeded > 0
            ? Mathf.Clamp01(_payloads / (float)_payloadsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _payloads = Mathf.Min(_payloads + 1, _payloadsNeeded);
            if (_payloads >= _payloadsNeeded)
            {
                int bonus = _bonusPerDelivery;
                _deliveryCount++;
                _totalBonusAwarded += bonus;
                _payloads           = 0;
                _onPacketDelivered?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _payloads = Mathf.Max(0, _payloads - _fragmentPerBot);
        }

        public void Reset()
        {
            _payloads          = 0;
            _deliveryCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

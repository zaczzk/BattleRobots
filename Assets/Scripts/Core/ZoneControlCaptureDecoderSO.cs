using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDecoder", order = 329)]
    public sealed class ZoneControlCaptureDecoderSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _packetsNeeded  = 7;
        [SerializeField, Min(1)] private int _dropPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerDecode = 1675;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDecoderDecoded;

        private int _packets;
        private int _decodeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PacketsNeeded     => _packetsNeeded;
        public int   DropPerBot        => _dropPerBot;
        public int   BonusPerDecode    => _bonusPerDecode;
        public int   Packets           => _packets;
        public int   DecodeCount       => _decodeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PacketProgress    => _packetsNeeded > 0
            ? Mathf.Clamp01(_packets / (float)_packetsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _packets = Mathf.Min(_packets + 1, _packetsNeeded);
            if (_packets >= _packetsNeeded)
            {
                int bonus = _bonusPerDecode;
                _decodeCount++;
                _totalBonusAwarded += bonus;
                _packets            = 0;
                _onDecoderDecoded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _packets = Mathf.Max(0, _packets - _dropPerBot);
        }

        public void Reset()
        {
            _packets           = 0;
            _decodeCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

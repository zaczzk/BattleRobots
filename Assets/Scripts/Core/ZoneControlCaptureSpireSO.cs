using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpire", order = 244)]
    public sealed class ZoneControlCaptureSpireSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _channelsNeeded  = 4;
        [SerializeField, Min(1)] private int _disruptionPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerChannel  = 440;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpireChanneled;

        private int _energy;
        private int _channelCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChannelsNeeded    => _channelsNeeded;
        public int   DisruptionPerBot  => _disruptionPerBot;
        public int   BonusPerChannel   => _bonusPerChannel;
        public int   Energy            => _energy;
        public int   ChannelCount      => _channelCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float EnergyProgress    => _channelsNeeded > 0
            ? Mathf.Clamp01(_energy / (float)_channelsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _energy = Mathf.Min(_energy + 1, _channelsNeeded);
            if (_energy >= _channelsNeeded)
            {
                int bonus = _bonusPerChannel;
                _channelCount++;
                _totalBonusAwarded += bonus;
                _energy             = 0;
                _onSpireChanneled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _energy = Mathf.Max(0, _energy - _disruptionPerBot);
        }

        public void Reset()
        {
            _energy            = 0;
            _channelCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

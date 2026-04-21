using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCandelabra", order = 268)]
    public sealed class ZoneControlCaptureCandelabraSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _flamesNeeded          = 6;
        [SerializeField, Min(1)] private int _snuffPerBot           = 2;
        [SerializeField, Min(0)] private int _bonusPerIllumination  = 760;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCandelabraIlluminated;

        private int _flames;
        private int _illuminationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FlamesNeeded          => _flamesNeeded;
        public int   SnuffPerBot           => _snuffPerBot;
        public int   BonusPerIllumination  => _bonusPerIllumination;
        public int   Flames                => _flames;
        public int   IlluminationCount     => _illuminationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float FlameProgress         => _flamesNeeded > 0
            ? Mathf.Clamp01(_flames / (float)_flamesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _flames = Mathf.Min(_flames + 1, _flamesNeeded);
            if (_flames >= _flamesNeeded)
            {
                int bonus = _bonusPerIllumination;
                _illuminationCount++;
                _totalBonusAwarded += bonus;
                _flames             = 0;
                _onCandelabraIlluminated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _flames = Mathf.Max(0, _flames - _snuffPerBot);
        }

        public void Reset()
        {
            _flames            = 0;
            _illuminationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

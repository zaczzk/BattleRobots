using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHammer", order = 276)]
    public sealed class ZoneControlCaptureHammerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _strikesNeeded = 5;
        [SerializeField, Min(1)] private int _coolPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerForge = 880;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHammerForged;

        private int _strikes;
        private int _forgeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StrikesNeeded     => _strikesNeeded;
        public int   CoolPerBot        => _coolPerBot;
        public int   BonusPerForge     => _bonusPerForge;
        public int   Strikes           => _strikes;
        public int   ForgeCount        => _forgeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StrikeProgress    => _strikesNeeded > 0
            ? Mathf.Clamp01(_strikes / (float)_strikesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _strikes = Mathf.Min(_strikes + 1, _strikesNeeded);
            if (_strikes >= _strikesNeeded)
            {
                int bonus = _bonusPerForge;
                _forgeCount++;
                _totalBonusAwarded += bonus;
                _strikes            = 0;
                _onHammerForged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _strikes = Mathf.Max(0, _strikes - _coolPerBot);
        }

        public void Reset()
        {
            _strikes           = 0;
            _forgeCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

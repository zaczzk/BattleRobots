using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAltar", order = 239)]
    public sealed class ZoneControlCaptureAltarSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesNeeded      = 5;
        [SerializeField, Min(1)] private int _desecrationPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerConsecration = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAltarConsecrated;

        private int _offerings;
        private int _consecrationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesNeeded       => _capturesNeeded;
        public int   DesecrationPerBot    => _desecrationPerBot;
        public int   BonusPerConsecration => _bonusPerConsecration;
        public int   Offerings            => _offerings;
        public int   ConsecrationCount    => _consecrationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float OfferingProgress     => _capturesNeeded > 0
            ? Mathf.Clamp01(_offerings / (float)_capturesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _offerings = Mathf.Min(_offerings + 1, _capturesNeeded);
            if (_offerings >= _capturesNeeded)
            {
                int bonus = _bonusPerConsecration;
                _consecrationCount++;
                _totalBonusAwarded += bonus;
                _offerings          = 0;
                _onAltarConsecrated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _offerings = Mathf.Max(0, _offerings - _desecrationPerBot);
        }

        public void Reset()
        {
            _offerings          = 0;
            _consecrationCount  = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

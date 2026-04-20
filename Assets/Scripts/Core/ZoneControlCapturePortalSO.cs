using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePortal", order = 240)]
    public sealed class ZoneControlCapturePortalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargesForActivation = 4;
        [SerializeField, Min(1)] private int _drainPerBot          = 1;
        [SerializeField, Min(0)] private int _bonusPerActivation   = 425;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPortalActivated;

        private int _charges;
        private int _activationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargesForActivation => _chargesForActivation;
        public int   DrainPerBot          => _drainPerBot;
        public int   BonusPerActivation   => _bonusPerActivation;
        public int   Charges              => _charges;
        public int   ActivationCount      => _activationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float ChargeProgress       => _chargesForActivation > 0
            ? Mathf.Clamp01(_charges / (float)_chargesForActivation)
            : 0f;

        public int RecordPlayerCapture()
        {
            _charges = Mathf.Min(_charges + 1, _chargesForActivation);
            if (_charges >= _chargesForActivation)
            {
                int bonus = _bonusPerActivation;
                _activationCount++;
                _totalBonusAwarded += bonus;
                _charges            = 0;
                _onPortalActivated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _charges = Mathf.Max(0, _charges - _drainPerBot);
        }

        public void Reset()
        {
            _charges           = 0;
            _activationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

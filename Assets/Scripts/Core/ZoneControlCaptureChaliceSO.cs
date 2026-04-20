using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureChalice", order = 250)]
    public sealed class ZoneControlCaptureChaliceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _offeringsNeeded = 4;
        [SerializeField, Min(1)] private int _drainPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerFilling = 520;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChaliceFilled;

        private int _offerings;
        private int _fillingCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OfferingsNeeded   => _offeringsNeeded;
        public int   DrainPerBot       => _drainPerBot;
        public int   BonusPerFilling   => _bonusPerFilling;
        public int   Offerings         => _offerings;
        public int   FillingCount      => _fillingCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float OfferingProgress  => _offeringsNeeded > 0
            ? Mathf.Clamp01(_offerings / (float)_offeringsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _offerings = Mathf.Min(_offerings + 1, _offeringsNeeded);
            if (_offerings >= _offeringsNeeded)
            {
                int bonus = _bonusPerFilling;
                _fillingCount++;
                _totalBonusAwarded += bonus;
                _offerings          = 0;
                _onChaliceFilled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _offerings = Mathf.Max(0, _offerings - _drainPerBot);
        }

        public void Reset()
        {
            _offerings         = 0;
            _fillingCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

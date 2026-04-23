using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCommaObject", order = 418)]
    public sealed class ZoneControlCaptureCommaObjectSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _arcsNeeded      = 7;
        [SerializeField, Min(1)] private int _contractPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerComma   = 3010;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCommaObjectFormed;

        private int _arcs;
        private int _commaCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ArcsNeeded        => _arcsNeeded;
        public int   ContractPerBot    => _contractPerBot;
        public int   BonusPerComma     => _bonusPerComma;
        public int   Arcs              => _arcs;
        public int   CommaCount        => _commaCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ArcProgress       => _arcsNeeded > 0
            ? Mathf.Clamp01(_arcs / (float)_arcsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _arcs = Mathf.Min(_arcs + 1, _arcsNeeded);
            if (_arcs >= _arcsNeeded)
            {
                int bonus = _bonusPerComma;
                _commaCount++;
                _totalBonusAwarded += bonus;
                _arcs               = 0;
                _onCommaObjectFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _arcs = Mathf.Max(0, _arcs - _contractPerBot);
        }

        public void Reset()
        {
            _arcs              = 0;
            _commaCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGroupoid", order = 449)]
    public sealed class ZoneControlCaptureGroupoidSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _morphismsNeeded   = 5;
        [SerializeField, Min(1)] private int _composePerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerInversion = 3475;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGroupoidInverted;

        private int _morphisms;
        private int _inversionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MorphismsNeeded   => _morphismsNeeded;
        public int   ComposePerBot     => _composePerBot;
        public int   BonusPerInversion => _bonusPerInversion;
        public int   Morphisms         => _morphisms;
        public int   InversionCount    => _inversionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float GroupoidProgress  => _morphismsNeeded > 0
            ? Mathf.Clamp01(_morphisms / (float)_morphismsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _morphisms = Mathf.Min(_morphisms + 1, _morphismsNeeded);
            if (_morphisms >= _morphismsNeeded)
            {
                int bonus = _bonusPerInversion;
                _inversionCount++;
                _totalBonusAwarded += bonus;
                _morphisms          = 0;
                _onGroupoidInverted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _morphisms = Mathf.Max(0, _morphisms - _composePerBot);
        }

        public void Reset()
        {
            _morphisms         = 0;
            _inversionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

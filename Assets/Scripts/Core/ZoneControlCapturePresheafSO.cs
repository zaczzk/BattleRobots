using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePresheaf", order = 398)]
    public sealed class ZoneControlCapturePresheafSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _restrictionsNeeded = 5;
        [SerializeField, Min(1)] private int _dissolvePerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerPresheaf   = 2710;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPresheafFormed;

        private int _restrictions;
        private int _presheafCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RestrictionsNeeded  => _restrictionsNeeded;
        public int   DissolvePerBot      => _dissolvePerBot;
        public int   BonusPerPresheaf    => _bonusPerPresheaf;
        public int   Restrictions        => _restrictions;
        public int   PresheafCount       => _presheafCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float RestrictionProgress => _restrictionsNeeded > 0
            ? Mathf.Clamp01(_restrictions / (float)_restrictionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _restrictions = Mathf.Min(_restrictions + 1, _restrictionsNeeded);
            if (_restrictions >= _restrictionsNeeded)
            {
                int bonus = _bonusPerPresheaf;
                _presheafCount++;
                _totalBonusAwarded += bonus;
                _restrictions       = 0;
                _onPresheafFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _restrictions = Mathf.Max(0, _restrictions - _dissolvePerBot);
        }

        public void Reset()
        {
            _restrictions      = 0;
            _presheafCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

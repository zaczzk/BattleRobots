using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureApplicative", order = 367)]
    public sealed class ZoneControlCaptureApplicativeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _applicationsNeeded  = 5;
        [SerializeField, Min(1)] private int _removePerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerApplication = 2245;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onApplicativeApplied;

        private int _applications;
        private int _applyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ApplicationsNeeded    => _applicationsNeeded;
        public int   RemovePerBot          => _removePerBot;
        public int   BonusPerApplication   => _bonusPerApplication;
        public int   Applications          => _applications;
        public int   ApplyCount            => _applyCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float ApplicationProgress   => _applicationsNeeded > 0
            ? Mathf.Clamp01(_applications / (float)_applicationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _applications = Mathf.Min(_applications + 1, _applicationsNeeded);
            if (_applications >= _applicationsNeeded)
            {
                int bonus = _bonusPerApplication;
                _applyCount++;
                _totalBonusAwarded += bonus;
                _applications       = 0;
                _onApplicativeApplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _applications = Mathf.Max(0, _applications - _removePerBot);
        }

        public void Reset()
        {
            _applications      = 0;
            _applyCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchPivot", order = 158)]
    public sealed class ZoneControlMatchPivotSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerPivot = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPivotAchieved;

        private int  _pivotCount;
        private int  _totalBonusAwarded;
        private bool _wasPlayerLeading;
        private bool _leadEstablished;

        private void OnEnable() => Reset();

        public int  PivotCount        => _pivotCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public int  BonusPerPivot     => _bonusPerPivot;
        public bool LeadEstablished   => _leadEstablished;

        public void RecordLeadState(bool playerLeading)
        {
            if (!_leadEstablished)
            {
                _leadEstablished  = true;
                _wasPlayerLeading = playerLeading;
                return;
            }

            if (playerLeading == _wasPlayerLeading) return;

            _wasPlayerLeading = playerLeading;

            if (playerLeading)
            {
                _pivotCount++;
                _totalBonusAwarded += _bonusPerPivot;
                _onPivotAchieved?.Raise();
            }
        }

        public void Reset()
        {
            _pivotCount        = 0;
            _totalBonusAwarded = 0;
            _wasPlayerLeading  = false;
            _leadEstablished   = false;
        }
    }
}

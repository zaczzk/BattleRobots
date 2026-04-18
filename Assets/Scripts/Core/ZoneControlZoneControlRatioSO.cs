using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneControlRatio", order = 101)]
    public sealed class ZoneControlZoneControlRatioSO : ScriptableObject
    {
        [Header("Majority Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float _majorityThreshold = 0.5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMajorityChanged;
        [SerializeField] private VoidGameEvent _onRatioUpdated;

        private int  _playerZones;
        private int  _totalZones;
        private bool _hasMajority;

        private void OnEnable() => Reset();

        public int   PlayerZones       => _playerZones;
        public int   TotalZones        => _totalZones;
        public bool  HasMajority       => _hasMajority;
        public float MajorityThreshold => _majorityThreshold;

        public float HoldRatio =>
            _totalZones > 0
                ? Mathf.Clamp01((float)_playerZones / _totalZones)
                : 0f;

        public void SetZoneCounts(int playerZones, int totalZones)
        {
            _playerZones = Mathf.Max(0, playerZones);
            _totalZones  = Mathf.Max(0, totalZones);

            bool nowHasMajority = _totalZones > 0 && HoldRatio > _majorityThreshold;
            if (nowHasMajority != _hasMajority)
            {
                _hasMajority = nowHasMajority;
                _onMajorityChanged?.Raise();
            }

            _onRatioUpdated?.Raise();
        }

        public void Reset()
        {
            _playerZones = 0;
            _totalZones  = 0;
            _hasMajority = false;
        }
    }
}

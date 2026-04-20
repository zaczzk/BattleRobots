using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCatapult", order = 237)]
    public sealed class ZoneControlCaptureCatapultSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _projectilesPerLaunch = 6;
        [SerializeField, Min(1)] private int _unloadPerBot         = 2;
        [SerializeField, Min(0)] private int _bonusPerLaunch       = 420;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLaunch;

        private int _loadedProjectiles;
        private int _launchCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ProjectilesPerLaunch => _projectilesPerLaunch;
        public int   UnloadPerBot         => _unloadPerBot;
        public int   BonusPerLaunch       => _bonusPerLaunch;
        public int   LoadedProjectiles    => _loadedProjectiles;
        public int   LaunchCount          => _launchCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float LoadProgress         => _projectilesPerLaunch > 0
            ? Mathf.Clamp01(_loadedProjectiles / (float)_projectilesPerLaunch)
            : 0f;

        public int RecordPlayerCapture()
        {
            _loadedProjectiles = Mathf.Min(_loadedProjectiles + 1, _projectilesPerLaunch);
            if (_loadedProjectiles >= _projectilesPerLaunch)
            {
                int bonus = _bonusPerLaunch;
                _launchCount++;
                _totalBonusAwarded  += bonus;
                _loadedProjectiles   = 0;
                _onLaunch?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _loadedProjectiles = Mathf.Max(0, _loadedProjectiles - _unloadPerBot);
        }

        public void Reset()
        {
            _loadedProjectiles = 0;
            _launchCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

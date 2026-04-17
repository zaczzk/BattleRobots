using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Config SO that maps <see cref="ThreatLevel"/> to a normalised pressure
    /// boost value and fires <c>_onBridgeActivated</c> when a non-zero boost
    /// is applied.  Consumers read <see cref="LastBoostApplied"/> to act on it.
    ///
    /// <see cref="ApplyBridge"/> records the most recent threat level and boost.
    /// <see cref="Reset"/> clears runtime state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlThreatPressureBridge.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlThreatPressureBridge", order = 80)]
    public sealed class ZoneControlThreatPressureBridgeSO : ScriptableObject
    {
        [Header("Pressure Boost Per Threat Level")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowThreatBoost = 0.0f;

        [Range(0f, 1f)]
        [SerializeField] private float _mediumThreatBoost = 0.2f;

        [Range(0f, 1f)]
        [SerializeField] private float _highThreatBoost = 0.5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBridgeActivated;

        private float       _lastBoostApplied;
        private ThreatLevel _lastLevel;
        private bool        _hasApplied;

        private void OnEnable() => Reset();

        public float       LastBoostApplied  => _lastBoostApplied;
        public ThreatLevel LastLevel         => _lastLevel;
        public bool        HasApplied        => _hasApplied;
        public float       LowThreatBoost    => _lowThreatBoost;
        public float       MediumThreatBoost => _mediumThreatBoost;
        public float       HighThreatBoost   => _highThreatBoost;

        /// <summary>Returns the configured boost for the given <paramref name="level"/>.</summary>
        public float GetBoostForThreat(ThreatLevel level)
        {
            switch (level)
            {
                case ThreatLevel.High:   return _highThreatBoost;
                case ThreatLevel.Medium: return _mediumThreatBoost;
                default:                 return _lowThreatBoost;
            }
        }

        /// <summary>
        /// Records the current threat level, caches the associated boost, and fires
        /// <c>_onBridgeActivated</c> when the boost is greater than zero.
        /// </summary>
        public void ApplyBridge(ThreatLevel level)
        {
            float boost       = GetBoostForThreat(level);
            _lastBoostApplied = boost;
            _lastLevel        = level;
            _hasApplied       = true;
            if (boost > 0f)
                _onBridgeActivated?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _lastBoostApplied = 0f;
            _lastLevel        = ThreatLevel.Low;
            _hasApplied       = false;
        }
    }
}

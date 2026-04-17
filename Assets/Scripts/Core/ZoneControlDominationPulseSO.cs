using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that fires a repeating "domination pulse" reward while the
    /// player maintains zone dominance.  Each <c>_pulseDuration</c> seconds of
    /// continuous dominance triggers one pulse and accumulates bonus currency.
    ///
    /// Call <see cref="StartDomination"/> when the player gains dominance and
    /// <see cref="EndDomination"/> when it is lost.  Drive <see cref="Tick"/>
    /// from a MonoBehaviour Update loop.
    /// Fires <c>_onPulseTriggered</c> per pulse; <see cref="Reset"/> clears all
    /// state silently and is called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlDominationPulse.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlDominationPulse", order = 81)]
    public sealed class ZoneControlDominationPulseSO : ScriptableObject
    {
        [Header("Pulse Settings")]
        [Min(1f)]
        [SerializeField] private float _pulseDuration = 10f;

        [Min(0)]
        [SerializeField] private int _pulseBonus = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPulseTriggered;

        private float _accumulated;
        private bool  _isDominating;
        private int   _totalPulseCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public bool  IsDominating      => _isDominating;
        public float PulseDuration     => _pulseDuration;
        public int   PulseBonus        => _pulseBonus;
        public int   TotalPulseCount   => _totalPulseCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;

        /// <summary>Normalised progress toward the next pulse [0, 1].</summary>
        public float PulseProgress =>
            _pulseDuration > 0f ? Mathf.Clamp01(_accumulated / _pulseDuration) : 0f;

        /// <summary>Arms the pulse timer.  Idempotent when already dominating.</summary>
        public void StartDomination()
        {
            _isDominating = true;
        }

        /// <summary>Disarms the pulse timer and resets accumulated time silently.</summary>
        public void EndDomination()
        {
            _isDominating = false;
            _accumulated  = 0f;
        }

        /// <summary>
        /// Advances the pulse timer by <paramref name="dt"/> seconds.
        /// No-op when not dominating.  Fires a pulse for each completed interval,
        /// supporting multi-pulse ticks.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isDominating) return;
            _accumulated += dt;
            while (_accumulated >= _pulseDuration)
            {
                _accumulated -= _pulseDuration;
                TriggerPulse();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _accumulated       = 0f;
            _isDominating      = false;
            _totalPulseCount   = 0;
            _totalBonusAwarded = 0;
        }

        private void TriggerPulse()
        {
            _totalPulseCount++;
            _totalBonusAwarded += _pulseBonus;
            _onPulseTriggered?.Raise();
        }
    }
}

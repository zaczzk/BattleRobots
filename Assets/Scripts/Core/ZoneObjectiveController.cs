using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that evaluates a <see cref="ZoneObjectiveSO"/> at match end
    /// and resets it at match start, bridging VoidGameEvent channels to the SO API.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onMatchStarted fires → ResetObjective():
    ///     • _objectiveSO?.Reset().
    ///   _onMatchEnded fires → EvaluateObjective():
    ///     • Reads _dominanceSO?.PlayerZoneCount (0 if null).
    ///     • Calls _objectiveSO?.Evaluate(count).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one controller per match manager.
    ///
    /// Scene wiring:
    ///   _objectiveSO    → ZoneObjectiveSO asset (defines required zone count).
    ///   _dominanceSO    → ZoneDominanceSO (provides PlayerZoneCount at eval).
    ///   _onMatchStarted → VoidGameEvent raised by MatchManager at match start.
    ///   _onMatchEnded   → VoidGameEvent raised by MatchManager at match end.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneObjectiveController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Zone objective SO defining the required zone count and completion event.")]
        [SerializeField] private ZoneObjectiveSO _objectiveSO;

        [Tooltip("Zone dominance SO providing the player's current zone count at evaluation.")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by MatchManager when the match begins. Calls ResetObjective().")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised by MatchManager when the match ends. Calls EvaluateObjective().")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _resetDelegate;
        private Action _evaluateDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _resetDelegate    = ResetObjective;
            _evaluateDelegate = EvaluateObjective;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_resetDelegate);
            _onMatchEnded?.RegisterCallback(_evaluateDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_resetDelegate);
            _onMatchEnded?.UnregisterCallback(_evaluateDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Resets the zone objective. Called when the match starts.
        /// Null-safe.
        /// </summary>
        public void ResetObjective()
        {
            _objectiveSO?.Reset();
        }

        /// <summary>
        /// Reads <see cref="ZoneDominanceSO.PlayerZoneCount"/> and passes it to
        /// <see cref="ZoneObjectiveSO.Evaluate"/>. Called when the match ends.
        /// Null-safe — treats null DominanceSO as 0 held zones.
        /// </summary>
        public void EvaluateObjective()
        {
            int count = _dominanceSO != null ? _dominanceSO.PlayerZoneCount : 0;
            _objectiveSO?.Evaluate(count);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneObjectiveSO"/>. May be null.</summary>
        public ZoneObjectiveSO ObjectiveSO => _objectiveSO;

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;
    }
}

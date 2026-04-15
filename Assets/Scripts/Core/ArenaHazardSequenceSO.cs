using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configuration ScriptableObject for <see cref="BattleRobots.Physics.ArenaHazardSequenceController"/>.
    /// Defines how long each hazard in a fixed sequence stays active before the controller
    /// cycles to the next one, and exposes an optional event channel fired on each advance.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • <see cref="CycleDuration"/> — seconds each hazard is active before the next
    ///     one takes over.  Minimum 0.1 s (enforced by [Min]).
    ///   • <see cref="OnSequenceAdvanced"/> — optional VoidGameEvent raised once per
    ///     cycle step by the controller (e.g. to drive audio or flash an indicator).
    ///   • This SO is configuration-only; all mutable state (current index, elapsed
    ///     timer) lives in the Physics-layer controller so the SO stays immutable at
    ///     runtime.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ArenaHazardSequence.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaHazardSequence", order = 14)]
    public sealed class ArenaHazardSequenceSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Sequence Settings")]
        [Tooltip("Seconds each hazard in the sequence stays active before the controller " +
                 "deactivates it and moves on to the next. " +
                 "Shorter values produce fast-rotating hazards; longer values give players " +
                 "more time to respond.")]
        [SerializeField, Min(0.1f)] private float _cycleDuration = 10f;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised by ArenaHazardSequenceController each time the active hazard " +
                 "advances to the next in the sequence. Wire to audio or HUD indicators.")]
        [SerializeField] private VoidGameEvent _onSequenceAdvanced;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Seconds each hazard stays active before the sequence advances.</summary>
        public float CycleDuration => _cycleDuration;

        /// <summary>
        /// Event channel raised by the controller on each sequence advance.
        /// May be null.
        /// </summary>
        public VoidGameEvent OnSequenceAdvanced => _onSequenceAdvanced;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_cycleDuration < 1f)
                Debug.LogWarning($"[ArenaHazardSequenceSO] '{name}': _cycleDuration ({_cycleDuration:F2}s) " +
                                 "is very short — hazards may cycle too fast for players to react.");
        }
#endif
    }
}

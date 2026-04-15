using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A serializable data struct representing one phase in an arena lifecycle.
    /// Each phase specifies an event to fire when the phase starts and a duration
    /// that controls how long the phase lasts before the controller advances to the next.
    /// </summary>
    [Serializable]
    public struct ArenaPhase
    {
        [Tooltip("VoidGameEvent raised when this phase starts. May be null.")]
        public VoidGameEvent phaseEvent;

        [Tooltip("Seconds this phase lasts before advancing to the next phase. Minimum 0.1 s.")]
        [Min(0.1f)] public float duration;
    }

    /// <summary>
    /// Configuration ScriptableObject for <see cref="ArenaPhaseController"/>.
    /// Defines an ordered list of arena phases, each with a duration and an optional
    /// event fired when that phase begins.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • Phases execute in index order (0, 1, 2, …).
    ///   • When all phases complete, <see cref="OnAllPhasesComplete"/> is raised.
    ///   • This SO is configuration-only; all mutable state (current phase index,
    ///     elapsed timer) lives in the controller so the SO stays immutable at runtime.
    ///   • <see cref="GetPhaseDuration"/> and <see cref="GetPhaseEvent"/> guard
    ///     out-of-range indices with null / 0f returns.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ArenaPhaseController.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaPhaseController", order = 15)]
    public sealed class ArenaPhaseControllerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Phases")]
        [Tooltip("Ordered list of arena phases. Executed from index 0 to the end.")]
        [SerializeField] private ArenaPhase[] _phases;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised once all phases have completed.")]
        [SerializeField] private VoidGameEvent _onAllPhasesComplete;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of phases in the sequence.</summary>
        public int PhaseCount => _phases?.Length ?? 0;

        /// <summary>Event channel raised when all phases finish. May be null.</summary>
        public VoidGameEvent OnAllPhasesComplete => _onAllPhasesComplete;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the duration (in seconds) for the phase at <paramref name="index"/>.
        /// Returns 0f for a null phases array or an out-of-range index.
        /// </summary>
        public float GetPhaseDuration(int index)
        {
            if (_phases == null || index < 0 || index >= _phases.Length)
                return 0f;

            return _phases[index].duration;
        }

        /// <summary>
        /// Returns the <see cref="VoidGameEvent"/> for the phase at <paramref name="index"/>.
        /// Returns null for a null phases array, an out-of-range index, or a phase
        /// with no event assigned.
        /// </summary>
        public VoidGameEvent GetPhaseEvent(int index)
        {
            if (_phases == null || index < 0 || index >= _phases.Length)
                return null;

            return _phases[index].phaseEvent;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_phases == null || _phases.Length == 0)
            {
                Debug.LogWarning($"[ArenaPhaseControllerSO] '{name}': " +
                                 "_phases is null or empty — no phases will execute.");
                return;
            }

            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i].duration < 0.5f)
                    Debug.LogWarning($"[ArenaPhaseControllerSO] '{name}': " +
                                     $"Phase [{i}] duration ({_phases[i].duration:F2}s) is very short " +
                                     "— phases may advance too quickly for players to notice.");
            }
        }
#endif
    }
}

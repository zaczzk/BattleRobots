using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that enables or disables all child <see cref="PowerUpController"/>
    /// components in response to match-lifecycle events, so power-up pickups only
    /// respawn during active matches.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • <see cref="HandleMatchStarted"/> — called when <c>_onMatchStarted</c> fires;
    ///     sets <c>enabled = true</c> on every child <see cref="PowerUpController"/>.
    ///   • <see cref="HandleMatchEnded"/> — called when <c>_onMatchEnded</c> fires;
    ///     sets <c>enabled = false</c> on every child <see cref="PowerUpController"/>,
    ///     which triggers <c>OnDisable</c> and cancels any pending respawn coroutines.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   1. Add this MB to the parent GameObject that contains all arena power-up
    ///      pickups as children (direct or nested).
    ///   2. Assign <c>_onMatchStarted</c> → the MatchStarted VoidGameEvent SO.
    ///   3. Assign <c>_onMatchEnded</c>   → the MatchEnded VoidGameEvent SO.
    ///   4. Child <see cref="PowerUpController"/> components may begin disabled
    ///      (to prevent out-of-match collection) and will be enabled at match start.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Physics</c> namespace — may reference Core; must not
    ///     reference <c>BattleRobots.UI</c>.
    ///   • Child <see cref="PowerUpController"/> references are cached in <c>Awake</c>
    ///     (includes inactive children) — zero per-event allocation.
    ///   • Delegate references are cached in <c>Awake</c> to allow correct
    ///     Register / Unregister pairing in <c>OnEnable</c> / <c>OnDisable</c>.
    ///   • <see cref="HandleMatchStarted"/> and <see cref="HandleMatchEnded"/> are
    ///     public so EditMode tests can drive them directly without raising events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PowerUpMatchLifecycleManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised when a match begins. " +
                 "On receipt, all child PowerUpController components are enabled " +
                 "so pickups can be collected and their respawn timers can run.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("VoidGameEvent raised when a match ends. " +
                 "On receipt, all child PowerUpController components are disabled, " +
                 "cancelling any in-flight respawn coroutines via OnDisable.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Cached in Awake to avoid repeated GetComponentsInChildren calls on the event path.
        private PowerUpController[] _controllers;

        // Cached delegate references — required for correct Register / Unregister pairing.
        private Action _matchStartedDelegate;
        private Action _matchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Include inactive children so controllers that start disabled are still managed.
            _controllers = GetComponentsInChildren<PowerUpController>(includeInactive: true);

            _matchStartedDelegate = HandleMatchStarted;
            _matchEndedDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_matchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_matchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_matchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_matchEndedDelegate);
        }

        // ── Public testable API ───────────────────────────────────────────────

        /// <summary>
        /// Enables all cached child <see cref="PowerUpController"/> components,
        /// allowing pickups to be collected and respawn timers to run.
        ///
        /// Called automatically when <c>_onMatchStarted</c> fires.
        /// Also safe to call directly from editor scripts or EditMode tests.
        /// </summary>
        public void HandleMatchStarted() => SetControllersEnabled(true);

        /// <summary>
        /// Disables all cached child <see cref="PowerUpController"/> components,
        /// preventing new collections and cancelling pending respawn coroutines
        /// via <see cref="MonoBehaviour.OnDisable"/> on each child.
        ///
        /// Called automatically when <c>_onMatchEnded</c> fires.
        /// Also safe to call directly from editor scripts or EditMode tests.
        /// </summary>
        public void HandleMatchEnded() => SetControllersEnabled(false);

        // ── Private helpers ───────────────────────────────────────────────────

        private void SetControllersEnabled(bool enabled)
        {
            foreach (PowerUpController ctrl in _controllers)
            {
                if (ctrl != null)
                    ctrl.enabled = enabled;
            }
        }
    }
}

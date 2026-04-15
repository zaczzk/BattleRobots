using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that bridges per-zone capture / loss events into a
    /// <see cref="ZoneDominanceSO"/> aggregate count.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. On <c>_onMatchStarted</c>: calls <see cref="ZoneDominanceSO.Reset"/>
    ///      so the player starts each match holding zero zones.
    ///   2. On <c>_onMatchEnded</c>: calls <see cref="ZoneDominanceSO.Reset"/>
    ///      to clear the count after the match ends.
    ///   3. On <c>_onPlayerZoneCaptured</c>: calls <see cref="ZoneDominanceSO.AddPlayerZone"/>.
    ///   4. On <c>_onPlayerZoneLost</c>: calls <see cref="ZoneDominanceSO.RemovePlayerZone"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI dependencies.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_dominanceSO</c>           → a ZoneDominanceSO asset.
    ///   2. Assign <c>_onMatchStarted</c>        → shared match-start VoidGameEvent.
    ///   3. Assign <c>_onMatchEnded</c>          → shared match-end VoidGameEvent.
    ///   4. Assign <c>_onPlayerZoneCaptured</c>  → the VoidGameEvent raised when
    ///      any player-controlled zone is captured (e.g., ControlZoneSO._onCaptured).
    ///   5. Assign <c>_onPlayerZoneLost</c>      → the VoidGameEvent raised when
    ///      any player-controlled zone is lost (e.g., ControlZoneSO._onLost).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneDominanceController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The ZoneDominanceSO that accumulates the player zone count.")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneLost;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _startDelegate;
        private Action _endDelegate;
        private Action _capturedDelegate;
        private Action _lostDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _startDelegate   = HandleMatchStarted;
            _endDelegate     = HandleMatchEnded;
            _capturedDelegate = HandleZoneCaptured;
            _lostDelegate    = HandleZoneLost;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_capturedDelegate);
            _onPlayerZoneLost?.RegisterCallback(_lostDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_capturedDelegate);
            _onPlayerZoneLost?.UnregisterCallback(_lostDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Resets the dominance count for a fresh match.
        /// Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            _dominanceSO?.Reset();
        }

        /// <summary>
        /// Resets the dominance count after the match ends.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _dominanceSO?.Reset();
        }

        /// <summary>
        /// Adds one zone to the player's count.
        /// Wired to <c>_onPlayerZoneCaptured</c>.
        /// </summary>
        public void HandleZoneCaptured()
        {
            _dominanceSO?.AddPlayerZone();
        }

        /// <summary>
        /// Removes one zone from the player's count.
        /// Wired to <c>_onPlayerZoneLost</c>.
        /// </summary>
        public void HandleZoneLost()
        {
            _dominanceSO?.RemovePlayerZone();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;
    }
}

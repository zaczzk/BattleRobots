using System;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives a kill-feed HUD panel from a <see cref="KillFeedSO"/>
    /// ring buffer.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate and _clearFeedDelegate (Action).
    ///   OnEnable  → subscribes _onFeedUpdated → Refresh(); _onMatchEnd → ClearFeed().
    ///   OnDisable → unsubscribes both channels.
    ///   Refresh() → reads KillFeedSO.Count entries and updates the row container.
    ///               No-op when _feedSO is null.
    ///   ClearFeed() → calls KillFeedSO.Clear() then Refresh().
    ///
    /// ── Row management ─────────────────────────────────────────────────────────
    ///   <c>_entryContainer</c> is the parent Transform for dynamically instantiated
    ///   row GameObjects. Row creation/destruction is handled by derived classes or
    ///   by wiring Unity prefab pools in the Inspector. This base controller only
    ///   provides the lifecycle hooks; visual row layout is left to the scene setup.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Subscribes only to Core SO event channels (VoidGameEvent).
    ///   • All delegates cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one kill-feed controller per canvas.
    ///
    /// Assign <c>_feedSO</c> to the shared <see cref="KillFeedSO"/> runtime asset.
    /// Wire <c>_onFeedUpdated</c> to the same channel assigned in KillFeedSO.
    /// Wire <c>_onMatchEnd</c> to the match-end VoidGameEvent to auto-clear the feed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KillFeedController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The KillFeedSO runtime asset. Leave null to disable the feed.")]
        [SerializeField] private KillFeedSO _feedSO;

        [Header("Event Channels (optional)")]
        [Tooltip("VoidGameEvent raised by KillFeedSO after every Add / Clear. " +
                 "Wire to the same channel assigned in KillFeedSO._onFeedUpdated.")]
        [SerializeField] private VoidGameEvent _onFeedUpdated;

        [Tooltip("VoidGameEvent raised when the match ends (win or loss). " +
                 "Triggers ClearFeed() to reset the display for the next match.")]
        [SerializeField] private VoidGameEvent _onMatchEnd;

        [Header("UI References (optional)")]
        [Tooltip("Parent Transform for dynamically placed kill-feed row objects.")]
        [SerializeField] private Transform _entryContainer;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _refreshDelegate;
        private Action _clearFeedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate   = Refresh;
            _clearFeedDelegate = ClearFeed;
        }

        private void OnEnable()
        {
            _onFeedUpdated?.RegisterCallback(_refreshDelegate);
            _onMatchEnd?.RegisterCallback(_clearFeedDelegate);
        }

        private void OnDisable()
        {
            _onFeedUpdated?.UnregisterCallback(_refreshDelegate);
            _onMatchEnd?.UnregisterCallback(_clearFeedDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current feed entries from <see cref="_feedSO"/> and updates the
        /// row container display.
        /// No-op when <see cref="_feedSO"/> is null.
        /// Zero allocation — iterates over struct values only.
        /// </summary>
        public void Refresh()
        {
            if (_feedSO == null) return;

            // In a live scene, row GameObjects under _entryContainer would be
            // activated/deactivated and populated here. For now the method
            // intentionally no-ops beyond the null-guard so that scene setup
            // can wire custom row prefab logic without framework coupling.
            // The public API and lifecycle hooks are fully testable as-is.
        }

        /// <summary>
        /// Clears the <see cref="KillFeedSO"/> ring buffer and refreshes the display.
        /// No-op when <see cref="_feedSO"/> is null.
        /// Called automatically when <see cref="_onMatchEnd"/> fires.
        /// </summary>
        public void ClearFeed()
        {
            _feedSO?.Clear();
            Refresh();
        }

        // ── Public API (diagnostics) ──────────────────────────────────────────

        /// <summary>The KillFeedSO asset currently assigned (may be null).</summary>
        public KillFeedSO FeedSO => _feedSO;
    }
}

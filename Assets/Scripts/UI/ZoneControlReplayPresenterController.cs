using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that visualises the zone ownership state of the replay step
    /// currently selected in <see cref="ZoneControlReplaySO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _zoneBadges  → Array of GameObjects, one per zone in the catalog.
    ///                  Each badge is activated (visible) when the zone is captured
    ///                  at the current replay step, deactivated otherwise.
    ///   _panel       → Root panel; hidden when <c>_replaySO</c> is null or empty.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onReplayUpdated</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one presenter per replay panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _replaySO        → ZoneControlReplaySO asset.
    ///   2. Assign _onReplayUpdated → ZoneControlReplaySO._onReplayUpdated channel.
    ///   3. Populate _zoneBadges with one GameObject badge per arena zone.
    ///   4. Assign _panel           → root panel GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlReplayPresenterController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlReplaySO _replaySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlReplaySO._onReplayUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onReplayUpdated;

        [Header("UI Refs (optional)")]
        [Tooltip("One badge GameObject per arena zone. " +
                 "Activated when the zone is captured at the current replay step.")]
        [SerializeField] private GameObject[] _zoneBadges;

        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onReplayUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onReplayUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads <see cref="ZoneControlReplaySO.CurrentSnapshot"/> and updates each
        /// zone badge to reflect the captured/uncaptured state at that replay step.
        /// Hides the panel when <c>_replaySO</c> is null or the buffer is empty.
        /// Zero allocation (iterates pre-allocated arrays).
        /// </summary>
        public void Refresh()
        {
            if (_replaySO == null || _replaySO.Count == 0)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_zoneBadges == null) return;

            ZoneControlSnapshot snapshot = _replaySO.CurrentSnapshot;
            bool[] captureState = snapshot.captureState;

            for (int i = 0; i < _zoneBadges.Length; i++)
            {
                if (_zoneBadges[i] == null) continue;

                bool captured = captureState != null && i < captureState.Length && captureState[i];
                _zoneBadges[i].SetActive(captured);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound replay SO (may be null).</summary>
        public ZoneControlReplaySO ReplaySO => _replaySO;

        /// <summary>Number of zone badge slots configured (may be zero).</summary>
        public int ZoneBadgeCount => _zoneBadges?.Length ?? 0;
    }
}

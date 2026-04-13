using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the post-match event timeline from a <see cref="MatchEventLogSO"/> as a
    /// scrollable list of timestamped rows.
    ///
    /// ── When does it refresh? ────────────────────────────────────────────────────
    ///   • Immediately on <c>OnEnable</c> (catches the state at panel open).
    ///   • On every <c>_onMatchEnded</c> event (standard post-match trigger).
    ///
    /// ── Row layout (via _rowPrefab) ───────────────────────────────────────────────
    ///   The row prefab must have at least two <see cref="Text"/> components accessible
    ///   via <c>GetComponentsInChildren&lt;Text&gt;</c>:
    ///     Texts[0] — formatted game time (e.g. "1m 23s" or "45s").
    ///     Texts[1] — event description string.
    ///
    /// ── Empty state ───────────────────────────────────────────────────────────────
    ///   When the log has no entries, <c>_emptyLabel</c> is shown (if assigned) and no
    ///   row prefabs are instantiated.
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────
    ///   Data (optional):
    ///     _eventLog        → the <see cref="MatchEventLogSO"/> SO asset.
    ///   Event Channels — In (optional):
    ///     _onMatchEnded    → VoidGameEvent fired at match end (drives Refresh).
    ///   UI Refs (optional):
    ///     _listContainer   → parent Transform for row prefab instances.
    ///     _rowPrefab       → prefab with ≥ 2 Text components (time + description).
    ///     _emptyLabel      → GameObject shown when the event log is empty.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No allocations in Update/FixedUpdate — no Update loop.
    ///   - All inspector fields optional; null-safe throughout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchTimelineController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("SO that stores the chronological list of match event entries. " +
                 "Leave null to show empty state on the panel.")]
        [SerializeField] private MatchEventLogSO _eventLog;

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by MatchManager when the match ends. Triggers Refresh(). " +
                 "Leave null — OnEnable still calls Refresh() once.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI Refs (all optional)")]
        [Tooltip("Parent Transform under which row prefabs are instantiated. Leave null to skip list.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Row prefab with ≥ 2 Text children: Texts[0]=time, Texts[1]=description.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("GameObject shown when the event log has no entries. Leave null to skip.")]
        [SerializeField] private GameObject _emptyLabel;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the timeline row list from the current <see cref="MatchEventLogSO"/> state.
        /// Called on OnEnable and whenever <c>_onMatchEnded</c> fires.
        /// </summary>
        public void Refresh()
        {
            bool hasEvents = _eventLog != null && _eventLog.Events.Count > 0;

            _emptyLabel?.SetActive(!hasEvents);

            // Always destroy stale rows first.
            if (_listContainer != null)
            {
                for (int i = _listContainer.childCount - 1; i >= 0; i--)
                    Destroy(_listContainer.GetChild(i).gameObject);
            }

            if (!hasEvents || _listContainer == null || _rowPrefab == null) return;

            var entries = _eventLog.Events;
            for (int i = 0; i < entries.Count; i++)
            {
                MatchEventEntry entry = entries[i];
                GameObject row  = Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>(includeInactive: true);

                if (texts.Length > 0) texts[0].text = FormatTime(entry.gameTime);
                if (texts.Length > 1) texts[1].text = entry.description;
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Formats a game-time value in seconds as "Xm Ys" (when ≥ 1 min) or "Ys" otherwise.
        /// Negative values are treated as 0.
        /// </summary>
        internal static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int mins = totalSeconds / 60;
            int secs = totalSeconds % 60;
            return mins > 0 ? $"{mins}m {secs}s" : $"{secs}s";
        }
    }
}

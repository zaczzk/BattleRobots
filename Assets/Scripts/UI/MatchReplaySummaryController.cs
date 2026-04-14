using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match timeline panel that displays a chronological list of combat events
    /// recorded in <see cref="MatchReplaySummarySO"/>.
    ///
    /// ── Row format ────────────────────────────────────────────────────────────────
    ///   Texts[0] → "P: +amount" when the player dealt damage; "E: -amount" when the
    ///              enemy dealt damage (rounded to nearest integer).
    ///   Texts[1] → DamageType name (e.g. "Physical", "Energy").
    ///
    ///   The empty-label is activated when the summary has no recorded events.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _onMatchEndedDelegate.
    ///   OnEnable  → subscribes _onMatchEnded → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → shows/hides _emptyLabel; rebuilds row list newest-first.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one timeline panel per canvas.
    ///   • All UI fields are optional.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _summary        → shared MatchReplaySummarySO ring buffer.
    ///   _onMatchEnded   → VoidGameEvent raised when a match ends.
    ///   _listContainer  → Transform parent for the timeline row pool.
    ///   _rowPrefab      → Prefab containing at least one Text child.
    ///   _emptyLabel     → GameObject shown when the summary is empty.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchReplaySummaryController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Ring-buffer SO containing the recorded match events.")]
        [SerializeField] private MatchReplaySummarySO _summary;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised when the match ends. Triggers a Refresh.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Parent transform for timeline row prefab instances.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab with at least two Text children (event summary, damage type).")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("GameObject shown when the summary is empty or null.")]
        [SerializeField] private GameObject _emptyLabel;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _onMatchEndedDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchEndedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_onMatchEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_onMatchEndedDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the timeline row list newest-first.
        /// Shows <see cref="_emptyLabel"/> when the summary has no events.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            bool empty = _summary == null || _summary.Count == 0;
            _emptyLabel?.SetActive(empty);

            if (_listContainer == null) return;

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            if (empty || _rowPrefab == null) return;

            for (int i = 0; i < _summary.Count; i++)
            {
                var entry = _summary.GetEntry(i);
                if (!entry.HasValue) continue;

                var  row   = Instantiate(_rowPrefab, _listContainer);
                var  texts = row.GetComponentsInChildren<Text>();

                if (texts.Length > 0)
                {
                    string prefix  = entry.Value.wasPlayer ? "P" : "E";
                    string sign    = entry.Value.wasPlayer ? "+" : "-";
                    int    rounded = Mathf.RoundToInt(entry.Value.amount);
                    texts[0].text = string.Format("{0}: {1}{2}", prefix, sign, rounded);
                }

                if (texts.Length > 1)
                    texts[1].text = entry.Value.damageType.ToString();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchReplaySummarySO"/>. May be null.</summary>
        public MatchReplaySummarySO Summary => _summary;
    }
}

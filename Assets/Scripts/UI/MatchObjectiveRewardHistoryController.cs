using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Live-updating panel that shows bonus-objective reward outcomes in newest-first order,
    /// sourced from <see cref="MatchObjectivePersistenceSO"/>. Unlike
    /// <see cref="MatchObjectiveHistoryController"/> (which refreshes on match end),
    /// this controller subscribes to <c>_onHistoryUpdated</c> from the SO and rebuilds
    /// whenever an objective resolves mid-match.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   <c>_onHistoryUpdated</c> → Refresh().
    ///   Refresh() destroys old rows, then rebuilds them newest-first:
    ///     Texts[0] = objective title
    ///     Texts[1] = "COMPLETED" (entry.completed) or "FAILED" (not completed)
    ///     Texts[2] = "+N credits" (when reward > 0) or "—"
    ///   When history is null or empty, <c>_emptyLabel</c> is shown instead.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - Delegate cached in Awake; zero heap allocation after initialisation
    ///     except per-row Instantiate (unavoidable).
    ///   - DisallowMultipleComponent — one reward-history panel per canvas.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_history</c>          — MatchObjectivePersistenceSO asset.
    ///   2. Assign <c>_onHistoryUpdated</c> — the same VoidGameEvent wired to the SO's
    ///                                        _onHistoryUpdated field (refreshes live).
    ///   3. Assign <c>_listContainer</c>    — scrollable content Transform.
    ///   4. Assign <c>_rowPrefab</c>        — prefab with ≥ 3 Text children (title / badge / reward).
    ///   5. Optionally assign <c>_emptyLabel</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchObjectiveRewardHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Ring-buffer SO holding per-match objective outcomes.")]
        [SerializeField] private MatchObjectivePersistenceSO _history;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchObjectivePersistenceSO after every " +
                 "Record() or Reset() call. Subscribe to the SO's own channel so the " +
                 "panel refreshes live as objectives complete during a match.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("UI References (optional)")]
        [Tooltip("Container Transform that holds the instantiated row GameObjects.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab instantiated once per history entry. Must have at least three " +
                 "Text children: index 0 = title, 1 = COMPLETED/FAILED badge, 2 = reward.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("Label shown when there are no history entries to display.")]
        [SerializeField] private Text _emptyLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchObjectivePersistenceSO"/>. May be null.</summary>
        public MatchObjectivePersistenceSO History => _history;

        /// <summary>
        /// Destroys all existing row children and rebuilds the list newest-first.
        /// Shows <c>_emptyLabel</c> when history is null or empty.
        /// Fully null-safe; no-op when <c>_listContainer</c> or <c>_rowPrefab</c> is null.
        /// Row texts:
        ///   [0] Objective title
        ///   [1] "COMPLETED" or "FAILED" badge
        ///   [2] "+N credits" when reward > 0; "—" (em-dash) otherwise
        /// </summary>
        public void Refresh()
        {
            if (_listContainer == null || _rowPrefab == null) return;

            // Destroy stale rows
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            bool isEmpty = _history == null || _history.Count == 0;

            if (_emptyLabel != null)
                _emptyLabel.gameObject.SetActive(isEmpty);

            if (isEmpty) return;

            // Build rows newest-first
            var entries = _history.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                var row   = Instantiate(_rowPrefab, _listContainer);

                var texts = row.GetComponentsInChildren<Text>(includeInactive: true);
                if (texts.Length > 0) texts[0].text = entry.title;
                if (texts.Length > 1) texts[1].text = entry.completed ? "COMPLETED" : "FAILED";
                if (texts.Length > 2)
                    texts[2].text = entry.reward > 0
                        ? string.Format("+{0} credits", entry.reward)
                        : "\u2014"; // em-dash
            }
        }
    }
}

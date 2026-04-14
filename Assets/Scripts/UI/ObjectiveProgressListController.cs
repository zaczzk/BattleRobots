using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that displays a scrollable list of all active
    /// <see cref="InMatchObjectiveSO"/> instances, showing title, progress, and
    /// a completion badge for each.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Any InMatchObjectiveSO raises _onObjectiveChanged (shared event)
    ///   ──► ObjectiveProgressListController.Refresh() rebuilds the row list.
    ///
    /// ── Row prefab layout ─────────────────────────────────────────────────────
    ///   The row prefab is expected to contain at least two Text children
    ///   (GetComponentsInChildren order):
    ///     [0] — objective title
    ///     [1] — progress text: "N/M" when incomplete, "DONE" when complete
    ///   An optional third Text child ([2]) is used as a completion badge;
    ///   its parent GameObject is shown only when the objective is complete.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation
    ///     (row instantiation during Refresh is intentional — infrequent rebuild).
    ///   • DisallowMultipleComponent — one objective list per HUD canvas.
    ///
    /// Scene wiring:
    ///   _objectives          → InMatchObjectiveSO assets to display.
    ///   _onObjectiveChanged  → VoidGameEvent raised by any of the objective SOs.
    ///   _listContainer       → Parent Transform for instantiated rows.
    ///   _rowPrefab           → Prefab for each objective row (see layout above).
    ///   _panel               → Root panel; hidden when _objectives is null/empty.
    ///   _allCompleteLabel    → Optional Text activated when all objectives are done.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectiveProgressListController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Array of InMatchObjectiveSO assets to display in the list.")]
        [SerializeField] private InMatchObjectiveSO[] _objectives;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by any InMatchObjectiveSO on progress changes.")]
        [SerializeField] private VoidGameEvent _onObjectiveChanged;

        [Header("UI References (optional)")]
        [Tooltip("Parent Transform into which objective rows are instantiated.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab for each objective row. Must contain at least two Text children.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("Root panel hidden when objectives array is null or empty.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Text or label activated when all objectives are complete.")]
        [SerializeField] private Text _allCompleteLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onObjectiveChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onObjectiveChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the objective row list from the current state of <see cref="_objectives"/>.
        /// Hides the panel when the array is null or empty.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            bool hasObjectives = _objectives != null && _objectives.Length > 0;

            if (!hasObjectives || _listContainer == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Object.Destroy(_listContainer.GetChild(i).gameObject);

            if (_rowPrefab == null) return;

            bool allComplete = true;
            foreach (var obj in _objectives)
            {
                if (obj == null) continue;
                if (!obj.IsComplete) allComplete = false;

                var row   = Object.Instantiate(_rowPrefab, _listContainer);
                var texts = row.GetComponentsInChildren<Text>(includeInactive: true);

                if (texts.Length > 0)
                    texts[0].text = obj.ObjectiveTitle;

                if (texts.Length > 1)
                    texts[1].text = obj.IsComplete
                        ? "DONE"
                        : string.Format("{0}/{1}", obj.CurrentCount, obj.TargetCount);

                // Optional completion badge (third Text child's parent GO).
                if (texts.Length > 2)
                    texts[2].gameObject.SetActive(obj.IsComplete);
            }

            // Show "All Complete!" label when every objective is done.
            if (_allCompleteLabel != null)
                _allCompleteLabel.gameObject.SetActive(allComplete);
        }

        /// <summary>The configured objectives array. May be null or empty.</summary>
        public InMatchObjectiveSO[] Objectives => _objectives;
    }
}

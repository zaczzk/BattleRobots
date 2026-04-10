using System;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Reads <see cref="SaveData.matchHistory"/> from <see cref="SaveSystem"/> and
    /// instantiates one <see cref="MatchHistoryRowController"/> prefab per record,
    /// displaying the most recent matches first.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Create a prefab with <see cref="MatchHistoryRowController"/> attached.
    ///      Assign optional Text fields inside the prefab Inspector:
    ///      _outcomeText, _durationText, _rewardText, _dateText.
    ///   2. Add <see cref="MatchHistoryController"/> to a panel GameObject in the
    ///      Main Menu or Post-Match results screen.
    ///   3. Assign _rowPrefab, _listContainer (ScrollRect Content Transform;
    ///      VerticalLayoutGroup recommended), and _maxDisplayCount.
    ///   4. Optionally assign _onMatchEnded so history auto-refreshes after every
    ///      match without requiring a scene reload.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — no Physics refs.
    ///   • Delegate cached in Awake; zero alloc after Awake outside PopulateHistory.
    ///   • PopulateHistory() is the only path that allocates (row instantiation).
    ///   • On Enable: subscribes to _onMatchEnded and populates immediately so the
    ///     list is up-to-date whenever the panel is shown.
    ///   • On Disable: unsubscribes to avoid stale callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchHistoryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Row Prefab")]
        [Tooltip("Prefab with MatchHistoryRowController attached. " +
                 "One instance is spawned per match record.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("Parent Transform for instantiated rows (ScrollRect Content). " +
                 "A VerticalLayoutGroup on this Transform is recommended.")]
        [SerializeField] private Transform _listContainer;

        [Header("Settings")]
        [Tooltip("Maximum number of recent matches to show. Rows are ordered most-recent-first.")]
        [SerializeField, Min(1)] private int _maxDisplayCount = 10;

        [Header("Event Channels — In")]
        [Tooltip("Optional: subscribe to refresh the history list after each match ends. " +
                 "Assign the same VoidGameEvent SO used by MatchManager._onMatchEnded.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegate (avoids lambda allocation per subscribe/unsubscribe) ──

        private Action _populateDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _populateDelegate = PopulateHistory;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_populateDelegate);
            PopulateHistory();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_populateDelegate);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys all existing rows, loads the current <see cref="SaveData.matchHistory"/>
        /// from disk, and instantiates one <see cref="MatchHistoryRowController"/> row per
        /// record (up to <see cref="_maxDisplayCount"/>, most-recent-first).
        ///
        /// Safe to call from a UI Button.onClick UnityEvent.
        /// </summary>
        public void PopulateHistory()
        {
            if (_listContainer == null || _rowPrefab == null)
            {
                Debug.LogWarning("[MatchHistoryController] _listContainer or _rowPrefab not assigned — " +
                                 "check Inspector wiring.");
                return;
            }

            // Destroy existing rows top-down so Destroy order matches child order.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            SaveData saveData = SaveSystem.Load();
            var history = saveData.matchHistory;
            if (history == null || history.Count == 0) return;

            // Iterate most-recent-first (end → start), respecting the display cap.
            int firstIndex = Mathf.Max(0, history.Count - _maxDisplayCount);
            for (int i = history.Count - 1; i >= firstIndex; i--)
            {
                GameObject row = Instantiate(_rowPrefab, _listContainer);
                var rowCtrl = row.GetComponent<MatchHistoryRowController>();
                rowCtrl?.Setup(history[i]);
            }
        }

        // ── Editor validation ──────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_rowPrefab == null)
                Debug.LogWarning("[MatchHistoryController] _rowPrefab not assigned.");
            if (_listContainer == null)
                Debug.LogWarning("[MatchHistoryController] _listContainer not assigned.");
        }
#endif
    }
}

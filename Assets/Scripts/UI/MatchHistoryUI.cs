using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's match history from the persistent save file.
    ///
    /// Behaviour:
    ///   - On <c>OnEnable</c>, loads <see cref="SaveData"/> from <see cref="SaveSystem"/>
    ///     and builds one <see cref="MatchHistoryEntryUI"/> row per record (newest first).
    ///   - Existing rows are destroyed and rebuilt each time the panel opens so the list
    ///     is always fresh.  This is safe because the panel is opened infrequently.
    ///   - Displays aggregate stats (total matches, wins, total currency) in a summary bar.
    ///
    /// Architecture rules:
    ///   - <c>BattleRobots.UI</c> namespace; no <c>BattleRobots.Physics</c> references.
    ///   - No heap allocations in Update (Update not overridden; all work is event-driven).
    ///   - Reads persistence via <see cref="SaveSystem"/>; never writes.
    ///
    /// Inspector wiring checklist:
    ///   □ _entryPrefab       → prefab with MatchHistoryEntryUI on root
    ///   □ _scrollContent     → ScrollView Content RectTransform
    ///   □ _totalMatchesLabel → Text
    ///   □ _winsLabel         → Text
    ///   □ _winRateLabel      → Text
    ///   □ _totalEarningsLabel→ Text
    ///   □ _emptyStateLabel   → Text (shown when history is empty)
    ///   □ _closeButton       → Button (hides this GameObject on click)
    /// </summary>
    public sealed class MatchHistoryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("List")]
        [Tooltip("Prefab instantiated per match record. Root must have MatchHistoryEntryUI.")]
        [SerializeField] private GameObject _entryPrefab;

        [Tooltip("ScrollView Content transform — entry rows are parented here.")]
        [SerializeField] private RectTransform _scrollContent;

        [Header("Summary Bar")]
        [SerializeField] private Text _totalMatchesLabel;
        [SerializeField] private Text _winsLabel;
        [SerializeField] private Text _winRateLabel;
        [SerializeField] private Text _totalEarningsLabel;

        [Header("Empty State")]
        [Tooltip("Shown in place of the list when there are no match records.")]
        [SerializeField] private Text _emptyStateLabel;

        [Header("Navigation")]
        [Tooltip("Closes (hides) this panel when clicked.")]
        [SerializeField] private Button _closeButton;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        /// <summary>
        /// Rebuilds the list each time the panel becomes visible so it always
        /// reflects the latest save data (e.g. after returning from a match).
        /// </summary>
        private void OnEnable()
        {
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reloads save data and rebuilds the UI list.
        /// Safe to call externally (e.g. from a "Refresh" button).
        /// </summary>
        public void Refresh()
        {
            ClearRows();

            SaveData saveData = SaveSystem.Load();
            List<MatchRecord> history = saveData.matchHistory;

            bool hasRecords = history != null && history.Count > 0;

            if (_emptyStateLabel != null)
                _emptyStateLabel.gameObject.SetActive(!hasRecords);

            if (!hasRecords)
            {
                RefreshSummary(0, 0, 0);
                return;
            }

            // Display newest match first.
            int totalWins     = 0;
            int totalEarnings = 0;

            for (int i = history.Count - 1; i >= 0; i--)
            {
                MatchRecord record = history[i];

                if (record.playerWon) totalWins++;
                totalEarnings += record.currencyEarned;

                if (_entryPrefab != null && _scrollContent != null)
                {
                    GameObject go  = Instantiate(_entryPrefab, _scrollContent);
                    var        row = go.GetComponent<MatchHistoryEntryUI>();
                    if (row != null)
                        row.Populate(record);
                }
            }

            RefreshSummary(history.Count, totalWins, totalEarnings);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void ClearRows()
        {
            if (_scrollContent == null) return;

            for (int i = _scrollContent.childCount - 1; i >= 0; i--)
                Destroy(_scrollContent.GetChild(i).gameObject);
        }

        private void RefreshSummary(int total, int wins, int earnings)
        {
            if (_totalMatchesLabel != null)
                _totalMatchesLabel.text = $"Matches: {total}";

            if (_winsLabel != null)
                _winsLabel.text = $"Wins: {wins}";

            if (_winRateLabel != null)
            {
                float rate = total > 0 ? (wins / (float)total) * 100f : 0f;
                _winRateLabel.text = $"Win Rate: {rate:F0}%";
            }

            if (_totalEarningsLabel != null)
                _totalEarningsLabel.text = $"Earned: {earnings} cr";
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MB that records a loadout snapshot at the end of each match and displays
    /// the ring-buffer history as a scrollable row list.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────
    ///   1. Subscribe <c>_onMatchEnded</c> → <see cref="OnMatchEnded"/>.
    ///      Reads <see cref="PlayerLoadout.EquippedPartIds"/> and
    ///      <see cref="MatchResultSO.PlayerWon"/>, then calls
    ///      <see cref="LoadoutHistorySO.AddEntry"/>.
    ///   2. Rebuild the row list in <c>_listContainer</c> using <c>_rowPrefab</c>.
    ///      Each row exposes at least two Text children:
    ///        [0] — part count ("N parts")
    ///        [1] — outcome ("WIN" / "LOSS")
    ///   3. <c>_emptyLabel</c> is shown/hidden based on buffer count.
    ///   4. <see cref="RestoreLatest"/> copies the most-recent loadout back into
    ///      <see cref="PlayerLoadout"/> and persists it via the save system.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one history panel per canvas.
    ///   - All inspector fields optional — assign only those present in the scene.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _history          → shared LoadoutHistorySO ring-buffer.
    ///   _playerLoadout    → shared PlayerLoadout runtime SO.
    ///   _matchResult      → shared MatchResultSO blackboard.
    ///   _onMatchEnded     → same VoidGameEvent as MatchManager raises.
    ///   _listContainer    → Transform that holds the generated row GameObjects.
    ///   _rowPrefab        → prefab with ≥ 2 Text children (part count, outcome).
    ///   _emptyLabel       → GameObject shown when the history is empty.
    ///   _restoreButton    → Button that calls RestoreLatest() when clicked.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadoutHistoryController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Ring-buffer SO that stores loadout snapshots.")]
        [SerializeField] private LoadoutHistorySO _history;

        [Tooltip("PlayerLoadout SO — source of part IDs at match end, and write target for RestoreLatest.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("MatchResultSO blackboard — read for PlayerWon at match end.")]
        [SerializeField] private MatchResultSO _matchResult;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by MatchManager at the end of each match.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Parent Transform for generated history rows.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab for each history row. Needs ≥ 2 Text children: [0] part count, [1] outcome.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("GameObject shown when the history buffer is empty.")]
        [SerializeField] private GameObject _emptyLabel;

        [Tooltip("Button that restores the most-recent loadout.")]
        [SerializeField] private Button _restoreButton;

        // ── Cached delegates ─────────────────────────────────────────────────

        private Action _onMatchEndedDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchEndedDelegate = OnMatchEnded;

            if (_restoreButton != null)
                _restoreButton.onClick.AddListener(RestoreLatest);
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

        // ── Logic ────────────────────────────────────────────────────────────

        private void OnMatchEnded()
        {
            if (_history == null || _playerLoadout == null) return;

            bool playerWon = _matchResult?.PlayerWon ?? false;
            double ts      = GetTimestamp();

            _history.AddEntry(_playerLoadout.EquippedPartIds, playerWon, ts);
            Refresh();
        }

        /// <summary>
        /// Rebuilds the history row list.  Safe to call with any combination of
        /// null references.
        /// </summary>
        public void Refresh()
        {
            bool hasEntries = _history != null && _history.Count > 0;
            _emptyLabel?.SetActive(!hasEntries);

            // Destroy stale rows.
            if (_listContainer != null)
            {
                for (int i = _listContainer.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(_listContainer.GetChild(i).gameObject);
            }

            if (_listContainer == null || _rowPrefab == null || !hasEntries) return;

            int count = _history.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = _history.GetEntry(i);
                if (entry == null) continue;

                GameObject row  = Instantiate(_rowPrefab, _listContainer);
                var texts       = row.GetComponentsInChildren<Text>();

                if (texts.Length > 0)
                    texts[0].text = string.Format("{0} parts", entry.Value.partIds?.Length ?? 0);

                if (texts.Length > 1)
                    texts[1].text = entry.Value.playerWon ? "WIN" : "LOSS";
            }
        }

        /// <summary>
        /// Copies the most-recent loadout from the history ring-buffer back into
        /// <see cref="PlayerLoadout"/> and persists it via the XOR save system.
        /// No-op when history is null/empty or playerLoadout is null.
        /// </summary>
        public void RestoreLatest()
        {
            if (_history == null || _playerLoadout == null) return;

            var latest = _history.GetLatest();
            if (latest == null) return;

            _playerLoadout.SetLoadout(latest.Value.partIds);

            // Persist: load → mutate → save pattern.
            var saveData = SaveSystem.Load();
            saveData.loadoutPartIds = saveData.loadoutPartIds ?? new System.Collections.Generic.List<string>();
            saveData.loadoutPartIds.Clear();
            if (latest.Value.partIds != null)
                saveData.loadoutPartIds.AddRange(latest.Value.partIds);
            SaveSystem.Save(saveData);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static double GetTimestamp()
        {
            return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                   .TotalSeconds;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="LoadoutHistorySO"/>. May be null.</summary>
        public LoadoutHistorySO History => _history;

        /// <summary>The assigned <see cref="PlayerLoadout"/>. May be null.</summary>
        public PlayerLoadout PlayerLoadout => _playerLoadout;
    }
}

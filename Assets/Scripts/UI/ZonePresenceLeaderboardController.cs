using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a per-zone presence-time leaderboard sorted
    /// from longest-held to shortest, with a rank badge highlighting the top zone.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel         → root container; hidden when _timerSO is null / on OnDisable.
    ///   _listContainer → parent Transform where per-zone row GameObjects are spawned.
    ///   _rowPrefab     → prefab with ≥ 2 Text children and an optional Image child
    ///                    used as the rank badge for the top zone:
    ///                      Text[0] = Zone label ("Zone 1", …).
    ///                      Text[1] = Presence time formatted as "{t:F1}s".
    ///                      Image   = rank badge (active only on the highest-time zone).
    ///   _emptyLabel    → shown when SO is null or MaxZones is 0.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Rows sorted descending by presence time on each Refresh.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _timerSO           → the ZonePresenceTimerSO asset.
    ///   2. Assign _onPresenceUpdated → ZonePresenceTimerSO._onPresenceUpdated channel.
    ///   3. Assign _listContainer, _rowPrefab, _emptyLabel, _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZonePresenceLeaderboardController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZonePresenceTimerSO _timerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPresenceUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Transform  _listContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Text       _emptyLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Runtime scratch — reused each Refresh to avoid allocations ────────

        private int[] _sortedIndices;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPresenceUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPresenceUpdated?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys existing rows and rebuilds the sorted presence-time leaderboard.
        /// Shows <c>_emptyLabel</c> when the SO is null or has no zones.
        /// No-op when <c>_listContainer</c> or <c>_rowPrefab</c> is unassigned.
        /// </summary>
        public void Refresh()
        {
            if (_listContainer == null || _rowPrefab == null)
                return;

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Object.Destroy(_listContainer.GetChild(i).gameObject);

            bool isEmpty = _timerSO == null || _timerSO.MaxZones == 0;
            _emptyLabel?.gameObject.SetActive(isEmpty);

            if (isEmpty)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int zoneCount = _timerSO.MaxZones;

            // Build sorted index array (descending by presence time).
            if (_sortedIndices == null || _sortedIndices.Length != zoneCount)
                _sortedIndices = new int[zoneCount];

            for (int i = 0; i < zoneCount; i++)
                _sortedIndices[i] = i;

            // Insertion sort — small N, avoids delegate/closure allocation.
            for (int i = 1; i < zoneCount; i++)
            {
                int key = _sortedIndices[i];
                float keyTime = _timerSO.GetPresenceTime(key);
                int j = i - 1;
                while (j >= 0 && _timerSO.GetPresenceTime(_sortedIndices[j]) < keyTime)
                {
                    _sortedIndices[j + 1] = _sortedIndices[j];
                    j--;
                }
                _sortedIndices[j + 1] = key;
            }

            // Spawn rows.
            for (int rank = 0; rank < zoneCount; rank++)
            {
                int   zoneIdx  = _sortedIndices[rank];
                float time     = _timerSO.GetPresenceTime(zoneIdx);

                GameObject row   = Object.Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>(true);

                if (texts.Length > 0) texts[0].text = $"Zone {zoneIdx + 1}";
                if (texts.Length > 1) texts[1].text = $"{time:F1}s";

                // Show rank badge only for the top-ranked zone (rank == 0).
                Image badge = row.GetComponentInChildren<Image>(true);
                if (badge != null)
                    badge.gameObject.SetActive(rank == 0);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZonePresenceTimerSO"/>. May be null.</summary>
        public ZonePresenceTimerSO TimerSO => _timerSO;
    }
}

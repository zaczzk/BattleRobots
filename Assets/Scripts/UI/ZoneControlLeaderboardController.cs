using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a live zone-control leaderboard panel,
    /// showing each zone's current status and a score summary sourced from
    /// <see cref="ZoneScoreTrackerSO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel          → root container; hidden when _catalog is null.
    ///   _listContainer  → parent Transform where per-zone rows are spawned.
    ///   _rowPrefab      → prefab with ≥ 3 Text children per row:
    ///                       [0] = ZoneId label.
    ///                       [1] = "CAPTURED" or "OPEN" status badge.
    ///                       [2] = Capture progress as "N%".
    ///   _emptyLabel     → shown when catalog is null or has no zones.
    ///   _scoreSummaryLabel → overall "P: N | E: N" score line (optional).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes reactively to _onScoreUpdated for live score refresh.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _catalog         → ControlZoneCatalogSO asset.
    ///   2. Assign _tracker         → ZoneScoreTrackerSO asset (optional).
    ///   3. Assign _onScoreUpdated  → ZoneScoreTrackerSO._onScoreUpdated channel.
    ///   4. Assign UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlLeaderboardController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ControlZoneCatalogSO  _catalog;
        [SerializeField] private ZoneScoreTrackerSO    _tracker;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onScoreUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Transform  _listContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Text       _emptyLabel;
        [SerializeField] private Text       _scoreSummaryLabel;
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
            _onScoreUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onScoreUpdated?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys existing rows and rebuilds the leaderboard from the current
        /// zone catalog and score tracker state.
        /// Shows _emptyLabel when catalog is null or empty.
        /// No-op when _listContainer or _rowPrefab is unassigned.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_listContainer == null || _rowPrefab == null)
                return;

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Object.Destroy(_listContainer.GetChild(i).gameObject);

            bool isEmpty = _catalog == null || _catalog.EntryCount == 0;
            _emptyLabel?.gameObject.SetActive(isEmpty);

            if (isEmpty)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            // Score summary row.
            if (_scoreSummaryLabel != null && _tracker != null)
            {
                _scoreSummaryLabel.text =
                    $"P: {Mathf.RoundToInt(_tracker.PlayerScore)} | " +
                    $"E: {Mathf.RoundToInt(_tracker.EnemyScore)}";
            }

            // Per-zone rows.
            for (int i = 0; i < _catalog.EntryCount; i++)
            {
                ControlZoneSO zone = _catalog.GetZone(i);
                if (zone == null) continue;

                GameObject row   = Object.Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>();

                if (texts.Length > 0) texts[0].text = zone.ZoneId;
                if (texts.Length > 1) texts[1].text = zone.IsCaptured ? "CAPTURED" : "OPEN";
                if (texts.Length > 2) texts[2].text = $"{Mathf.RoundToInt(zone.CaptureRatio * 100f)}%";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ControlZoneCatalogSO"/>. May be null.</summary>
        public ControlZoneCatalogSO Catalog => _catalog;

        /// <summary>The assigned <see cref="ZoneScoreTrackerSO"/>. May be null.</summary>
        public ZoneScoreTrackerSO Tracker => _tracker;
    }
}

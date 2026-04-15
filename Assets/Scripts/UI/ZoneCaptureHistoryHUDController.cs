using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a scrolling list of zone capture and loss
    /// events sourced from a <see cref="ZoneCaptureHistorySO"/> ring buffer.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel          → root container; hidden when _historySO is null.
    ///   _listContainer  → parent Transform where row GameObjects are spawned.
    ///   _rowPrefab      → prefab with ≥ 3 Text children:
    ///                       [0] = Zone ID label.
    ///                       [1] = "CAPTURED" or "LOST" badge.
    ///                       [2] = Timestamp formatted as "{t:F1}s".
    ///   _emptyLabel     → shown when history is null or empty.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes reactively to _onHistoryUpdated.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _historySO         → ZoneCaptureHistorySO asset.
    ///   2. Assign _onHistoryUpdated  → ZoneCaptureHistorySO._onHistoryUpdated channel.
    ///   3. Assign _listContainer, _rowPrefab, _emptyLabel, _panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCaptureHistoryHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneCaptureHistorySO _historySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Transform  _listContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Text       _emptyLabel;
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
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys existing rows and rebuilds the list from the current history.
        /// Shows _emptyLabel when history is null or empty.
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

            bool isEmpty = _historySO == null || _historySO.Count == 0;
            _emptyLabel?.gameObject.SetActive(isEmpty);

            if (isEmpty)
            {
                _panel?.SetActive(_historySO != null);
                return;
            }

            _panel?.SetActive(true);

            for (int i = 0; i < _historySO.Count; i++)
            {
                ZoneCaptureHistoryEntry entry = _historySO.GetEntry(i);
                GameObject row    = Object.Instantiate(_rowPrefab, _listContainer);
                Text[]     texts  = row.GetComponentsInChildren<Text>();

                if (texts.Length > 0) texts[0].text = entry.zoneId;
                if (texts.Length > 1) texts[1].text = entry.isCapture ? "CAPTURED" : "LOST";
                if (texts.Length > 2) texts[2].text = $"{entry.timestamp:F1}s";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneCaptureHistorySO"/>. May be null.</summary>
        public ZoneCaptureHistorySO HistorySO => _historySO;
    }
}

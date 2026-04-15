using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a per-zone occupation-time list from a
    /// <see cref="ZonePresenceTimerSO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel         → root container; hidden when _timerSO is null / on OnDisable.
    ///   _listContainer → parent Transform where per-zone row GameObjects are spawned.
    ///   _rowPrefab     → prefab with ≥ 2 Text children:
    ///                      [0] = Zone label ("Zone 1", "Zone 2", …).
    ///                      [1] = Presence time formatted as "{t:F1}s".
    ///   _emptyLabel    → shown when SO is null or MaxZones is 0.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes reactively to _onPresenceUpdated.
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
    public sealed class ZonePresenceTimerHUDController : MonoBehaviour
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
        /// Destroys existing rows and rebuilds the per-zone presence-time list.
        /// Shows <c>_emptyLabel</c> when the SO is null or has no zones.
        /// No-op when <c>_listContainer</c> or <c>_rowPrefab</c> is unassigned.
        /// Zero allocation after Awake.
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

            for (int i = 0; i < _timerSO.MaxZones; i++)
            {
                GameObject row   = Object.Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>();

                if (texts.Length > 0) texts[0].text = $"Zone {i + 1}";
                if (texts.Length > 1) texts[1].text = $"{_timerSO.GetPresenceTime(i):F1}s";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZonePresenceTimerSO"/>. May be null.</summary>
        public ZonePresenceTimerSO TimerSO => _timerSO;
    }
}

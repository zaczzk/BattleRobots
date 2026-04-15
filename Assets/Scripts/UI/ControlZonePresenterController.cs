using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that presents a summary of all control zones from a
    /// <see cref="ControlZoneCatalogSO"/> and reflects the player's dominance
    /// status from a <see cref="ZoneDominanceSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   OnEnable  → subscribes _onDominanceChanged → Refresh.
    ///   OnDisable → unsubscribes.
    ///   Refresh():
    ///     • Null catalog → hide _panel; return.
    ///     • Count captured zones by iterating the catalog.
    ///     • _summaryLabel.text = "Zones: {captured}/{total}".
    ///     • _dominanceBar.value = DominanceRatio (from _dominanceSO, or 0 if null).
    ///     • _panel.SetActive(true).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — NO Physics references.
    ///   - All refs optional; null refs produce silent no-ops.
    ///   - Delegates cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one presenter per HUD canvas.
    ///
    /// Scene wiring:
    ///   _catalog           → ControlZoneCatalogSO (zone definitions).
    ///   _dominanceSO       → ZoneDominanceSO (player zone count / ratio).
    ///   _onDominanceChanged → VoidGameEvent raised by ZoneDominanceSO on change.
    ///   _summaryLabel      → Text showing "Zones: X/N".
    ///   _dominanceBar      → Slider showing dominance ratio [0, 1].
    ///   _panel             → Root panel to show/hide.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ControlZonePresenterController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Catalog of ControlZoneSO assets for the current arena.")]
        [SerializeField] private ControlZoneCatalogSO _catalog;

        [Tooltip("Runtime ZoneDominanceSO for the player's dominance ratio.")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by ZoneDominanceSO whenever zone count changes.")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;

        // ── Inspector — UI Refs ───────────────────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Text displaying 'Zones: X/N' summary.")]
        [SerializeField] private Text _summaryLabel;

        [Tooltip("Slider driven by DominanceRatio [0, 1].")]
        [SerializeField] private Slider _dominanceBar;

        [Tooltip("Root panel shown when catalog is assigned, hidden otherwise.")]
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
            _onDominanceChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDominanceChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates all UI elements from the current catalog and dominance state.
        /// Hides the panel when the catalog is null.
        /// Zero allocation — integer arithmetic and string format only.
        /// </summary>
        public void Refresh()
        {
            if (_catalog == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int total    = _catalog.EntryCount;
            int captured = 0;
            for (int i = 0; i < total; i++)
            {
                ControlZoneSO zone = _catalog.GetZone(i);
                if (zone != null && zone.IsCaptured)
                    captured++;
            }

            if (_summaryLabel != null)
                _summaryLabel.text = $"Zones: {captured}/{total}";

            if (_dominanceBar != null)
                _dominanceBar.value = _dominanceSO != null ? _dominanceSO.DominanceRatio : 0f;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ControlZoneCatalogSO"/>. May be null.</summary>
        public ControlZoneCatalogSO Catalog => _catalog;

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the player's zone dominance state from a
    /// <see cref="ZoneDominanceSO"/> as a zone-count label, a dominance bar, and an
    /// optional bonus panel shown when the player holds a majority of zones.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _zoneCountLabel → "P: N / Total"    (player count over total zones)
    ///   _dominanceBar   → Slider.value = DominanceRatio  [0, 1]
    ///   _bonusPanel     → activated when HasDominance (player holds majority)
    ///   _panel          → activated on every Refresh; hidden when SO is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onDominanceChanged for reactive refresh.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_dominanceSO</c>         → the ZoneDominanceSO asset.
    ///   2. Assign <c>_onDominanceChanged</c>  → ZoneDominanceSO._onDominanceChanged.
    ///   3. Assign optional UI Text, Slider, and panel references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneDominanceHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _zoneCountLabel;
        [SerializeField] private Slider     _dominanceBar;
        [SerializeField] private GameObject _bonusPanel;
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

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD from the current dominance state.
        /// Hides the panel when <c>_dominanceSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_dominanceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_zoneCountLabel != null)
                _zoneCountLabel.text = $"P: {_dominanceSO.PlayerZoneCount} / {_dominanceSO.TotalZones}";

            if (_dominanceBar != null)
                _dominanceBar.value = _dominanceSO.DominanceRatio;

            _bonusPanel?.SetActive(_dominanceSO.HasDominance);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;
    }
}

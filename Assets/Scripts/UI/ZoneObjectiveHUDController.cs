using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the zone-control match objective progress and
    /// shows a completion badge when the win condition is met.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _objectiveLabel → "Hold N zones to win"  (N = RequiredZones)
    ///   _progressLabel  → "Holding X / N zones"  (live zone count)
    ///   _completeBadge  → activated when ZoneObjectiveSO.IsComplete
    ///   _panel          → hidden when either SO is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onDominanceChanged for live progress refresh.
    ///   - Subscribes to _onObjectiveComplete to show the completion badge.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _objectiveSO        → ZoneObjectiveSO asset.
    ///   2. Assign _dominanceSO        → ZoneDominanceSO asset.
    ///   3. Assign _onDominanceChanged → ZoneDominanceSO._onDominanceChanged VoidGameEvent.
    ///   4. Assign _onObjectiveComplete→ ZoneObjectiveSO._onObjectiveComplete VoidGameEvent.
    ///   5. Assign optional UI Text, badge, and panel refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneObjectiveHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneObjectiveSO  _objectiveSO;
        [SerializeField] private ZoneDominanceSO  _dominanceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;
        [SerializeField] private VoidGameEvent _onObjectiveComplete;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _objectiveLabel;
        [SerializeField] private Text       _progressLabel;
        [SerializeField] private GameObject _completeBadge;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;
        private Action _completeDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate  = Refresh;
            _completeDelegate = ShowComplete;
        }

        private void OnEnable()
        {
            _onDominanceChanged?.RegisterCallback(_refreshDelegate);
            _onObjectiveComplete?.RegisterCallback(_completeDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDominanceChanged?.UnregisterCallback(_refreshDelegate);
            _onObjectiveComplete?.UnregisterCallback(_completeDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void ShowComplete()
        {
            _completeBadge?.SetActive(true);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the HUD from the current objective and dominance state.
        /// Hides the panel when either required SO is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_objectiveSO == null || _dominanceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int required = _objectiveSO.RequiredZones;
            int held     = _dominanceSO.PlayerZoneCount;

            if (_objectiveLabel != null)
                _objectiveLabel.text = $"Hold {required} zones to win";

            if (_progressLabel != null)
                _progressLabel.text = $"Holding {held} / {required} zones";

            _completeBadge?.SetActive(_objectiveSO.IsComplete);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneObjectiveSO"/>. May be null.</summary>
        public ZoneObjectiveSO ObjectiveSO => _objectiveSO;

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;
    }
}

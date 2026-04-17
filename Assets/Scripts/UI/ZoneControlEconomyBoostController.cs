using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the economy boost timer and displays its
    /// current state to the player.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onBoostActivated</c> or <c>_onBoostExpired</c>: refreshes the
    ///   display.
    ///   <c>Update</c>: ticks <see cref="ZoneControlEconomyBoostSO"/> each frame
    ///   while the boost is active, then refreshes.
    ///   <see cref="Refresh"/>: when active, shows
    ///   "N× Economy Active: M:SS"; when inactive, shows "No Boost".
    ///   <c>_progressBar.value</c> mirrors <see cref="ZoneControlEconomyBoostSO.BoostProgress"/>.
    ///   Panel is hidden when <c>_boostSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one economy boost controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlEconomyBoostController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlEconomyBoostSO _boostSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBoostActivated;
        [SerializeField] private VoidGameEvent _onBoostExpired;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _progressBar;
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
            _onBoostActivated?.RegisterCallback(_refreshDelegate);
            _onBoostExpired?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBoostActivated?.UnregisterCallback(_refreshDelegate);
            _onBoostExpired?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_boostSO == null || !_boostSO.IsActive) return;
            _boostSO.Tick(Time.deltaTime);
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the status label and progress bar.
        /// Hides the panel when <c>_boostSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_boostSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                if (_boostSO.IsActive)
                {
                    int minutes = Mathf.FloorToInt(_boostSO.RemainingTime / 60f);
                    int seconds = Mathf.FloorToInt(_boostSO.RemainingTime % 60f);
                    _statusLabel.text = $"{_boostSO.BoostMultiplier:F0}x Economy Active: {minutes}:{seconds:D2}";
                }
                else
                {
                    _statusLabel.text = "No Boost";
                }
            }

            if (_progressBar != null)
                _progressBar.value = _boostSO.BoostProgress;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound economy boost SO (may be null).</summary>
        public ZoneControlEconomyBoostSO BoostSO => _boostSO;
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives a zone-lock cooldown HUD from
    /// <see cref="ZoneControlZoneLockSO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _lockProgressBar → value = <see cref="ZoneControlZoneLockSO.LockProgress"/> (1→0).
    ///   _lockLabel       → "Locked: {timer:F1}s" while locked / "Available" when free.
    ///   _panel           → shown while locked, hidden when the zone is available.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneLockHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneLockSO _lockSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneLocked;
        [SerializeField] private VoidGameEvent _onZoneUnlocked;

        [Header("UI Refs (optional)")]
        [SerializeField] private Slider _lockProgressBar;
        [SerializeField] private Text   _lockLabel;

        [Header("UI Refs — Panel (optional)")]
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
            _onZoneLocked?.RegisterCallback(_refreshDelegate);
            _onZoneUnlocked?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneLocked?.UnregisterCallback(_refreshDelegate);
            _onZoneUnlocked?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the lock HUD from the current SO state.
        /// Hides the panel when <c>_lockSO</c> is null or the zone is not locked.
        /// </summary>
        public void Refresh()
        {
            if (_lockSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            bool locked = _lockSO.IsLocked;
            _panel?.SetActive(locked);

            if (_lockProgressBar != null)
                _lockProgressBar.value = _lockSO.LockProgress;

            if (_lockLabel != null)
                _lockLabel.text = locked
                    ? $"Locked: {_lockSO.LockTimer:F1}s"
                    : "Available";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound zone-lock SO (may be null).</summary>
        public ZoneControlZoneLockSO LockSO => _lockSO;
    }
}

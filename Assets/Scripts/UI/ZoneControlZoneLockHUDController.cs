using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that visualises the <see cref="ZoneControlZoneLockSO"/>
    /// lock state: shows a cooldown progress bar and a label while a zone is locked,
    /// and hides the panel (or shows "Available") when the zone is free.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _lockProgressBar → Slider driven by ZoneControlZoneLockSO.LockProgress.
    ///   _lockLabel       → "Locked: F1s" remaining / "Available".
    ///   _panel           → Root panel; hidden when <c>_lockSO</c> is null.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Subscribes <c>_onZoneLocked</c>   → <see cref="HandleZoneLocked"/>.
    ///   • Subscribes <c>_onZoneUnlocked</c> → <see cref="HandleZoneUnlocked"/>.
    ///   • Subscribes <c>_onMatchStarted</c> → <see cref="HandleMatchStarted"/>
    ///     (resets <c>_isLocked</c> flag and refreshes).
    ///   • <c>Update</c> calls <see cref="Tick(float)"/> each frame so the progress
    ///     bar stays live while the zone is locked.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one zone-lock HUD per panel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneLockHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneLockSO _lockSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlZoneLockSO._onZoneLocked.")]
        [SerializeField] private VoidGameEvent _onZoneLocked;

        [Tooltip("Wire to ZoneControlZoneLockSO._onZoneUnlocked.")]
        [SerializeField] private VoidGameEvent _onZoneUnlocked;

        [Tooltip("Raised at match start; resets displayed state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI Refs (optional)")]
        [SerializeField] private Slider _lockProgressBar;
        [SerializeField] private Text   _lockLabel;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _isLocked;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleLockedDelegate;
        private Action _handleUnlockedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleLockedDelegate       = HandleZoneLocked;
            _handleUnlockedDelegate     = HandleZoneUnlocked;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onZoneLocked?.RegisterCallback(_handleLockedDelegate);
            _onZoneUnlocked?.RegisterCallback(_handleUnlockedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneLocked?.UnregisterCallback(_handleLockedDelegate);
            _onZoneUnlocked?.UnregisterCallback(_handleUnlockedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        private void Update() => Tick(Time.deltaTime);

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Records the lock state and refreshes the HUD.</summary>
        public void HandleZoneLocked()
        {
            _isLocked = true;
            Refresh();
        }

        /// <summary>Records the unlocked state and refreshes the HUD.</summary>
        public void HandleZoneUnlocked()
        {
            _isLocked = false;
            Refresh();
        }

        /// <summary>Resets displayed state at match start and refreshes.</summary>
        public void HandleMatchStarted()
        {
            _isLocked = false;
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the live progress bar each frame while the zone is locked.
        /// No-op when not locked or <c>_lockSO</c> is null.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isLocked || _lockSO == null) return;
            Refresh();
        }

        /// <summary>
        /// Rebuilds the progress bar and label from the current lock state.
        /// Hides the panel when <c>_lockSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_lockSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_lockSO.IsLocked)
            {
                float remaining = _lockSO.LockProgress * _lockSO.LockDuration;

                if (_lockProgressBar != null)
                    _lockProgressBar.value = _lockSO.LockProgress;

                if (_lockLabel != null)
                    _lockLabel.text = $"Locked: {remaining:F1}s";
            }
            else
            {
                if (_lockProgressBar != null)
                    _lockProgressBar.value = 0f;

                if (_lockLabel != null)
                    _lockLabel.text = "Available";
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound zone lock SO (may be null).</summary>
        public ZoneControlZoneLockSO LockSO => _lockSO;

        /// <summary>True when the HUD has been notified of a locked state.</summary>
        public bool IsLocked => _isLocked;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Shows a "N Win Streak! Bonus unlocked!" banner whenever the player's win streak
    /// crosses a configured <see cref="WinStreakMilestoneSO"/> threshold.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   1. <see cref="WinStreakSO.RecordWin"/> increments CurrentStreak and raises
    ///      <c>_onStreakChanged</c>.
    ///   2. This controller checks whether the new streak value matches a milestone
    ///      in <see cref="WinStreakMilestoneSO"/> (via <c>HasMilestoneAtStreak</c>)
    ///      AND the streak increased (guards against loss-driven resets re-triggering).
    ///   3. If both conditions pass, the banner is shown and the Tick timer is started.
    ///   4. Optionally forwards the message to a <see cref="NotificationQueueSO"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one notification banner per canvas.
    ///   - All inspector fields optional — null refs are handled safely.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _winStreak           → shared WinStreakSO asset.
    ///   _milestoneConfig     → shared WinStreakMilestoneSO asset.
    ///   _onStreakChanged      → same VoidGameEvent as WinStreakSO raises.
    ///   _notificationPanel   → banner root panel (starts inactive).
    ///   _notificationLabel   → Text child receiving "N Win Streak! Bonus unlocked!".
    ///   _notificationQueue   → optional global NotificationQueueSO.
    ///   _displayDuration     → seconds the banner stays visible (default 2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StreakBonusNotificationController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("WinStreakSO tracking the current win streak. " +
                 "Read for CurrentStreak on each _onStreakChanged event.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Tooltip("Milestone catalog. HasMilestoneAtStreak(N) determines whether the " +
                 "current streak value triggers a notification.")]
        [SerializeField] private WinStreakMilestoneSO _milestoneConfig;

        // ── Inspector — Event ─────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by WinStreakSO after every RecordWin() / RecordLoss(). " +
                 "Drives the milestone check.")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Banner root panel. Activated when a milestone is crossed; " +
                 "deactivated when the display timer expires.")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text label receiving 'N Win Streak! Bonus unlocked!'.")]
        [SerializeField] private Text _notificationLabel;

        // ── Inspector — Notification Queue ────────────────────────────────────

        [Header("Notification Queue (optional)")]
        [Tooltip("When assigned, the streak notification is also forwarded here.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        // ── Inspector — Settings ──────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Seconds the banner remains visible.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 2f;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _onStreakDelegate;
        private float  _displayTimer;
        private int    _previousStreak;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onStreakDelegate = OnStreakChanged;
        }

        private void OnEnable()
        {
            _previousStreak = _winStreak != null ? _winStreak.CurrentStreak : 0;
            _onStreakChanged?.RegisterCallback(_onStreakDelegate);
            _notificationPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onStreakChanged?.UnregisterCallback(_onStreakDelegate);
            _notificationPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the display timer and hides the panel when it expires.
        /// Exposed as public so tests can drive the timer without
        /// <c>Time.deltaTime</c> being non-zero in EditMode.
        /// </summary>
        public void Tick(float dt)
        {
            if (_displayTimer <= 0f) return;

            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                _notificationPanel?.SetActive(false);
        }

        private void OnStreakChanged()
        {
            int newStreak = _winStreak != null ? _winStreak.CurrentStreak : 0;

            // Only show when the streak increased (not on losses that reset to 0)
            // and the new value matches a configured milestone.
            if (newStreak > _previousStreak
                && _milestoneConfig != null
                && _milestoneConfig.HasMilestoneAtStreak(newStreak))
            {
                ShowNotification(newStreak);
            }

            _previousStreak = newStreak;
        }

        private void ShowNotification(int streak)
        {
            string message = string.Format("{0} Win Streak! Bonus unlocked!", streak);

            if (_notificationLabel != null) _notificationLabel.text = message;

            _notificationPanel?.SetActive(true);
            _displayTimer = _displayDuration;

            _notificationQueue?.Enqueue(message, string.Empty, _displayDuration);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="WinStreakSO"/>. May be null.</summary>
        public WinStreakSO WinStreak => _winStreak;

        /// <summary>The assigned <see cref="WinStreakMilestoneSO"/>. May be null.</summary>
        public WinStreakMilestoneSO MilestoneConfig => _milestoneConfig;

        /// <summary>The assigned <see cref="NotificationQueueSO"/>. May be null.</summary>
        public NotificationQueueSO NotificationQueue => _notificationQueue;

        /// <summary>Seconds the banner remains visible. Defaults to 2.</summary>
        public float DisplayDuration => _displayDuration;

        /// <summary>Remaining display time in seconds. 0 when the panel is hidden.</summary>
        public float DisplayTimer => _displayTimer;
    }
}

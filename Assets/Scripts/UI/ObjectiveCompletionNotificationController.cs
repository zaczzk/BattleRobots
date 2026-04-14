using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays a toast banner each time an objective is completed.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   1. <see cref="_onObjectiveCompleted"/> fires (raised by MatchObjectiveTrackerSO).
    ///   2. This MB activates <see cref="_notificationPanel"/>, sets
    ///      <c>_messageLabel</c> to "Objective Complete!" and <c>_rewardLabel</c>
    ///      to "+N credits" (0 reward → empty string).
    ///   3. <see cref="Tick"/> (driven by <c>Update</c>) counts down
    ///      <see cref="_displayDuration"/> and hides the panel when it reaches zero.
    ///   4. When <see cref="_notificationQueue"/> is assigned the same message is also
    ///      enqueued for the global <see cref="NotificationController"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one banner per canvas.
    ///   - Delegate cached in Awake; zero heap allocation after initialisation.
    ///   - All inspector fields optional.
    ///
    /// Scene wiring:
    ///   _bonusObjective       → MatchBonusObjectiveSO (for reward amount label).
    ///   _onObjectiveCompleted → same VoidGameEvent as MatchObjectiveTrackerSO._onObjectiveCompleted.
    ///   _notificationPanel    → banner root panel (starts inactive).
    ///   _messageLabel         → Text child for "Objective Complete!" headline.
    ///   _rewardLabel          → Text child for "+N credits" sub-label.
    ///   _notificationQueue    → optional NotificationQueueSO to forward toast globally.
    ///   _displayDuration      → seconds the banner stays visible (default 2.5).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectiveCompletionNotificationController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Provides BonusReward for the reward sub-label text. " +
                 "Leave null to omit the reward amount.")]
        [SerializeField] private MatchBonusObjectiveSO _bonusObjective;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchObjectiveTrackerSO once per objective completion.")]
        [SerializeField] private VoidGameEvent _onObjectiveCompleted;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root notification banner panel. Activated on completion; " +
                 "deactivated when the display timer expires.")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text label for the completion headline: \"Objective Complete!\"")]
        [SerializeField] private Text _messageLabel;

        [Tooltip("Text label for the reward sub-line: \"+N credits\", or empty when reward is 0.")]
        [SerializeField] private Text _rewardLabel;

        // ── Inspector — Notification Queue ────────────────────────────────────

        [Header("Notification Queue (optional)")]
        [Tooltip("When assigned, each completion is also forwarded to this queue " +
                 "so the global NotificationController can display it in sequence.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        // ── Inspector — Settings ──────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Seconds the notification banner remains visible.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 2.5f;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _completedDelegate;
        private float  _displayTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _completedDelegate = OnObjectiveCompleted;
        }

        private void OnEnable()
        {
            _onObjectiveCompleted?.RegisterCallback(_completedDelegate);
            _notificationPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onObjectiveCompleted?.UnregisterCallback(_completedDelegate);
            _notificationPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the display timer and hides the panel when the timer expires.
        /// Exposed as public so tests can drive the timer without relying on
        /// <c>Time.deltaTime</c> being non-zero in EditMode.
        /// </summary>
        public void Tick(float dt)
        {
            if (_displayTimer <= 0f) return;

            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                _notificationPanel?.SetActive(false);
        }

        private void OnObjectiveCompleted()
        {
            ShowNotification();
        }

        private void ShowNotification()
        {
            int    reward     = _bonusObjective != null ? _bonusObjective.BonusReward : 0;
            string message    = "Objective Complete!";
            string rewardText = reward > 0 ? $"+{reward} credits" : string.Empty;

            if (_messageLabel != null) _messageLabel.text = message;
            if (_rewardLabel  != null) _rewardLabel.text  = rewardText;

            _notificationPanel?.SetActive(true);
            _displayTimer = _displayDuration;

            _notificationQueue?.Enqueue(message, rewardText, _displayDuration);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchBonusObjectiveSO"/>. May be null.</summary>
        public MatchBonusObjectiveSO BonusObjective => _bonusObjective;

        /// <summary>The assigned <see cref="NotificationQueueSO"/>. May be null.</summary>
        public NotificationQueueSO NotificationQueue => _notificationQueue;

        /// <summary>Seconds the notification banner remains visible. Defaults to 2.5.</summary>
        public float DisplayDuration => _displayDuration;

        /// <summary>Remaining display time in seconds. 0 when the panel is hidden.</summary>
        public float DisplayTimer => _displayTimer;
    }
}

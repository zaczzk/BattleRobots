using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays a toast banner each time a mastery-milestone reward is granted by
    /// <see cref="BattleRobots.Physics.MilestoneRewardApplier"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   1. <see cref="BattleRobots.Physics.MilestoneRewardApplier"/> raises
    ///      <c>_onRewardGranted</c> once per newly-cleared milestone.
    ///   2. This MB's cached delegate calls <see cref="ShowNotification"/>.
    ///   3. The panel activates, label is set to "{label} Reached!" and the optional
    ///      reward-amount text to "+N".
    ///   4. <see cref="Tick"/> (driven by <c>Update</c>) decrements <c>_displayTimer</c>
    ///      and hides the panel when it reaches zero.
    ///   5. When <c>_notificationQueue</c> is assigned the same message is also
    ///      enqueued there so the global <see cref="NotificationController"/> can
    ///      show it in sequence.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one banner per canvas.
    ///   - All inspector fields optional — assign only those present in the scene.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _catalog            → MilestoneRewardCatalogSO (read for label text).
    ///   _onRewardGranted    → same VoidGameEvent as MilestoneRewardApplier._onRewardGranted.
    ///   _notificationPanel  → banner root panel (starts inactive).
    ///   _rewardLabel        → Text child for "{label} Reached!" headline.
    ///   _rewardAmountText   → Text child for "+N" sub-label (optional).
    ///   _notificationQueue  → optional NotificationQueueSO to forward toast globally.
    ///   _displayDuration    → seconds the banner stays visible (default 2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MilestoneRewardNotificationController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Catalog SO providing the label text used in the notification banner. " +
                 "Leave null to use 'Milestone Reward' as the fallback label.")]
        [SerializeField] private MilestoneRewardCatalogSO _catalog;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MilestoneRewardApplier once per milestone cleared. " +
                 "Leave null to disable automatic display.")]
        [SerializeField] private VoidGameEvent _onRewardGranted;

        [Header("UI (all optional)")]
        [Tooltip("Root notification banner panel. Activated when a milestone reward fires; " +
                 "deactivated when the display timer expires.")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text label that receives the reward headline, e.g. 'Milestone Reward Reached!'")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("Optional separate text label for the reward amount, e.g. '+250'. " +
                 "Reads from MilestoneRewardCatalogSO if the catalog is assigned. " +
                 "Left blank when no catalog is assigned.")]
        [SerializeField] private Text _rewardAmountText;

        [Header("Notification Queue (optional)")]
        [Tooltip("When assigned, each milestone reward is also forwarded to this queue " +
                 "so the global NotificationController can display it in order.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        [Header("Settings")]
        [Tooltip("Seconds the notification banner remains visible.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 2f;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _onRewardDelegate;
        private float  _displayTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onRewardDelegate = OnRewardGranted;
        }

        private void OnEnable()
        {
            _onRewardGranted?.RegisterCallback(_onRewardDelegate);
            _notificationPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onRewardGranted?.UnregisterCallback(_onRewardDelegate);
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

        /// <summary>Called when <c>_onRewardGranted</c> fires.</summary>
        private void OnRewardGranted()
        {
            ShowNotification();
        }

        /// <summary>
        /// Activates the notification panel, sets label text and amount text,
        /// resets the display timer, and optionally forwards to the global
        /// <see cref="NotificationQueueSO"/>.
        /// </summary>
        private void ShowNotification()
        {
            string label  = _catalog != null ? _catalog.RewardLabel : "Milestone Reward";
            string header = label + " Reached!";

            if (_rewardLabel      != null) _rewardLabel.text      = header;
            if (_rewardAmountText != null) _rewardAmountText.text = string.Empty;

            _notificationPanel?.SetActive(true);
            _displayTimer = _displayDuration;

            // Forward to the global notification queue when wired.
            _notificationQueue?.Enqueue(header, string.Empty, _displayDuration);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MilestoneRewardCatalogSO"/>. May be null.</summary>
        public MilestoneRewardCatalogSO Catalog => _catalog;

        /// <summary>The assigned <see cref="NotificationQueueSO"/>. May be null.</summary>
        public NotificationQueueSO NotificationQueue => _notificationQueue;

        /// <summary>Seconds the notification banner remains visible. Defaults to 2.</summary>
        public float DisplayDuration => _displayDuration;

        /// <summary>Remaining display time in seconds. 0 when the panel is hidden.</summary>
        public float DisplayTimer => _displayTimer;
    }
}

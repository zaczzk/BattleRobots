using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Flashes a "+N credits" banner for a configurable duration whenever a match ends
    /// and currency was earned.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   1. <see cref="BattleRobots.Core.MatchManager"/> writes <see cref="MatchResultSO"/>
    ///      and then raises <c>_onMatchEnded</c>.
    ///   2. This MB's cached delegate fires <see cref="OnMatchEnded"/>.
    ///   3. The panel activates with "+N" label text (or "+1 credits" when the result
    ///      SO is null as a defensive fallback).
    ///   4. <see cref="Tick"/> (driven by <c>Update</c>) decrements <c>_flashTimer</c>
    ///      and hides the panel when it reaches zero.
    ///   5. When <c>_notificationQueue</c> is assigned the same message is also
    ///      enqueued there for the global <see cref="NotificationController"/> to show.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one flash banner per canvas.
    ///   - All inspector fields optional — assign only those present in the scene.
    ///   - Zero currency earned silently skips the flash (no "+0" spam).
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _matchResult          → shared MatchResultSO blackboard (read for CurrencyEarned).
    ///   _onMatchEnded         → same VoidGameEvent as MatchManager raises at match end.
    ///   _flashPanel           → banner root panel (starts inactive).
    ///   _currencyLabel        → Text child receiving "+N credits".
    ///   _notificationQueue    → optional NotificationQueueSO for global queue forwarding.
    ///   _flashDuration        → seconds the banner stays visible (default 2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurrencyEarnedFlashController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Blackboard SO written by MatchManager before _onMatchEnded fires. " +
                 "Read for CurrencyEarned. Leave null to show a generic '+? credits' label.")]
        [SerializeField] private MatchResultSO _matchResult;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager at the end of each match. " +
                 "Leave null to disable automatic display.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("UI (all optional)")]
        [Tooltip("Root flash panel. Activated when a non-zero reward is earned; " +
                 "deactivated when the flash timer expires.")]
        [SerializeField] private GameObject _flashPanel;

        [Tooltip("Text label that receives '+N credits'.")]
        [SerializeField] private Text _currencyLabel;

        [Header("Notification Queue (optional)")]
        [Tooltip("When assigned, the reward flash is also forwarded to this queue " +
                 "so the global NotificationController can show it in order.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        [Header("Settings")]
        [Tooltip("Seconds the flash banner remains visible.")]
        [SerializeField, Min(0.1f)] private float _flashDuration = 2f;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _onMatchEndedDelegate;
        private float  _flashTimer;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchEndedDelegate = OnMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_onMatchEndedDelegate);
            _flashPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_onMatchEndedDelegate);
            _flashPanel?.SetActive(false);
            _flashTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the flash timer and hides the panel when it expires.
        /// Exposed as public so tests can drive the timer without relying on
        /// <c>Time.deltaTime</c> being non-zero in EditMode.
        /// </summary>
        public void Tick(float dt)
        {
            if (_flashTimer <= 0f) return;

            _flashTimer -= dt;
            if (_flashTimer <= 0f)
                _flashPanel?.SetActive(false);
        }

        /// <summary>Called when <c>_onMatchEnded</c> fires.</summary>
        private void OnMatchEnded()
        {
            int earned = _matchResult != null ? _matchResult.CurrencyEarned : 0;

            // Skip the flash entirely when zero currency was earned.
            if (earned <= 0) return;

            string label = string.Format("+{0} credits", earned);

            if (_currencyLabel != null) _currencyLabel.text = label;

            _flashPanel?.SetActive(true);
            _flashTimer = _flashDuration;

            // Forward to the global notification queue when wired.
            _notificationQueue?.Enqueue(label, string.Empty, _flashDuration);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchResultSO"/>. May be null.</summary>
        public MatchResultSO MatchResult => _matchResult;

        /// <summary>The assigned <see cref="NotificationQueueSO"/>. May be null.</summary>
        public NotificationQueueSO NotificationQueue => _notificationQueue;

        /// <summary>Seconds the flash banner remains visible. Defaults to 2.</summary>
        public float FlashDuration => _flashDuration;

        /// <summary>Remaining flash time in seconds. 0 when the panel is hidden.</summary>
        public float FlashTimer => _flashTimer;
    }
}

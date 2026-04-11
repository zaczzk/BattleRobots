using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays queued toast notifications one at a time, auto-dismissing each after
    /// its configured duration, then immediately pulling the next from the queue.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   1. A producer (e.g. <see cref="BattleRobots.Core.AchievementManager"/>)
    ///      calls <see cref="NotificationQueueSO.Enqueue"/>, which raises
    ///      <c>_onNotificationEnqueued</c>.
    ///   2. This MB's cached delegate fires <see cref="TryShowNext"/>.
    ///   3. If no notification is currently displayed, the next item is dequeued and
    ///      the <c>ShowNotification</c> coroutine runs; the panel becomes visible.
    ///   4. After the duration elapses (<c>WaitForSecondsRealtime</c> — unaffected by
    ///      time scale pauses), the panel hides and <see cref="TryShowNext"/> is called
    ///      again to drain remaining queued items.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All Action delegates cached in Awake — zero alloc in event callbacks.
    ///   - No Update; entirely event-driven and coroutine-driven.
    ///   - <see cref="OnDisable"/> stops any in-flight coroutine and hides the panel
    ///     so scene transitions clean up gracefully.
    ///   - Uses <c>WaitForSecondsRealtime</c> so notifications remain visible while
    ///     the game is paused (<c>Time.timeScale == 0</c>).
    ///
    /// ── Wiring instructions ───────────────────────────────────────────────────
    ///   1. Add this MB to any persistent Canvas GameObject (e.g. the HUD root).
    ///   2. Assign <c>_queue</c>                  → the NotificationQueueSO asset.
    ///   3. Assign <c>_onNotificationEnqueued</c>  → the VoidGameEvent SO wired to
    ///      <c>NotificationQueueSO._onNotificationEnqueued</c>.
    ///   4. Assign <c>_notificationPanel</c> (optional) → the toast panel root GO.
    ///   5. Assign <c>_titleText</c> and <c>_bodyText</c> (optional) Text components.
    /// </summary>
    public sealed class NotificationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data Source")]
        [Tooltip("NotificationQueueSO holding pending toasts. Required for any display.")]
        [SerializeField] private NotificationQueueSO _queue;

        [Header("Event Channel — In")]
        [Tooltip("VoidGameEvent raised by NotificationQueueSO after each Enqueue() call. " +
                 "Wire the same asset used by NotificationQueueSO._onNotificationEnqueued.")]
        [SerializeField] private VoidGameEvent _onNotificationEnqueued;

        [Header("UI References (all optional)")]
        [Tooltip("Root GameObject shown when a notification is active, hidden otherwise.")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text component for the notification headline (title).")]
        [SerializeField] private Text _titleText;

        [Tooltip("Text component for the notification body detail.")]
        [SerializeField] private Text _bodyText;

        // ── Private state ─────────────────────────────────────────────────────

        private Action    _tryShowNextDelegate;
        private bool      _isShowing;
        private Coroutine _activeCoroutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _tryShowNextDelegate = TryShowNext;
        }

        private void OnEnable()
        {
            _onNotificationEnqueued?.RegisterCallback(_tryShowNextDelegate);

            // Ensure the panel starts hidden whenever this component is enabled.
            _notificationPanel?.SetActive(false);
            _isShowing = false;
        }

        private void OnDisable()
        {
            _onNotificationEnqueued?.UnregisterCallback(_tryShowNextDelegate);

            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            _notificationPanel?.SetActive(false);
            _isShowing = false;
        }

        // ── Internal logic ────────────────────────────────────────────────────

        private void TryShowNext()
        {
            // Guard: already showing, no queue, or queue is empty.
            if (_isShowing || _queue == null) return;
            if (!_queue.TryDequeue(out NotificationData data)) return;

            _isShowing       = true;
            _activeCoroutine = StartCoroutine(ShowNotification(data));
        }

        private IEnumerator ShowNotification(NotificationData data)
        {
            if (_titleText != null) _titleText.text = data.title;
            if (_bodyText  != null) _bodyText.text  = data.body;
            _notificationPanel?.SetActive(true);

            // WaitForSecondsRealtime is immune to Time.timeScale == 0 (PauseManager).
            float visibleDuration = data.duration > 0f ? data.duration : 3f;
            yield return new WaitForSecondsRealtime(visibleDuration);

            _notificationPanel?.SetActive(false);
            _isShowing       = false;
            _activeCoroutine = null;

            // Drain: immediately show the next queued notification if one exists.
            TryShowNext();
        }
    }
}

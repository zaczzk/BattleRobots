using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data container for a single in-game notification toast.
    /// Carried through <see cref="NotificationQueueSO"/> and consumed by
    /// <see cref="BattleRobots.UI.NotificationController"/>.
    /// </summary>
    [Serializable]
    public struct NotificationData
    {
        /// <summary>Short headline shown in the notification banner (e.g. "Achievement Unlocked!").</summary>
        public string title;

        /// <summary>One-line detail text (e.g. the achievement description).</summary>
        public string body;

        /// <summary>How many real-time seconds the notification should remain visible. Default 3 s.</summary>
        public float duration;
    }

    /// <summary>
    /// Runtime SO that acts as a first-in-first-out notification queue.
    /// Producers call <see cref="Enqueue"/> to push a toast notification;
    /// consumers (e.g. <see cref="BattleRobots.UI.NotificationController"/>) subscribe
    /// to <c>_onNotificationEnqueued</c> and then call <see cref="TryDequeue"/> to pop
    /// and display the next item.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - The queue is runtime-only (not serialized); it clears on domain reload /
    ///     scene load, which is the desired behaviour for ephemeral toasts.
    ///   - <c>_onNotificationEnqueued</c> fires once per <see cref="Enqueue"/> call
    ///     so the consumer can wake up without polling.
    ///
    /// ── Wiring instructions ───────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ NotificationQueueSO.
    ///   2. Assign a VoidGameEvent SO to <c>_onNotificationEnqueued</c>.
    ///   3. Assign this SO to <see cref="BattleRobots.Core.AchievementManager._notificationQueue"/>
    ///      (and any other producer) to forward unlock/level-up toasts.
    ///   4. Assign this SO and the VoidGameEvent to
    ///      <see cref="BattleRobots.UI.NotificationController"/> in the Arena/Menu Canvas.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NotificationQueue",
        menuName = "BattleRobots/Core/NotificationQueueSO")]
    public sealed class NotificationQueueSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel — Out")]
        [Tooltip("Raised once per Enqueue() call so the NotificationController wakes up " +
                 "and drains the next item.")]
        [SerializeField] private VoidGameEvent _onNotificationEnqueued;

        // ── Runtime queue (not serialized — clears on domain reload) ──────────

        private readonly Queue<NotificationData> _queue = new Queue<NotificationData>();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of notifications currently waiting in the queue (≥ 0).</summary>
        public int Count => _queue.Count;

        // ── Producer API ──────────────────────────────────────────────────────

        /// <summary>
        /// Pushes a new notification to the back of the queue and raises
        /// <c>_onNotificationEnqueued</c> so the consumer can wake up.
        /// A zero or negative <paramref name="duration"/> is clamped to 3 s.
        /// </summary>
        /// <param name="title">Headline text, e.g. "Achievement Unlocked!".</param>
        /// <param name="body">Detail text, e.g. the achievement description.</param>
        /// <param name="duration">Seconds visible (real time). Defaults to 3 s.</param>
        public void Enqueue(string title, string body, float duration = 3f)
        {
            _queue.Enqueue(new NotificationData
            {
                title    = title,
                body     = body,
                duration = duration > 0f ? duration : 3f,
            });
            _onNotificationEnqueued?.Raise();
        }

        // ── Consumer API ──────────────────────────────────────────────────────

        /// <summary>
        /// Removes and returns the notification at the front of the queue.
        /// Returns <c>false</c> (and sets <paramref name="data"/> to <c>default</c>)
        /// when the queue is empty.
        /// </summary>
        public bool TryDequeue(out NotificationData data)
        {
            if (_queue.Count > 0)
            {
                data = _queue.Dequeue();
                return true;
            }
            data = default;
            return false;
        }

        /// <summary>
        /// Returns the front notification without removing it.
        /// Returns <c>false</c> (and sets <paramref name="data"/> to <c>default</c>)
        /// when the queue is empty.
        /// </summary>
        public bool TryPeek(out NotificationData data)
        {
            if (_queue.Count > 0)
            {
                data = _queue.Peek();
                return true;
            }
            data = default;
            return false;
        }

        /// <summary>Removes all pending notifications. Does not fire any event.</summary>
        public void Clear() => _queue.Clear();

        /// <summary>
        /// Alias for <see cref="Clear"/>. Intended for use in tests so test SetUp / TearDown
        /// can reset state without accessing internal fields.
        /// </summary>
        public void Reset() => _queue.Clear();

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onNotificationEnqueued == null)
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

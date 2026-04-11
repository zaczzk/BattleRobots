using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="NotificationQueueSO"/> and the
    /// <see cref="NotificationData"/> struct.
    ///
    /// Covers:
    ///   • Fresh-instance contract: Count == 0.
    ///   • <see cref="NotificationQueueSO.Enqueue"/>: Count increments; FIFO order;
    ///     zero/negative duration clamped to 3 s; default duration == 3 s;
    ///     <c>_onNotificationEnqueued</c> fired once per call.
    ///   • <see cref="NotificationQueueSO.TryDequeue"/>: empty returns false;
    ///     non-empty returns true + correct data; Count decrements; FIFO preserved.
    ///   • <see cref="NotificationQueueSO.TryPeek"/>: empty returns false;
    ///     non-empty returns true; does NOT decrement Count;
    ///     peek title matches subsequent dequeue title.
    ///   • <see cref="NotificationQueueSO.Clear"/> and
    ///     <see cref="NotificationQueueSO.Reset"/>: queue emptied.
    /// </summary>
    public class NotificationQueueSOTests
    {
        private NotificationQueueSO _queue;

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _queue = ScriptableObject.CreateInstance<NotificationQueueSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_queue);
            _queue = null;
        }

        // ── Fresh-instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            Assert.AreEqual(0, _queue.Count);
        }

        // ── Enqueue ───────────────────────────────────────────────────────────

        [Test]
        public void Enqueue_SingleItem_CountIsOne()
        {
            _queue.Enqueue("Title", "Body");
            Assert.AreEqual(1, _queue.Count);
        }

        [Test]
        public void Enqueue_TwoItems_CountIsTwo()
        {
            _queue.Enqueue("A", "a");
            _queue.Enqueue("B", "b");
            Assert.AreEqual(2, _queue.Count);
        }

        [Test]
        public void Enqueue_DefaultDuration_IsThreeSeconds()
        {
            _queue.Enqueue("T", "B"); // no explicit duration
            _queue.TryDequeue(out NotificationData data);
            Assert.AreEqual(3f, data.duration, 0.001f,
                "Default duration must be 3 s when not specified.");
        }

        [Test]
        public void Enqueue_ZeroDuration_ClampedToThreeSeconds()
        {
            _queue.Enqueue("T", "B", 0f);
            _queue.TryDequeue(out NotificationData data);
            Assert.AreEqual(3f, data.duration, 0.001f,
                "Zero duration must be clamped to 3 s.");
        }

        [Test]
        public void Enqueue_NegativeDuration_ClampedToThreeSeconds()
        {
            _queue.Enqueue("T", "B", -5f);
            _queue.TryDequeue(out NotificationData data);
            Assert.AreEqual(3f, data.duration, 0.001f,
                "Negative duration must be clamped to 3 s.");
        }

        [Test]
        public void Enqueue_FiresOnNotificationEnqueued_OncePerCall()
        {
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            InjectEvent(_queue, evt);

            int fireCount = 0;
            evt.RegisterCallback(() => fireCount++);

            _queue.Enqueue("A", "a");
            _queue.Enqueue("B", "b");

            Assert.AreEqual(2, fireCount,
                "_onNotificationEnqueued must fire once per Enqueue() call.");
            Object.DestroyImmediate(evt);
        }

        // ── TryDequeue ────────────────────────────────────────────────────────

        [Test]
        public void TryDequeue_EmptyQueue_ReturnsFalse()
        {
            bool result = _queue.TryDequeue(out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryDequeue_SingleItem_ReturnsTrue()
        {
            _queue.Enqueue("T", "B");
            bool result = _queue.TryDequeue(out _);
            Assert.IsTrue(result);
        }

        [Test]
        public void TryDequeue_ReturnsCorrectTitleBodyDuration()
        {
            _queue.Enqueue("Hello", "World", 5f);
            _queue.TryDequeue(out NotificationData data);
            Assert.AreEqual("Hello", data.title);
            Assert.AreEqual("World", data.body);
            Assert.AreEqual(5f, data.duration, 0.001f);
        }

        [Test]
        public void TryDequeue_AfterAllItems_CountIsZero()
        {
            _queue.Enqueue("T", "B");
            _queue.TryDequeue(out _);
            Assert.AreEqual(0, _queue.Count);
        }

        [Test]
        public void TryDequeue_FIFO_FirstEnqueuedIsFirstDequeued()
        {
            _queue.Enqueue("First", "f");
            _queue.Enqueue("Second", "s");
            _queue.TryDequeue(out NotificationData first);
            _queue.TryDequeue(out NotificationData second);
            Assert.AreEqual("First", first.title);
            Assert.AreEqual("Second", second.title);
        }

        // ── TryPeek ───────────────────────────────────────────────────────────

        [Test]
        public void TryPeek_EmptyQueue_ReturnsFalse()
        {
            bool result = _queue.TryPeek(out _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryPeek_NonEmpty_ReturnsTrue()
        {
            _queue.Enqueue("T", "B");
            bool result = _queue.TryPeek(out _);
            Assert.IsTrue(result);
        }

        [Test]
        public void TryPeek_DoesNotConsumeItem_CountUnchanged()
        {
            _queue.Enqueue("T", "B");
            _queue.TryPeek(out _);
            Assert.AreEqual(1, _queue.Count,
                "TryPeek must not remove the item from the queue.");
        }

        [Test]
        public void TryPeek_TitleMatchesSubsequentDequeue()
        {
            _queue.Enqueue("Head", "h");
            _queue.TryPeek(out NotificationData peeked);
            _queue.TryDequeue(out NotificationData dequeued);
            Assert.AreEqual(peeked.title, dequeued.title,
                "Peeked title must equal the title returned by TryDequeue.");
        }

        // ── Clear / Reset ─────────────────────────────────────────────────────

        [Test]
        public void Clear_EmptiesQueue()
        {
            _queue.Enqueue("A", "a");
            _queue.Enqueue("B", "b");
            _queue.Clear();
            Assert.AreEqual(0, _queue.Count);
        }

        [Test]
        public void Reset_ClearsQueue()
        {
            _queue.Enqueue("A", "a");
            _queue.Reset();
            Assert.AreEqual(0, _queue.Count);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static void InjectEvent(NotificationQueueSO queue, VoidGameEvent evt)
        {
            FieldInfo fi = typeof(NotificationQueueSO)
                .GetField("_onNotificationEnqueued",
                          BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "_onNotificationEnqueued field not found on NotificationQueueSO.");
            fi.SetValue(queue, evt);
        }
    }
}

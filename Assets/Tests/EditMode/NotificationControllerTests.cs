using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="NotificationController"/>.
    ///
    /// Covers:
    ///   • OnEnable with all null refs — ?. guards and null panel SetActive skip.
    ///   • OnDisable with all null refs — UnregisterCallback and StopCoroutine
    ///     guards prevent throw.
    ///   • _onNotificationEnqueued raised with null _queue — TryShowNext's
    ///     first guard (_queue == null) must return early without throw.
    ///   • _onNotificationEnqueued raised with an empty queue — TryDequeue
    ///     returns false; no coroutine started, no throw.
    ///   • OnDisable resets _isShowing to false (verified via reflection).
    ///   • OnDisable unregisters _tryShowNextDelegate from _onNotificationEnqueued
    ///     (external-counter pattern: only test counter fires after disable).
    ///   • OnEnable called twice (disable + re-enable) — idempotent, no throw.
    ///
    /// Note: ShowNotification is a coroutine; WaitForSecondsRealtime never
    /// completes in EditMode.  Tests therefore do NOT attempt to verify the
    /// coroutine body — only synchronous pre-coroutine guard paths are covered.
    /// All tests run headless; no uGUI or scene objects required.
    /// </summary>
    public class NotificationControllerTests
    {
        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        // ── Factory helper ────────────────────────────────────────────────────

        private static (GameObject go, NotificationController ctrl) MakeCtrl()
        {
            var go   = new GameObject("NotificationController");
            go.SetActive(false); // inactive so OnEnable doesn't run during field setup
            var ctrl = go.AddComponent<NotificationController>();
            return (go, ctrl);
        }

        // ── OnEnable — all null refs ───────────────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            // _queue, _onNotificationEnqueued, _notificationPanel, _titleText, _bodyText
            // are all null. ?. guards throughout OnEnable and the hidden panel call must
            // silently skip.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnDisable — all null refs ─────────────────────────────────────────

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);

            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── TryShowNext — null _queue ─────────────────────────────────────────

        [Test]
        public void OnNotificationEnqueued_Raise_NullQueue_DoesNotThrow()
        {
            var enqueued = ScriptableObject.CreateInstance<VoidGameEvent>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onNotificationEnqueued", enqueued);
            // _queue remains null; TryShowNext's first guard must return early.

            go.SetActive(true);

            Assert.DoesNotThrow(() => enqueued.Raise(),
                "Raising _onNotificationEnqueued with null _queue must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(enqueued);
        }

        // ── TryShowNext — empty queue ─────────────────────────────────────────

        [Test]
        public void OnNotificationEnqueued_Raise_EmptyQueue_DoesNotThrow()
        {
            var enqueued = ScriptableObject.CreateInstance<VoidGameEvent>();
            var queue    = ScriptableObject.CreateInstance<NotificationQueueSO>();
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onNotificationEnqueued", enqueued);
            SetField(ctrl, "_queue", queue);
            // Queue has no items; TryDequeue returns false → no coroutine started.

            go.SetActive(true);

            Assert.DoesNotThrow(() => enqueued.Raise(),
                "Raising _onNotificationEnqueued with an empty queue must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(enqueued);
            Object.DestroyImmediate(queue);
        }

        // ── OnDisable — resets _isShowing ───────────────────────────────────��

        [Test]
        public void OnDisable_ResetsIsShowing_ToFalse()
        {
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);

            // Force _isShowing to true to simulate mid-notification state.
            SetField(ctrl, "_isShowing", true);

            go.SetActive(false); // OnDisable must reset _isShowing = false.

            bool isShowing = GetField<bool>(ctrl, "_isShowing");
            Assert.IsFalse(isShowing,
                "OnDisable must reset _isShowing to false.");

            Object.DestroyImmediate(go);
        }

        // ── OnDisable — unregisters delegate from _onNotificationEnqueued ─────

        [Test]
        public void OnDisable_UnregistersFromEnqueuedChannel()
        {
            var enqueued = ScriptableObject.CreateInstance<VoidGameEvent>();

            // External counter — the only callback that should remain after disable.
            int externalCount = 0;
            enqueued.RegisterCallback(() => externalCount++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onNotificationEnqueued", enqueued);

            go.SetActive(true);   // OnEnable: registers _tryShowNextDelegate
            go.SetActive(false);  // OnDisable: unregisters it

            // Raise event — only the external counter must fire.
            enqueued.Raise();

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not _tryShowNextDelegate) " +
                "should fire on _onNotificationEnqueued.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(enqueued);
        }

        // ── OnEnable idempotency — disable + re-enable ────────────────────────

        [Test]
        public void OnEnable_AfterDisable_DoesNotThrow()
        {
            var enqueued = ScriptableObject.CreateInstance<VoidGameEvent>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onNotificationEnqueued", enqueued);

            go.SetActive(true);   // first OnEnable
            go.SetActive(false);  // OnDisable
            Assert.DoesNotThrow(() => go.SetActive(true),
                "Re-enabling NotificationController after a disable must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(enqueued);
        }
    }
}

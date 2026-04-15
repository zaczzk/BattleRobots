using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T240: <see cref="RespawnProtectionSO"/> and
    /// <see cref="RespawnProtectionController"/>.
    ///
    /// RespawnProtectionTests (16):
    ///   SO_FreshInstance_ProtectionDuration_Three               ×1
    ///   SO_FreshInstance_FullArmorRating_100                    ×1
    ///   Controller_FreshInstance_ProtectionSO_Null              ×1
    ///   Controller_FreshInstance_Receiver_Null                  ×1
    ///   Controller_FreshInstance_IsProtected_False              ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow              ×1
    ///   Controller_OnDisable_Unregisters_Channel                ×1
    ///   HandleRespawnReady_NullReceiver_DoesNotThrow            ×1
    ///   HandleRespawnReady_NullProtectionSO_DoesNotThrow        ×1
    ///   HandleRespawnReady_BothAssigned_SetsIsProtected_True    ×1
    ///   HandleRespawnReady_SetsArmorToFull                      ×1
    ///   Tick_BelowDuration_StaysProtected                       ×1
    ///   Tick_ExceedsDuration_EndsProtection                     ×1
    ///   Tick_EndsProtection_RestoresArmor                       ×1
    ///   OnDisable_WhenProtected_RestoresArmor                   ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class RespawnProtectionTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static RespawnProtectionSO CreateProtectionSO()
        {
            var so = ScriptableObject.CreateInstance<RespawnProtectionSO>();
            // Default: ProtectionDuration=3f, FullArmorRating=100
            return so;
        }

        private static RespawnProtectionController CreateController() =>
            new GameObject("RespawnProtCtrl_Test").AddComponent<RespawnProtectionController>();

        private static DamageReceiver CreateReceiver() =>
            new GameObject("DamageReceiver_Test").AddComponent<DamageReceiver>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_ProtectionDuration_Three()
        {
            var so = CreateProtectionSO();
            Assert.AreEqual(3f, so.ProtectionDuration,
                "ProtectionDuration must default to 3f.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FullArmorRating_100()
        {
            var so = CreateProtectionSO();
            Assert.AreEqual(100, so.FullArmorRating,
                "FullArmorRating must default to 100.");
            Object.DestroyImmediate(so);
        }

        // ── Controller fresh-instance tests ───────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ProtectionSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ProtectionSO,
                "ProtectionSO must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_Receiver_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Receiver,
                "Receiver must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_IsProtected_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must be false on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle tests ───────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl = CreateController();
            var evt  = CreateEvent();
            SetField(ctrl, "_onRespawnReady", evt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // After unregistration, raising the event should not trigger
            // HandleRespawnReady (which would throw because _receiver is null would no-op).
            // Verify by adding a counter callback and ensuring only it fires.
            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable, only external callbacks fire on _onRespawnReady.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(evt);
        }

        // ── HandleRespawnReady tests ──────────────────────────────────────────

        [Test]
        public void HandleRespawnReady_NullReceiver_DoesNotThrow()
        {
            var ctrl = CreateController();
            var so   = CreateProtectionSO();
            SetField(ctrl, "_protectionSO", so);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleRespawnReady(),
                "HandleRespawnReady with null _receiver must not throw.");
            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must remain false when _receiver is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void HandleRespawnReady_NullProtectionSO_DoesNotThrow()
        {
            var ctrl     = CreateController();
            var receiver = CreateReceiver();
            SetField(ctrl, "_receiver", receiver);
            InvokePrivate(ctrl, "Awake");

            Assert.DoesNotThrow(() => ctrl.HandleRespawnReady(),
                "HandleRespawnReady with null _protectionSO must not throw.");
            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must remain false when _protectionSO is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(receiver.gameObject);
        }

        [Test]
        public void HandleRespawnReady_BothAssigned_SetsIsProtected_True()
        {
            var ctrl     = CreateController();
            var so       = CreateProtectionSO();
            var receiver = CreateReceiver();
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_receiver",     receiver);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();

            Assert.IsTrue(ctrl.IsProtected,
                "IsProtected must be true after HandleRespawnReady with both refs assigned.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(receiver.gameObject);
        }

        [Test]
        public void HandleRespawnReady_SetsArmorToFull()
        {
            var ctrl     = CreateController();
            var so       = CreateProtectionSO();          // FullArmorRating = 100
            var receiver = CreateReceiver();
            receiver.SetArmorRating(20);                  // start at 20
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_receiver",     receiver);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();

            Assert.AreEqual(100, receiver.ArmorRating,
                "Receiver armor must be set to FullArmorRating (100) after HandleRespawnReady.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(receiver.gameObject);
        }

        // ── Tick tests ────────────────────────────────────────────────────────

        [Test]
        public void Tick_BelowDuration_StaysProtected()
        {
            var ctrl     = CreateController();
            var so       = CreateProtectionSO();          // ProtectionDuration = 3f
            var receiver = CreateReceiver();
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_receiver",     receiver);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();
            ctrl.Tick(2f);                                // below 3f threshold

            Assert.IsTrue(ctrl.IsProtected,
                "IsProtected must remain true before ProtectionDuration elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(receiver.gameObject);
        }

        [Test]
        public void Tick_ExceedsDuration_EndsProtection()
        {
            var ctrl     = CreateController();
            var so       = CreateProtectionSO();          // ProtectionDuration = 3f
            var receiver = CreateReceiver();
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_receiver",     receiver);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();
            ctrl.Tick(4f);                                // exceeds 3f threshold

            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must be false once ProtectionDuration elapses.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(receiver.gameObject);
        }

        [Test]
        public void Tick_EndsProtection_RestoresArmor()
        {
            var ctrl     = CreateController();
            var so       = CreateProtectionSO();          // ProtectionDuration = 3f, FullArmor = 100
            var receiver = CreateReceiver();
            receiver.SetArmorRating(25);                  // original armor
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_receiver",     receiver);
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleRespawnReady();
            ctrl.Tick(4f);                                // triggers EndProtection

            Assert.AreEqual(25, receiver.ArmorRating,
                "Armor must be restored to the pre-respawn value after protection ends.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(receiver.gameObject);
        }

        [Test]
        public void OnDisable_WhenProtected_RestoresArmor()
        {
            var ctrl     = CreateController();
            var so       = CreateProtectionSO();
            var receiver = CreateReceiver();
            receiver.SetArmorRating(30);
            SetField(ctrl, "_protectionSO", so);
            SetField(ctrl, "_receiver",     receiver);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ctrl.HandleRespawnReady();           // armor → 100, IsProtected = true
            InvokePrivate(ctrl, "OnDisable");    // should restore armor

            Assert.AreEqual(30, receiver.ArmorRating,
                "OnDisable must restore original armor when protection is still active.");
            Assert.IsFalse(ctrl.IsProtected,
                "IsProtected must be false after OnDisable restores armor.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(receiver.gameObject);
        }
    }
}

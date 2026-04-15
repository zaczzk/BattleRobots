using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T271:
    ///   <see cref="ZoneCaptureStreakSO"/> and
    ///   <see cref="ZoneCaptureStreakController"/>.
    ///
    /// ZoneCaptureStreakSOTests (8):
    ///   FreshInstance_CurrentStreak_Zero                                ×1
    ///   FreshInstance_StreakThreshold_Three                             ×1
    ///   FreshInstance_BonusMultiplier_Two                               ×1
    ///   FreshInstance_HasBonus_False                                    ×1
    ///   IncrementStreak_IncreasesCount                                  ×1
    ///   IncrementStreak_AtThreshold_HasBonus                            ×1
    ///   ResetStreak_ResetsToZero                                        ×1
    ///   ResetStreak_WhenZero_NoEventFired                               ×1
    ///
    /// ZoneCaptureStreakControllerTests (6):
    ///   FreshInstance_StreakSO_Null                                     ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                  ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                 ×1
    ///   OnDisable_Unregisters_AllChannels                               ×1
    ///   HandleZoneCaptured_IncreasesStreak                              ×1
    ///   HandleZoneLost_ResetsStreak                                     ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneCaptureStreakTests
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

        private static ZoneCaptureStreakSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneCaptureStreakSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneCaptureStreakController CreateController() =>
            new GameObject("ZoneCaptureStreakCtrl_Test")
                .AddComponent<ZoneCaptureStreakController>();

        // ── ZoneCaptureStreakSO tests ──────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentStreak_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.CurrentStreak,
                "A fresh ZoneCaptureStreakSO must have CurrentStreak == 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_StreakThreshold_Three()
        {
            var so = CreateSO();
            Assert.AreEqual(3, so.StreakThreshold,
                "Default StreakThreshold must be 3.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_BonusMultiplier_Two()
        {
            var so = CreateSO();
            Assert.AreEqual(2f, so.BonusMultiplier, 0.001f,
                "Default BonusMultiplier must be 2.0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_HasBonus_False()
        {
            var so = CreateSO();
            Assert.IsFalse(so.HasBonus,
                "HasBonus must be false on a fresh instance (streak 0 < threshold 3).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void IncrementStreak_IncreasesCount()
        {
            var so = CreateSO();
            so.IncrementStreak();
            Assert.AreEqual(1, so.CurrentStreak,
                "IncrementStreak must increase CurrentStreak by 1.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void IncrementStreak_AtThreshold_HasBonus()
        {
            var so = CreateSO();
            // Default threshold is 3; increment three times.
            so.IncrementStreak();
            so.IncrementStreak();
            so.IncrementStreak();
            Assert.IsTrue(so.HasBonus,
                "HasBonus must be true once CurrentStreak reaches StreakThreshold.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ResetStreak_ResetsToZero()
        {
            var so = CreateSO();
            so.IncrementStreak();
            so.IncrementStreak();
            so.ResetStreak();
            Assert.AreEqual(0, so.CurrentStreak,
                "ResetStreak must set CurrentStreak back to 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ResetStreak_WhenZero_NoEventFired()
        {
            var so  = CreateSO();
            var evt = CreateEvent();
            SetField(so, "_onStreakChanged", evt);

            int fireCount = 0;
            evt.RegisterCallback(() => fireCount++);

            // Streak is already 0 — ResetStreak must be a no-op.
            so.ResetStreak();
            Assert.AreEqual(0, fireCount,
                "ResetStreak on a zero streak must not fire _onStreakChanged.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── ZoneCaptureStreakController tests ─────────────────────────────────

        [Test]
        public void FreshInstance_StreakSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.StreakSO,
                "StreakSO must be null on a fresh ZoneCaptureStreakController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_AllChannels()
        {
            var ctrl       = CreateController();
            var streakSO   = CreateSO();
            var evtCapture = CreateEvent();

            SetField(ctrl, "_streakSO",        streakSO);
            SetField(ctrl, "_onZoneCaptured",  evtCapture);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Raising capture after disable must not increment the streak.
            Assert.AreEqual(0, streakSO.CurrentStreak,
                "Pre-condition: streak should be 0.");
            evtCapture.Raise(); // must NOT call HandleZoneCaptured
            Assert.AreEqual(0, streakSO.CurrentStreak,
                "After OnDisable, captured event must not increment the streak.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(evtCapture);
        }

        [Test]
        public void HandleZoneCaptured_IncreasesStreak()
        {
            var ctrl     = CreateController();
            var streakSO = CreateSO();
            SetField(ctrl, "_streakSO", streakSO);

            InvokePrivate(ctrl, "Awake");
            ctrl.HandleZoneCaptured();

            Assert.AreEqual(1, streakSO.CurrentStreak,
                "HandleZoneCaptured must call IncrementStreak on the SO.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
        }

        [Test]
        public void HandleZoneLost_ResetsStreak()
        {
            var ctrl     = CreateController();
            var streakSO = CreateSO();
            SetField(ctrl, "_streakSO", streakSO);

            InvokePrivate(ctrl, "Awake");
            ctrl.HandleZoneCaptured();
            ctrl.HandleZoneCaptured();
            Assert.AreEqual(2, streakSO.CurrentStreak, "Pre-condition: streak should be 2.");

            ctrl.HandleZoneLost();
            Assert.AreEqual(0, streakSO.CurrentStreak,
                "HandleZoneLost must call ResetStreak on the SO.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streakSO);
        }
    }
}

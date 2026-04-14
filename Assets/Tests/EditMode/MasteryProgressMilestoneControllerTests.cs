using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T192 — <see cref="MasteryProgressMilestoneController"/>.
    ///
    /// MasteryProgressMilestoneControllerTests (8):
    ///   Ctrl_FreshInstance_MilestoneSOIsNull                        ×1
    ///   Ctrl_FreshInstance_MasteryIsNull                            ×1
    ///   Ctrl_OnEnable_NullRefs_DoesNotThrow                         ×1
    ///   Ctrl_OnDisable_NullRefs_DoesNotThrow                        ×1
    ///   Ctrl_OnDisable_Unregisters_MasteryChannel                   ×1
    ///   Ctrl_OnDisable_Unregisters_MatchEndedChannel                ×1
    ///   Ctrl_Refresh_NullMastery_DoesNotThrow                       ×1
    ///   Ctrl_Refresh_NullUI_DoesNotThrow                            ×1
    /// </summary>
    public class MasteryProgressMilestoneControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found.");
            fi.SetValue(target, value);
        }

        private static MasteryProgressMilestoneController CreateCtrl()
        {
            var go = new GameObject("MilestoneCtrl_Test");
            return go.AddComponent<MasteryProgressMilestoneController>();
        }

        private static MasteryProgressMilestoneSO CreateMilestoneSO()
        {
            var so = ScriptableObject.CreateInstance<MasteryProgressMilestoneSO>();
            SetField(so, "_physicalMilestones", new float[] { 500f, 1000f });
            return so;
        }

        private static DamageTypeMasterySO CreateMastery() =>
            ScriptableObject.CreateInstance<DamageTypeMasterySO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── FreshInstance ─────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_MilestoneSOIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.MilestoneSO,
                "Fresh controller must have null MilestoneSO.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_MasteryIsNull()
        {
            var ctrl = CreateCtrl();
            Assert.IsNull(ctrl.Mastery,
                "Fresh controller must have null Mastery.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        // ── Lifecycle null safety ─────────────────────────────────────────────

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateCtrl();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_MasteryChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMasteryUnlocked", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onMasteryUnlocked.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters_MatchEndedChannel()
        {
            var ctrl = CreateCtrl();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count,
                "OnDisable must unregister from _onMatchEnded.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        // ── Refresh null safety ───────────────────────────────────────────────

        [Test]
        public void Ctrl_Refresh_NullMastery_DoesNotThrow()
        {
            var ctrl       = CreateCtrl();
            var milestoneSO = CreateMilestoneSO();
            SetField(ctrl, "_milestoneSO", milestoneSO);
            // _mastery is null

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null mastery must silently return.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(milestoneSO);
        }

        [Test]
        public void Ctrl_Refresh_NullUI_DoesNotThrow()
        {
            var ctrl        = CreateCtrl();
            var milestoneSO = CreateMilestoneSO();
            var mastery     = CreateMastery();
            SetField(ctrl, "_milestoneSO", milestoneSO);
            SetField(ctrl, "_mastery",     mastery);
            // No UI labels or bars assigned

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh with null UI refs must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(milestoneSO);
            Object.DestroyImmediate(mastery);
        }
    }
}

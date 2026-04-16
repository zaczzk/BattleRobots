using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T317: <see cref="ZoneControlBonusChestSO"/> and
    /// <see cref="ZoneControlBonusChestController"/>.
    ///
    /// ZoneControlBonusChestTests (12):
    ///   SO_FreshInstance_TotalChests_Zero                                         ×1
    ///   SO_CheckChest_FiresAtInterval                                             ×1
    ///   SO_CheckChest_DoesNotFireBelowInterval                                    ×1
    ///   SO_CheckChest_NonPositiveCaptures_Ignored                                 ×1
    ///   SO_CheckChest_MultipleIntervalsCrossed                                    ×1
    ///   SO_Reset_ClearsState                                                      ×1
    ///   Controller_FreshInstance_ChestSO_Null                                     ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandlePlayerCaptured_IncrementsCount                           ×1
    ///   Controller_Refresh_NullSO_HidesPanel                                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlBonusChestTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlBonusChestSO CreateChestSO(int interval = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlBonusChestSO>();
            SetField(so, "_captureInterval", interval);
            return so;
        }

        private static ZoneControlBonusChestController CreateController() =>
            new GameObject("BonusChestCtrl_Test")
                .AddComponent<ZoneControlBonusChestController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalChests_Zero()
        {
            var so = CreateChestSO();
            Assert.AreEqual(0, so.TotalChests,
                "TotalChests must be 0 on a fresh instance.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CheckChest_FiresAtInterval()
        {
            var so = CreateChestSO(interval: 5);

            int fired = 0;
            var onChest = CreateEvent();
            SetField(so, "_onChestSpawned", onChest);
            onChest.RegisterCallback(() => fired++);

            so.CheckChest(5); // First milestone.
            Assert.AreEqual(1, fired,
                "_onChestSpawned must fire once when captures == captureInterval.");
            Assert.AreEqual(1, so.TotalChests,
                "TotalChests must be 1 after the first milestone.");

            UnityEngine.Object.DestroyImmediate(so);
            UnityEngine.Object.DestroyImmediate(onChest);
        }

        [Test]
        public void SO_CheckChest_DoesNotFireBelowInterval()
        {
            var so = CreateChestSO(interval: 5);

            int fired = 0;
            var onChest = CreateEvent();
            SetField(so, "_onChestSpawned", onChest);
            onChest.RegisterCallback(() => fired++);

            so.CheckChest(4); // Below first milestone.
            Assert.AreEqual(0, fired,
                "_onChestSpawned must not fire when captures < captureInterval.");

            UnityEngine.Object.DestroyImmediate(so);
            UnityEngine.Object.DestroyImmediate(onChest);
        }

        [Test]
        public void SO_CheckChest_NonPositiveCaptures_Ignored()
        {
            var so = CreateChestSO(interval: 5);
            so.CheckChest(0);
            so.CheckChest(-3);
            Assert.AreEqual(0, so.TotalChests,
                "CheckChest must ignore non-positive capture values.");
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CheckChest_MultipleIntervalsCrossed()
        {
            var so = CreateChestSO(interval: 5);

            int fired = 0;
            var onChest = CreateEvent();
            SetField(so, "_onChestSpawned", onChest);
            onChest.RegisterCallback(() => fired++);

            // Jump straight to 15 captures — crosses milestones at 5, 10, 15.
            so.CheckChest(15);
            Assert.AreEqual(3, fired,
                "CheckChest must fire once per milestone crossed in a single call.");
            Assert.AreEqual(3, so.TotalChests,
                "TotalChests must equal the number of milestones crossed.");

            UnityEngine.Object.DestroyImmediate(so);
            UnityEngine.Object.DestroyImmediate(onChest);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateChestSO(interval: 5);
            so.CheckChest(10);
            Assert.AreEqual(2, so.TotalChests);

            so.Reset();
            Assert.AreEqual(0, so.TotalChests,
                "TotalChests must be 0 after Reset.");

            // After Reset the next CheckChest should re-trigger at the first milestone.
            int fired = 0;
            var onChest = CreateEvent();
            SetField(so, "_onChestSpawned", onChest);
            onChest.RegisterCallback(() => fired++);
            so.CheckChest(5);
            Assert.AreEqual(1, fired,
                "CheckChest must fire again for the first milestone after Reset.");

            UnityEngine.Object.DestroyImmediate(so);
            UnityEngine.Object.DestroyImmediate(onChest);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ChestSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ChestSO,
                "ChestSO must be null on a freshly added controller.");
            UnityEngine.Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlBonusChestController>(),
                "Adding controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlBonusChestController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlBonusChestController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onPlayerCaptured", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onPlayerCaptured must be unregistered after OnDisable.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandlePlayerCaptured_IncrementsCount()
        {
            var go   = new GameObject("Test_Capture");
            var ctrl = go.AddComponent<ZoneControlBonusChestController>();
            var so   = CreateChestSO(interval: 5);
            SetField(ctrl, "_chestSO", so);

            ctrl.HandlePlayerCaptured();
            ctrl.HandlePlayerCaptured();

            Assert.AreEqual(2, ctrl.CaptureCount,
                "CaptureCount must reflect the number of HandlePlayerCaptured calls.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlBonusChestController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when ChestSO is null.");

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(panel);
        }
    }
}

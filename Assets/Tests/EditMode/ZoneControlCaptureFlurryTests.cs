using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFlurryTests
    {
        private static ZoneControlCaptureFlurrySO CreateSO(int target = 4, float window = 10f, int bonus = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFlurrySO>();
            typeof(ZoneControlCaptureFlurrySO)
                .GetField("_flurryTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, target);
            typeof(ZoneControlCaptureFlurrySO)
                .GetField("_flurryWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, window);
            typeof(ZoneControlCaptureFlurrySO)
                .GetField("_bonusPerFlurry", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFlurryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFlurryController>();
        }

        [Test]
        public void SO_FreshInstance_FlurryCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FlurryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_WithinWindow_IncrementsCount()
        {
            var so = CreateSO(target: 4, window: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            Assert.That(so.CaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_OutsideWindow_PrunesOldEntries()
        {
            var so = CreateSO(target: 4, window: 5f);
            so.RecordCapture(0f);
            so.RecordCapture(10f);
            Assert.That(so.CaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FlurryFires_WhenTargetReached()
        {
            var so = CreateSO(target: 3, window: 30f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(so.FlurryCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Flurry_ClearsTimestamps_AfterFiring()
        {
            var so = CreateSO(target: 3, window: 30f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFlurries_AccumulatesCount()
        {
            var so = CreateSO(target: 2, window: 30f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            so.RecordCapture(3f);
            Assert.That(so.FlurryCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Flurry_AccumulatesBonus()
        {
            var so = CreateSO(target: 2, window: 30f, bonus: 100);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(target: 2, window: 30f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.Reset();
            Assert.That(so.FlurryCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.CaptureCount,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FlurrySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FlurrySO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureFlurryController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();
            typeof(ZoneControlCaptureFlurryController)
                .GetField("_flurrySO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureFlurryController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(false);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}

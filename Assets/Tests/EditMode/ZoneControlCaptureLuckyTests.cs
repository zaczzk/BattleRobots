using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLuckyTests
    {
        private static ZoneControlCaptureLuckySO CreateSO(
            float interval = 10f, float tolerance = 0.5f, int bonus = 250)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLuckySO>();
            typeof(ZoneControlCaptureLuckySO)
                .GetField("_intervalSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, interval);
            typeof(ZoneControlCaptureLuckySO)
                .GetField("_tolerance",       BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tolerance);
            typeof(ZoneControlCaptureLuckySO)
                .GetField("_bonusPerLucky",   BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLuckyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLuckyController>();
        }

        [Test]
        public void SO_FreshInstance_LuckyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LuckyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureAtIntervalStart_IsLucky()
        {
            var so = CreateSO(interval: 10f, tolerance: 0.5f);
            so.RecordCapture(0f);
            Assert.That(so.LuckyCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureAtIntervalBoundary_IsLucky()
        {
            var so = CreateSO(interval: 10f, tolerance: 0.5f);
            so.RecordCapture(10f);
            Assert.That(so.LuckyCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureWithinTolerance_IsLucky()
        {
            var so = CreateSO(interval: 10f, tolerance: 0.5f);
            so.RecordCapture(0.3f);
            Assert.That(so.LuckyCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureOutsideTolerance_NotLucky()
        {
            var so = CreateSO(interval: 10f, tolerance: 0.5f);
            so.RecordCapture(4f);
            Assert.That(so.LuckyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLuckyCaptures_AccumulatesBonus()
        {
            var so = CreateSO(interval: 10f, tolerance: 0.5f, bonus: 250);
            so.RecordCapture(0f);
            so.RecordCapture(10f);
            Assert.That(so.LuckyCount,      Is.EqualTo(2));
            Assert.That(so.TotalLuckyBonus, Is.EqualTo(500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO();
            so.RecordCapture(0f);
            so.Reset();
            Assert.That(so.LuckyCount,      Is.EqualTo(0));
            Assert.That(so.TotalLuckyBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CaptureNearEndOfInterval_IsLucky()
        {
            var so = CreateSO(interval: 10f, tolerance: 0.5f);
            so.RecordCapture(9.7f);
            Assert.That(so.LuckyCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LuckySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LuckySO, Is.Null);
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
            typeof(ZoneControlCaptureLuckyController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

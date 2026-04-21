using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCompassTests
    {
        private static ZoneControlCaptureCompassSO CreateSO(
            int bearingsNeeded     = 5,
            int lostPerBot         = 1,
            int bonusPerNavigation = 745)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCompassSO>();
            typeof(ZoneControlCaptureCompassSO)
                .GetField("_bearingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bearingsNeeded);
            typeof(ZoneControlCaptureCompassSO)
                .GetField("_lostPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lostPerBot);
            typeof(ZoneControlCaptureCompassSO)
                .GetField("_bonusPerNavigation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerNavigation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCompassController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCompassController>();
        }

        [Test]
        public void SO_FreshInstance_Bearings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bearings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_NavigationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NavigationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBearings()
        {
            var so = CreateSO(bearingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Bearings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_NavigatesAtThreshold()
        {
            var so    = CreateSO(bearingsNeeded: 3, bonusPerNavigation: 745);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(745));
            Assert.That(so.NavigationCount,  Is.EqualTo(1));
            Assert.That(so.Bearings,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bearingsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsBearings()
        {
            var so = CreateSO(bearingsNeeded: 5, lostPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bearings, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bearingsNeeded: 5, lostPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bearings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BearingProgress_Clamped()
        {
            var so = CreateSO(bearingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BearingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCompassNavigated_FiresEvent()
        {
            var so    = CreateSO(bearingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCompassSO)
                .GetField("_onCompassNavigated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bearingsNeeded: 2, bonusPerNavigation: 745);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bearings,          Is.EqualTo(0));
            Assert.That(so.NavigationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleNavigations_Accumulate()
        {
            var so = CreateSO(bearingsNeeded: 2, bonusPerNavigation: 745);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.NavigationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1490));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CompassSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CompassSO, Is.Null);
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
            typeof(ZoneControlCaptureCompassController)
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

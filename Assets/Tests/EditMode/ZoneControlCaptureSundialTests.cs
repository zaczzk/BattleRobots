using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSundialTests
    {
        private static ZoneControlCaptureSundialSO CreateSO(
            int hoursNeeded  = 6,
            int shadowPerBot = 2,
            int bonusPerNoon = 475)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSundialSO>();
            typeof(ZoneControlCaptureSundialSO)
                .GetField("_hoursNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, hoursNeeded);
            typeof(ZoneControlCaptureSundialSO)
                .GetField("_shadowPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, shadowPerBot);
            typeof(ZoneControlCaptureSundialSO)
                .GetField("_bonusPerNoon", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerNoon);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSundialController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSundialController>();
        }

        [Test]
        public void SO_FreshInstance_Hours_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Hours, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_NoonCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NoonCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesHours()
        {
            var so = CreateSO(hoursNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Hours, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesNoonAtThreshold()
        {
            var so    = CreateSO(hoursNeeded: 3, bonusPerNoon: 475);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(475));
            Assert.That(so.NoonCount, Is.EqualTo(1));
            Assert.That(so.Hours,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(hoursNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CastsHourShadow()
        {
            var so = CreateSO(hoursNeeded: 6, shadowPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Hours, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(hoursNeeded: 6, shadowPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Hours, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HourProgress_Clamped()
        {
            var so = CreateSO(hoursNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.HourProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnNoonReached_FiresEvent()
        {
            var so    = CreateSO(hoursNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSundialSO)
                .GetField("_onNoonReached", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(hoursNeeded: 2, bonusPerNoon: 475);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Hours,             Is.EqualTo(0));
            Assert.That(so.NoonCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleNoons_Accumulate()
        {
            var so = CreateSO(hoursNeeded: 2, bonusPerNoon: 475);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.NoonCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(950));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SundialSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SundialSO, Is.Null);
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
            typeof(ZoneControlCaptureSundialController)
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

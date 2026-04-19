using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePrismaticTests
    {
        private static ZoneControlCapturePrismaticSO CreateSO(int pulseEvery = 8, int bonus = 225)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePrismaticSO>();
            typeof(ZoneControlCapturePrismaticSO)
                .GetField("_pulseEveryN", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pulseEvery);
            typeof(ZoneControlCapturePrismaticSO)
                .GetField("_bonusPerPulse", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePrismaticController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePrismaticController>();
        }

        [Test]
        public void SO_FreshInstance_TotalCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PulseCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PulseCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowPulseThreshold_NoPulse()
        {
            var so = CreateSO(pulseEvery: 5);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.PulseCount,     Is.EqualTo(0));
            Assert.That(so.TotalCaptures,  Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_IncrementsPulseCount()
        {
            var so = CreateSO(pulseEvery: 3);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.PulseCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_FiresEvent()
        {
            var so    = CreateSO(pulseEvery: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePrismaticSO)
                .GetField("_onPrismaticPulse", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordCapture_AccumulatesBonus()
        {
            var so = CreateSO(pulseEvery: 2, bonus: 100);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_MultiplePulses_AccumulatesMultiple()
        {
            var so = CreateSO(pulseEvery: 2, bonus: 50);
            for (int i = 0; i < 6; i++) so.RecordCapture();
            Assert.That(so.PulseCount,        Is.EqualTo(3));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PulseProgress_Computed()
        {
            var so = CreateSO(pulseEvery: 4);
            Assert.That(so.PulseProgress, Is.EqualTo(0f).Within(0.001f));
            so.RecordCapture();
            Assert.That(so.PulseProgress, Is.EqualTo(0.25f).Within(0.001f));
            so.RecordCapture();
            Assert.That(so.PulseProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CapturesUntilPulse_Computed()
        {
            var so = CreateSO(pulseEvery: 5);
            Assert.That(so.CapturesUntilPulse, Is.EqualTo(5));
            so.RecordCapture();
            Assert.That(so.CapturesUntilPulse, Is.EqualTo(4));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(pulseEvery: 2, bonus: 100);
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.TotalCaptures,     Is.EqualTo(0));
            Assert.That(so.PulseCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PrismaticSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PrismaticSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCapturePrismaticController)
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

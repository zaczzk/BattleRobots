using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCycloneTests
    {
        private static ZoneControlCaptureCycloneSO CreateSO(int activatesEvery = 5, int bonus = 200, float duration = 8f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCycloneSO>();
            typeof(ZoneControlCaptureCycloneSO)
                .GetField("_activatesEveryN", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, activatesEvery);
            typeof(ZoneControlCaptureCycloneSO)
                .GetField("_cycloneBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            typeof(ZoneControlCaptureCycloneSO)
                .GetField("_cycloneDurationSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, duration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCycloneController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCycloneController>();
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CycloneCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CycloneCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowThreshold_DoesNotActivate()
        {
            var so = CreateSO(activatesEvery: 4);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.IsActive, Is.False);
            Assert.That(so.TotalCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_ActivatesCyclone()
        {
            var so = CreateSO(activatesEvery: 3);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.IsActive, Is.True);
            Assert.That(so.CycloneCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_FiresOpenedEvent()
        {
            var so    = CreateSO(activatesEvery: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCycloneSO)
                .GetField("_onCycloneOpened", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_GetCaptureBonus_WhenActive_ReturnsBonus()
        {
            var so = CreateSO(activatesEvery: 2, bonus: 150);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.IsActive, Is.True);
            Assert.That(so.GetCaptureBonus(), Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetCaptureBonus_WhenInactive_ReturnsZero()
        {
            var so = CreateSO(activatesEvery: 5, bonus: 200);
            Assert.That(so.GetCaptureBonus(), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExpiresCyclone_AfterDuration()
        {
            var so = CreateSO(activatesEvery: 2, duration: 5f);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.IsActive, Is.True);
            so.Tick(6f);
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhenInactive_DoesNotThrow()
        {
            var so = CreateSO();
            Assert.DoesNotThrow(() => so.Tick(1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(activatesEvery: 2);
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.TotalCaptures,  Is.EqualTo(0));
            Assert.That(so.IsActive,       Is.False);
            Assert.That(so.CycloneCount,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CycloneSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CycloneSO, Is.Null);
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
            typeof(ZoneControlCaptureCycloneController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNovaTests
    {
        private static ZoneControlCaptureNovaSO CreateSO(float threshold = 5f, float decayRate = 0.5f, int bonus = 250)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNovaSO>();
            typeof(ZoneControlCaptureNovaSO)
                .GetField("_novaThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            typeof(ZoneControlCaptureNovaSO)
                .GetField("_decayRate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decayRate);
            typeof(ZoneControlCaptureNovaSO)
                .GetField("_bonusPerNova", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNovaController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNovaController>();
        }

        [Test]
        public void SO_FreshInstance_Pool_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NovaPool, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowThreshold_NoNova()
        {
            var so = CreateSO(threshold: 5f);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.NovaCount, Is.EqualTo(0));
            Assert.That(so.NovaPool,  Is.EqualTo(2f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_FiresNova()
        {
            var so    = CreateSO(threshold: 3f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNovaSO)
                .GetField("_onNova", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(fired,        Is.EqualTo(1));
            Assert.That(so.NovaCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_ResetsPool()
        {
            var so = CreateSO(threshold: 3f);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.NovaPool, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_AccumulatesBonus()
        {
            var so = CreateSO(threshold: 2f, bonus: 100);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DecaysPool()
        {
            var so = CreateSO(threshold: 10f, decayRate: 1f);
            so.RecordCapture();
            so.RecordCapture();
            so.Tick(1f);
            Assert.That(so.NovaPool, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DoesNotGoBelowZero()
        {
            var so = CreateSO(threshold: 10f, decayRate: 5f);
            so.RecordCapture();
            so.Tick(10f);
            Assert.That(so.NovaPool, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(threshold: 2f, bonus: 100);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.NovaPool,          Is.EqualTo(0f));
            Assert.That(so.NovaCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NovaSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NovaSO, Is.Null);
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
            typeof(ZoneControlCaptureNovaController)
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
            typeof(ZoneControlCaptureNovaController)
                .GetField("_novaSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureNovaController)
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

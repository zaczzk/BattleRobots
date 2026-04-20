using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEmberTests
    {
        private static ZoneControlCaptureEmberSO CreateSO(
            float heatPerCapture = 25f, float igniteThreshold = 100f,
            float coolingAmount  = 30f, int   bonusPerIgnite  = 350)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEmberSO>();
            typeof(ZoneControlCaptureEmberSO)
                .GetField("_heatPerCapture",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, heatPerCapture);
            typeof(ZoneControlCaptureEmberSO)
                .GetField("_igniteThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, igniteThreshold);
            typeof(ZoneControlCaptureEmberSO)
                .GetField("_coolingAmount",   BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coolingAmount);
            typeof(ZoneControlCaptureEmberSO)
                .GetField("_bonusPerIgnite",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIgnite);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEmberController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEmberController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentHeat_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentHeat, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IgniteCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IgniteCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(heatPerCapture: 25f, igniteThreshold: 100f);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_AccumulatesHeat()
        {
            var so = CreateSO(heatPerCapture: 25f, igniteThreshold: 100f);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentHeat, Is.EqualTo(25f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesThreshold_Ignites()
        {
            var so = CreateSO(heatPerCapture: 50f, igniteThreshold: 100f, bonusPerIgnite: 200);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(200));
            Assert.That(so.IgniteCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AfterIgnite_ResetsHeat()
        {
            var so = CreateSO(heatPerCapture: 100f, igniteThreshold: 100f);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentHeat, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Ignite_FiresEvent()
        {
            var so    = CreateSO(heatPerCapture: 100f, igniteThreshold: 100f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEmberSO)
                .GetField("_onIgnite", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_CoolsHeat()
        {
            var so = CreateSO(heatPerCapture: 50f, igniteThreshold: 100f, coolingAmount: 30f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentHeat, Is.EqualTo(20f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampedToZero()
        {
            var so = CreateSO(heatPerCapture: 10f, igniteThreshold: 100f, coolingAmount: 50f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentHeat, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EmberProgress_ReflectsHeatRatio()
        {
            var so = CreateSO(heatPerCapture: 50f, igniteThreshold: 100f);
            so.RecordPlayerCapture();
            Assert.That(so.EmberProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(heatPerCapture: 100f, igniteThreshold: 100f, bonusPerIgnite: 350);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentHeat,       Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.IgniteCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EmberSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EmberSO, Is.Null);
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
            typeof(ZoneControlCaptureEmberController)
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

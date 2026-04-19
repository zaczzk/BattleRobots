using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAvalancheTests
    {
        private static ZoneControlCaptureAvalancheSO CreateSO(
            int   baseBonus      = 50,
            float multiplierStep = 0.5f,
            float maxMultiplier  = 4f,
            float windowSeconds  = 8f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAvalancheSO>();
            typeof(ZoneControlCaptureAvalancheSO)
                .GetField("_baseBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, baseBonus);
            typeof(ZoneControlCaptureAvalancheSO)
                .GetField("_multiplierStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, multiplierStep);
            typeof(ZoneControlCaptureAvalancheSO)
                .GetField("_maxMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxMultiplier);
            typeof(ZoneControlCaptureAvalancheSO)
                .GetField("_windowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, windowSeconds);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAvalancheController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAvalancheController>();
        }

        [Test]
        public void SO_FreshInstance_Multiplier_One()
        {
            var so = CreateSO();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_ReturnsBaseBonus()
        {
            var so     = CreateSO(baseBonus: 50);
            int result = so.RecordCapture(0f);
            Assert.That(result, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SequentialCaptures_MultiplierIncreases()
        {
            var so = CreateSO(baseBonus: 50, multiplierStep: 0.5f, windowSeconds: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Multiplier_CappedAtMax()
        {
            var so = CreateSO(multiplierStep: 2f, maxMultiplier: 3f, windowSeconds: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(2f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(3f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OutOfWindow_MultiplierResets()
        {
            var so = CreateSO(windowSeconds: 5f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.RecordCapture(20f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_InWindow_AvalancheCountIncremented()
        {
            var so = CreateSO(windowSeconds: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            Assert.That(so.AvalancheCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_InWindow_FiresEvent()
        {
            var so    = CreateSO(windowSeconds: 10f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAvalancheSO)
                .GetField("_onAvalanche", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture(0f);
            so.RecordCapture(3f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(windowSeconds: 10f);
            so.RecordCapture(0f);
            so.RecordCapture(1f);
            so.Reset();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Assert.That(so.AvalancheCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AvalancheSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AvalancheSO, Is.Null);
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
            typeof(ZoneControlCaptureAvalancheController)
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
            typeof(ZoneControlCaptureAvalancheController)
                .GetField("_avalancheSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureAvalancheController)
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

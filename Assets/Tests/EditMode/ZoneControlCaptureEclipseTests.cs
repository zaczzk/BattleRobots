using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEclipseTests
    {
        private static ZoneControlCaptureEclipseSO CreateSO(int margin = 3, int bonus = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEclipseSO>();
            typeof(ZoneControlCaptureEclipseSO)
                .GetField("_eclipseMargin", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, margin);
            typeof(ZoneControlCaptureEclipseSO)
                .GetField("_bonusPerEclipseCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEclipseController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEclipseController>();
        }

        [Test]
        public void SO_FreshInstance_NotEclipsed()
        {
            var so = CreateSO();
            Assert.That(so.IsEclipsed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotLeadsBelowMargin_NoEclipse()
        {
            var so = CreateSO(margin: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsEclipsed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotLeadsAtMargin_StartsEclipse()
        {
            var so    = CreateSO(margin: 3);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEclipseSO)
                .GetField("_onEclipseStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsEclipsed, Is.True);
            Assert.That(fired,         Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_PlayerCapturesDuringEclipse_AccumulateBonus()
        {
            var so = CreateSO(margin: 2, bonus: 100);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.EclipseCaptures,   Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCatchesUp_EndsEclipse()
        {
            var so    = CreateSO(margin: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEclipseSO)
                .GetField("_onEclipseEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsEclipsed, Is.False);
            Assert.That(fired,         Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_PlayerCapturesOutsideEclipse_NoBonus()
        {
            var so = CreateSO(margin: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.EclipseCaptures,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(margin: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsEclipsed,        Is.False);
            Assert.That(so.EclipseCaptures,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.PlayerCaptures,    Is.EqualTo(0));
            Assert.That(so.BotCaptures,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EclipseSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EclipseSO, Is.Null);
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
            typeof(ZoneControlCaptureEclipseController)
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
            typeof(ZoneControlCaptureEclipseController)
                .GetField("_eclipseSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureEclipseController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureScrollTests
    {
        private static ZoneControlCaptureScrollSO CreateSO(
            int scrollsNeeded  = 4,
            int lostPerBot     = 1,
            int bonusPerCodex  = 550)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureScrollSO>();
            typeof(ZoneControlCaptureScrollSO)
                .GetField("_scrollsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, scrollsNeeded);
            typeof(ZoneControlCaptureScrollSO)
                .GetField("_lostPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lostPerBot);
            typeof(ZoneControlCaptureScrollSO)
                .GetField("_bonusPerCodex", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCodex);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureScrollController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureScrollController>();
        }

        [Test]
        public void SO_FreshInstance_Scrolls_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Scrolls, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CodexCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CodexCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesScrolls()
        {
            var so = CreateSO(scrollsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Scrolls, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesCodexAtThreshold()
        {
            var so    = CreateSO(scrollsNeeded: 3, bonusPerCodex: 550);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(550));
            Assert.That(so.CodexCount, Is.EqualTo(1));
            Assert.That(so.Scrolls,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(scrollsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_LosesScrolls()
        {
            var so = CreateSO(scrollsNeeded: 4, lostPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Scrolls, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(scrollsNeeded: 4, lostPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Scrolls, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ScrollProgress_Clamped()
        {
            var so = CreateSO(scrollsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.ScrollProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCodexComplete_FiresEvent()
        {
            var so    = CreateSO(scrollsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureScrollSO)
                .GetField("_onCodexComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(scrollsNeeded: 2, bonusPerCodex: 550);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Scrolls,           Is.EqualTo(0));
            Assert.That(so.CodexCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCodexes_Accumulate()
        {
            var so = CreateSO(scrollsNeeded: 2, bonusPerCodex: 550);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CodexCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ScrollSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ScrollSO, Is.Null);
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
            typeof(ZoneControlCaptureScrollController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpectrumObjectTests
    {
        private static ZoneControlCaptureSpectrumObjectSO CreateSO(
            int loopsNeeded   = 7,
            int breakPerBot   = 2,
            int bonusPerDeloop = 3655)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpectrumObjectSO>();
            typeof(ZoneControlCaptureSpectrumObjectSO)
                .GetField("_loopsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, loopsNeeded);
            typeof(ZoneControlCaptureSpectrumObjectSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureSpectrumObjectSO)
                .GetField("_bonusPerDeloop", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDeloop);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpectrumObjectController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpectrumObjectController>();
        }

        [Test]
        public void SO_FreshInstance_Loops_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DeloopCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DeloopCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLoops()
        {
            var so = CreateSO(loopsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(loopsNeeded: 3, bonusPerDeloop: 3655);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3655));
            Assert.That(so.DeloopCount,  Is.EqualTo(1));
            Assert.That(so.Loops,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(loopsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BreaksLoops()
        {
            var so = CreateSO(loopsNeeded: 7, breakPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(loopsNeeded: 7, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoopProgress_Clamped()
        {
            var so = CreateSO(loopsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.LoopProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpectrumObjectDelooped_FiresEvent()
        {
            var so    = CreateSO(loopsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpectrumObjectSO)
                .GetField("_onSpectrumObjectDelooped", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(loopsNeeded: 2, bonusPerDeloop: 3655);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Loops,             Is.EqualTo(0));
            Assert.That(so.DeloopCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDeloopings_Accumulate()
        {
            var so = CreateSO(loopsNeeded: 2, bonusPerDeloop: 3655);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DeloopCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7310));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpectrumObjectSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpectrumObjectSO, Is.Null);
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
            typeof(ZoneControlCaptureSpectrumObjectController)
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

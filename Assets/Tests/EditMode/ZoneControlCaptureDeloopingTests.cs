using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDeloopingTests
    {
        private static ZoneControlCaptureDeloopingSO CreateSO(
            int loopsNeeded      = 5,
            int trivializePerBot = 1,
            int bonusPerDeloop   = 3595)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDeloopingSO>();
            typeof(ZoneControlCaptureDeloopingSO)
                .GetField("_loopsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, loopsNeeded);
            typeof(ZoneControlCaptureDeloopingSO)
                .GetField("_trivializePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, trivializePerBot);
            typeof(ZoneControlCaptureDeloopingSO)
                .GetField("_bonusPerDeloop", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDeloop);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDeloopingController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDeloopingController>();
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
            var so = CreateSO(loopsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(loopsNeeded: 3, bonusPerDeloop: 3595);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3595));
            Assert.That(so.DeloopCount, Is.EqualTo(1));
            Assert.That(so.Loops,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(loopsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_TrivialesLoops()
        {
            var so = CreateSO(loopsNeeded: 5, trivializePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(loopsNeeded: 5, trivializePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoopProgress_Clamped()
        {
            var so = CreateSO(loopsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.LoopProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDeloopingComplete_FiresEvent()
        {
            var so    = CreateSO(loopsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDeloopingSO)
                .GetField("_onDeloopingComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(loopsNeeded: 2, bonusPerDeloop: 3595);
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
            var so = CreateSO(loopsNeeded: 2, bonusPerDeloop: 3595);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DeloopCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7190));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DeloopingSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DeloopingSO, Is.Null);
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
            typeof(ZoneControlCaptureDeloopingController)
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

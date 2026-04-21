using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLatheTests
    {
        private static ZoneControlCaptureLatheSO CreateSO(
            int turningsNeeded = 5,
            int shavingsPerBot = 1,
            int bonusPerSpin   = 1045)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLatheSO>();
            typeof(ZoneControlCaptureLatheSO)
                .GetField("_turningsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, turningsNeeded);
            typeof(ZoneControlCaptureLatheSO)
                .GetField("_shavingsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, shavingsPerBot);
            typeof(ZoneControlCaptureLatheSO)
                .GetField("_bonusPerSpin", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSpin);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLatheController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLatheController>();
        }

        [Test]
        public void SO_FreshInstance_Turnings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Turnings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SpinCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SpinCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTurnings()
        {
            var so = CreateSO(turningsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Turnings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TurningsAtThreshold()
        {
            var so    = CreateSO(turningsNeeded: 3, bonusPerSpin: 1045);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1045));
            Assert.That(so.SpinCount,    Is.EqualTo(1));
            Assert.That(so.Turnings,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(turningsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesTurnings()
        {
            var so = CreateSO(turningsNeeded: 5, shavingsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Turnings, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(turningsNeeded: 5, shavingsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Turnings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TurningProgress_Clamped()
        {
            var so = CreateSO(turningsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.TurningProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLatheSpun_FiresEvent()
        {
            var so    = CreateSO(turningsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLatheSO)
                .GetField("_onLatheSpun", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(turningsNeeded: 2, bonusPerSpin: 1045);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Turnings,          Is.EqualTo(0));
            Assert.That(so.SpinCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSpins_Accumulate()
        {
            var so = CreateSO(turningsNeeded: 2, bonusPerSpin: 1045);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SpinCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2090));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LatheSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LatheSO, Is.Null);
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
            typeof(ZoneControlCaptureLatheController)
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

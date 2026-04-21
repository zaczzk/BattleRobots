using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureWinchTests
    {
        private static ZoneControlCaptureWinchSO CreateSO(
            int cranksNeeded = 6,
            int slackPerBot  = 2,
            int bonusPerHaul = 1120)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureWinchSO>();
            typeof(ZoneControlCaptureWinchSO)
                .GetField("_cranksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cranksNeeded);
            typeof(ZoneControlCaptureWinchSO)
                .GetField("_slackPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slackPerBot);
            typeof(ZoneControlCaptureWinchSO)
                .GetField("_bonusPerHaul", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerHaul);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureWinchController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureWinchController>();
        }

        [Test]
        public void SO_FreshInstance_Cranks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cranks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HaulCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HaulCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCranks()
        {
            var so = CreateSO(cranksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Cranks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CranksAtThreshold()
        {
            var so    = CreateSO(cranksNeeded: 3, bonusPerHaul: 1120);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1120));
            Assert.That(so.HaulCount,   Is.EqualTo(1));
            Assert.That(so.Cranks,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cranksNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCranks()
        {
            var so = CreateSO(cranksNeeded: 6, slackPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cranks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cranksNeeded: 6, slackPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cranks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CrankProgress_Clamped()
        {
            var so = CreateSO(cranksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.CrankProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnWinchHauled_FiresEvent()
        {
            var so    = CreateSO(cranksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureWinchSO)
                .GetField("_onWinchHauled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cranksNeeded: 2, bonusPerHaul: 1120);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cranks,            Is.EqualTo(0));
            Assert.That(so.HaulCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleHauls_Accumulate()
        {
            var so = CreateSO(cranksNeeded: 2, bonusPerHaul: 1120);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.HaulCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2240));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_WinchSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.WinchSO, Is.Null);
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
            typeof(ZoneControlCaptureWinchController)
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

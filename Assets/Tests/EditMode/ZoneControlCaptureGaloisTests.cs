using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGaloisTests
    {
        private static ZoneControlCaptureGaloisSO CreateSO(
            int closuresNeeded  = 5,
            int breakPerBot     = 1,
            int bonusPerConnect = 3370)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGaloisSO>();
            typeof(ZoneControlCaptureGaloisSO)
                .GetField("_closuresNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, closuresNeeded);
            typeof(ZoneControlCaptureGaloisSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureGaloisSO)
                .GetField("_bonusPerConnect", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConnect);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGaloisController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGaloisController>();
        }

        [Test]
        public void SO_FreshInstance_Closures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Closures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConnectionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConnectionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesClosures()
        {
            var so = CreateSO(closuresNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Closures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(closuresNeeded: 3, bonusPerConnect: 3370);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(3370));
            Assert.That(so.ConnectionCount,  Is.EqualTo(1));
            Assert.That(so.Closures,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(closuresNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesClosures()
        {
            var so = CreateSO(closuresNeeded: 5, breakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Closures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(closuresNeeded: 5, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Closures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClosureProgress_Clamped()
        {
            var so = CreateSO(closuresNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ClosureProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGaloisConnected_FiresEvent()
        {
            var so    = CreateSO(closuresNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGaloisSO)
                .GetField("_onGaloisConnected", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(closuresNeeded: 2, bonusPerConnect: 3370);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Closures,          Is.EqualTo(0));
            Assert.That(so.ConnectionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConnections_Accumulate()
        {
            var so = CreateSO(closuresNeeded: 2, bonusPerConnect: 3370);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConnectionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6740));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GaloisSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GaloisSO, Is.Null);
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
            typeof(ZoneControlCaptureGaloisController)
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

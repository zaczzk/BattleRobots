using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGobletTests
    {
        private static ZoneControlCaptureGobletSO CreateSO(
            int poursNeeded    = 4,
            int spiltPerBot    = 1,
            int bonusPerGoblet = 790)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGobletSO>();
            typeof(ZoneControlCaptureGobletSO)
                .GetField("_poursNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, poursNeeded);
            typeof(ZoneControlCaptureGobletSO)
                .GetField("_spiltPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, spiltPerBot);
            typeof(ZoneControlCaptureGobletSO)
                .GetField("_bonusPerGoblet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGoblet);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGobletController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGobletController>();
        }

        [Test]
        public void SO_FreshInstance_Pours_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Pours, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GobletCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GobletCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPours()
        {
            var so = CreateSO(poursNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Pours, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsAtThreshold()
        {
            var so    = CreateSO(poursNeeded: 3, bonusPerGoblet: 790);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(790));
            Assert.That(so.GobletCount,  Is.EqualTo(1));
            Assert.That(so.Pours,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(poursNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SpiltsPours()
        {
            var so = CreateSO(poursNeeded: 4, spiltPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pours, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(poursNeeded: 4, spiltPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pours, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PourProgress_Clamped()
        {
            var so = CreateSO(poursNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.PourProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGobletFilled_FiresEvent()
        {
            var so    = CreateSO(poursNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGobletSO)
                .GetField("_onGobletFilled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(poursNeeded: 2, bonusPerGoblet: 790);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Pours,             Is.EqualTo(0));
            Assert.That(so.GobletCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGoblets_Accumulate()
        {
            var so = CreateSO(poursNeeded: 2, bonusPerGoblet: 790);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GobletCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1580));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GobletSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GobletSO, Is.Null);
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
            typeof(ZoneControlCaptureGobletController)
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

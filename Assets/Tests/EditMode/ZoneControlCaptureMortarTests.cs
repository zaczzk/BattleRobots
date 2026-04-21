using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMortarTests
    {
        private static ZoneControlCaptureMortarSO CreateSO(
            int grindsNeeded  = 7,
            int spillPerBot   = 2,
            int bonusPerGrind = 1030)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMortarSO>();
            typeof(ZoneControlCaptureMortarSO)
                .GetField("_grindsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, grindsNeeded);
            typeof(ZoneControlCaptureMortarSO)
                .GetField("_spillPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, spillPerBot);
            typeof(ZoneControlCaptureMortarSO)
                .GetField("_bonusPerGrind", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGrind);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMortarController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMortarController>();
        }

        [Test]
        public void SO_FreshInstance_Grinds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Grinds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GrindCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GrindCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGrinds()
        {
            var so = CreateSO(grindsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Grinds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_GrindsAtThreshold()
        {
            var so    = CreateSO(grindsNeeded: 3, bonusPerGrind: 1030);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1030));
            Assert.That(so.GrindCount,  Is.EqualTo(1));
            Assert.That(so.Grinds,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(grindsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesGrinds()
        {
            var so = CreateSO(grindsNeeded: 7, spillPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Grinds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(grindsNeeded: 7, spillPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Grinds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GrindProgress_Clamped()
        {
            var so = CreateSO(grindsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.GrindProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMortarGround_FiresEvent()
        {
            var so    = CreateSO(grindsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMortarSO)
                .GetField("_onMortarGround", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(grindsNeeded: 2, bonusPerGrind: 1030);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Grinds,            Is.EqualTo(0));
            Assert.That(so.GrindCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGrinds_Accumulate()
        {
            var so = CreateSO(grindsNeeded: 2, bonusPerGrind: 1030);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GrindCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2060));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MortarSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MortarSO, Is.Null);
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
            typeof(ZoneControlCaptureMortarController)
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

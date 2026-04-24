using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCobordismTests
    {
        private static ZoneControlCaptureCobordismSO CreateSO(
            int boundariesNeeded  = 5,
            int puncturePerBot    = 1,
            int bonusPerCobordism = 3535)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCobordismSO>();
            typeof(ZoneControlCaptureCobordismSO)
                .GetField("_boundariesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, boundariesNeeded);
            typeof(ZoneControlCaptureCobordismSO)
                .GetField("_puncturePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, puncturePerBot);
            typeof(ZoneControlCaptureCobordismSO)
                .GetField("_bonusPerCobordism", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCobordism);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCobordismController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCobordismController>();
        }

        [Test]
        public void SO_FreshInstance_Boundaries_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Boundaries, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CobordismCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CobordismCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBoundaries()
        {
            var so = CreateSO(boundariesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Boundaries, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(boundariesNeeded: 3, bonusPerCobordism: 3535);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3535));
            Assert.That(so.CobordismCount,  Is.EqualTo(1));
            Assert.That(so.Boundaries,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(boundariesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_PuncturesBoundaries()
        {
            var so = CreateSO(boundariesNeeded: 5, puncturePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Boundaries, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(boundariesNeeded: 5, puncturePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Boundaries, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CobordismProgress_Clamped()
        {
            var so = CreateSO(boundariesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CobordismProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCobordismComplete_FiresEvent()
        {
            var so    = CreateSO(boundariesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCobordismSO)
                .GetField("_onCobordismComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(boundariesNeeded: 2, bonusPerCobordism: 3535);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Boundaries,        Is.EqualTo(0));
            Assert.That(so.CobordismCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCobordisms_Accumulate()
        {
            var so = CreateSO(boundariesNeeded: 2, bonusPerCobordism: 3535);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CobordismCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7070));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CobordismSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CobordismSO, Is.Null);
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
            typeof(ZoneControlCaptureCobordismController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureColimitTests
    {
        private static ZoneControlCaptureColimitSO CreateSO(
            int diagramsNeeded  = 7,
            int dissolvePerBot  = 2,
            int bonusPerColimit = 2770)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureColimitSO>();
            typeof(ZoneControlCaptureColimitSO)
                .GetField("_diagramsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, diagramsNeeded);
            typeof(ZoneControlCaptureColimitSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureColimitSO)
                .GetField("_bonusPerColimit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerColimit);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureColimitController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureColimitController>();
        }

        [Test]
        public void SO_FreshInstance_Diagrams_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Diagrams, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ColimitCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ColimitCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDiagrams()
        {
            var so = CreateSO(diagramsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Diagrams, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(diagramsNeeded: 3, bonusPerColimit: 2770);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2770));
            Assert.That(so.ColimitCount, Is.EqualTo(1));
            Assert.That(so.Diagrams,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(diagramsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesDiagrams()
        {
            var so = CreateSO(diagramsNeeded: 7, dissolvePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Diagrams, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(diagramsNeeded: 7, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Diagrams, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DiagramProgress_Clamped()
        {
            var so = CreateSO(diagramsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.DiagramProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnColimitComputed_FiresEvent()
        {
            var so    = CreateSO(diagramsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureColimitSO)
                .GetField("_onColimitComputed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(diagramsNeeded: 2, bonusPerColimit: 2770);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Diagrams,          Is.EqualTo(0));
            Assert.That(so.ColimitCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleColimits_Accumulate()
        {
            var so = CreateSO(diagramsNeeded: 2, bonusPerColimit: 2770);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ColimitCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5540));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ColimitSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ColimitSO, Is.Null);
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
            typeof(ZoneControlCaptureColimitController)
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

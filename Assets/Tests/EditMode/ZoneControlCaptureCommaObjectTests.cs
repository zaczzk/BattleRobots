using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCommaObjectTests
    {
        private static ZoneControlCaptureCommaObjectSO CreateSO(
            int arcsNeeded    = 7,
            int contractPerBot = 2,
            int bonusPerComma  = 3010)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCommaObjectSO>();
            typeof(ZoneControlCaptureCommaObjectSO)
                .GetField("_arcsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, arcsNeeded);
            typeof(ZoneControlCaptureCommaObjectSO)
                .GetField("_contractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, contractPerBot);
            typeof(ZoneControlCaptureCommaObjectSO)
                .GetField("_bonusPerComma", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerComma);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCommaObjectController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCommaObjectController>();
        }

        [Test]
        public void SO_FreshInstance_Arcs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Arcs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CommaCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CommaCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesArcs()
        {
            var so = CreateSO(arcsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Arcs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(arcsNeeded: 3, bonusPerComma: 3010);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3010));
            Assert.That(so.CommaCount,  Is.EqualTo(1));
            Assert.That(so.Arcs,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(arcsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesArcs()
        {
            var so = CreateSO(arcsNeeded: 7, contractPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Arcs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(arcsNeeded: 7, contractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Arcs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ArcProgress_Clamped()
        {
            var so = CreateSO(arcsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ArcProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCommaObjectFormed_FiresEvent()
        {
            var so    = CreateSO(arcsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCommaObjectSO)
                .GetField("_onCommaObjectFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(arcsNeeded: 2, bonusPerComma: 3010);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Arcs,              Is.EqualTo(0));
            Assert.That(so.CommaCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCommas_Accumulate()
        {
            var so = CreateSO(arcsNeeded: 2, bonusPerComma: 3010);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CommaCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6020));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CommaObjectSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CommaObjectSO, Is.Null);
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
            typeof(ZoneControlCaptureCommaObjectController)
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

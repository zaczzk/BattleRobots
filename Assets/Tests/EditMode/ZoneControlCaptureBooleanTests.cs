using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBooleanTests
    {
        private static ZoneControlCaptureBooleanSO CreateSO(
            int complementsNeeded   = 6,
            int flipPerBot          = 2,
            int bonusPerComplement  = 3250)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBooleanSO>();
            typeof(ZoneControlCaptureBooleanSO)
                .GetField("_complementsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, complementsNeeded);
            typeof(ZoneControlCaptureBooleanSO)
                .GetField("_flipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, flipPerBot);
            typeof(ZoneControlCaptureBooleanSO)
                .GetField("_bonusPerComplement", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerComplement);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBooleanController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBooleanController>();
        }

        [Test]
        public void SO_FreshInstance_Complements_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Complements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComplementCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComplementCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesComplements()
        {
            var so = CreateSO(complementsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Complements, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(complementsNeeded: 3, bonusPerComplement: 3250);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(3250));
            Assert.That(so.ComplementCount,   Is.EqualTo(1));
            Assert.That(so.Complements,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(complementsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesComplements()
        {
            var so = CreateSO(complementsNeeded: 6, flipPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Complements, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(complementsNeeded: 6, flipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Complements, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComplementProgress_Clamped()
        {
            var so = CreateSO(complementsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ComplementProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnComplemented_FiresEvent()
        {
            var so    = CreateSO(complementsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBooleanSO)
                .GetField("_onComplemented", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(complementsNeeded: 2, bonusPerComplement: 3250);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Complements,       Is.EqualTo(0));
            Assert.That(so.ComplementCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleComplements_Accumulate()
        {
            var so = CreateSO(complementsNeeded: 2, bonusPerComplement: 3250);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComplementCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BooleanSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BooleanSO, Is.Null);
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
            typeof(ZoneControlCaptureBooleanController)
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

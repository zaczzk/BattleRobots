using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMonoidTests
    {
        private static ZoneControlCaptureMonoidSO CreateSO(
            int unitsNeeded      = 7,
            int resetPerBot      = 2,
            int bonusPerIdentity = 2290)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMonoidSO>();
            typeof(ZoneControlCaptureMonoidSO)
                .GetField("_unitsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unitsNeeded);
            typeof(ZoneControlCaptureMonoidSO)
                .GetField("_resetPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, resetPerBot);
            typeof(ZoneControlCaptureMonoidSO)
                .GetField("_bonusPerIdentity", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIdentity);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMonoidController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMonoidController>();
        }

        [Test]
        public void SO_FreshInstance_Units_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Units, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IdentityCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IdentityCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesUnits()
        {
            var so = CreateSO(unitsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Units, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(unitsNeeded: 3, bonusPerIdentity: 2290);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(2290));
            Assert.That(so.IdentityCount,  Is.EqualTo(1));
            Assert.That(so.Units,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(unitsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesUnits()
        {
            var so = CreateSO(unitsNeeded: 7, resetPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Units, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(unitsNeeded: 7, resetPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Units, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UnitProgress_Clamped()
        {
            var so = CreateSO(unitsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.UnitProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMonoidIdentified_FiresEvent()
        {
            var so    = CreateSO(unitsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMonoidSO)
                .GetField("_onMonoidIdentified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(unitsNeeded: 2, bonusPerIdentity: 2290);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Units,             Is.EqualTo(0));
            Assert.That(so.IdentityCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIdentities_Accumulate()
        {
            var so = CreateSO(unitsNeeded: 2, bonusPerIdentity: 2290);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IdentityCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4580));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MonoidSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MonoidSO, Is.Null);
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
            typeof(ZoneControlCaptureMonoidController)
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

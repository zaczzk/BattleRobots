using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCommutatorTests
    {
        private static ZoneControlCaptureCommutatorSO CreateSO(
            int contactsNeeded      = 7,
            int wearPerBot          = 2,
            int bonusPerCommutation = 1510)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCommutatorSO>();
            typeof(ZoneControlCaptureCommutatorSO)
                .GetField("_contactsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, contactsNeeded);
            typeof(ZoneControlCaptureCommutatorSO)
                .GetField("_wearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wearPerBot);
            typeof(ZoneControlCaptureCommutatorSO)
                .GetField("_bonusPerCommutation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCommutation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCommutatorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCommutatorController>();
        }

        [Test]
        public void SO_FreshInstance_Contacts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Contacts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CommutationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CommutationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesContacts()
        {
            var so = CreateSO(contactsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Contacts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ContactsAtThreshold()
        {
            var so    = CreateSO(contactsNeeded: 3, bonusPerCommutation: 1510);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(1510));
            Assert.That(so.CommutationCount,  Is.EqualTo(1));
            Assert.That(so.Contacts,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(contactsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WearsContacts()
        {
            var so = CreateSO(contactsNeeded: 7, wearPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Contacts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(contactsNeeded: 7, wearPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Contacts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ContactProgress_Clamped()
        {
            var so = CreateSO(contactsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ContactProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCommutatorFired_FiresEvent()
        {
            var so    = CreateSO(contactsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCommutatorSO)
                .GetField("_onCommutatorFired", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(contactsNeeded: 2, bonusPerCommutation: 1510);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Contacts,          Is.EqualTo(0));
            Assert.That(so.CommutationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCommutations_Accumulate()
        {
            var so = CreateSO(contactsNeeded: 2, bonusPerCommutation: 1510);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CommutationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3020));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CommutatorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CommutatorSO, Is.Null);
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
            typeof(ZoneControlCaptureCommutatorController)
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

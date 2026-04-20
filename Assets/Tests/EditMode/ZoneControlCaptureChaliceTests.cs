using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureChaliceTests
    {
        private static ZoneControlCaptureChaliceSO CreateSO(
            int offeringsNeeded = 4,
            int drainPerBot     = 1,
            int bonusPerFilling = 520)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureChaliceSO>();
            typeof(ZoneControlCaptureChaliceSO)
                .GetField("_offeringsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, offeringsNeeded);
            typeof(ZoneControlCaptureChaliceSO)
                .GetField("_drainPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drainPerBot);
            typeof(ZoneControlCaptureChaliceSO)
                .GetField("_bonusPerFilling", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFilling);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureChaliceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureChaliceController>();
        }

        [Test]
        public void SO_FreshInstance_Offerings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Offerings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FillingCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FillingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesOfferings()
        {
            var so = CreateSO(offeringsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Offerings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsAtThreshold()
        {
            var so    = CreateSO(offeringsNeeded: 3, bonusPerFilling: 520);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(520));
            Assert.That(so.FillingCount, Is.EqualTo(1));
            Assert.That(so.Offerings,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(offeringsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsOfferings()
        {
            var so = CreateSO(offeringsNeeded: 4, drainPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Offerings, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(offeringsNeeded: 4, drainPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Offerings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OfferingProgress_Clamped()
        {
            var so = CreateSO(offeringsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.OfferingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnChaliceFilled_FiresEvent()
        {
            var so    = CreateSO(offeringsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureChaliceSO)
                .GetField("_onChaliceFilled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(offeringsNeeded: 2, bonusPerFilling: 520);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Offerings,         Is.EqualTo(0));
            Assert.That(so.FillingCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFillings_Accumulate()
        {
            var so = CreateSO(offeringsNeeded: 2, bonusPerFilling: 520);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FillingCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1040));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ChaliceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChaliceSO, Is.Null);
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
            typeof(ZoneControlCaptureChaliceController)
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

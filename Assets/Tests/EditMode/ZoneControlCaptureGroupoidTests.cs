using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGroupoidTests
    {
        private static ZoneControlCaptureGroupoidSO CreateSO(
            int morphismsNeeded   = 5,
            int composePerBot     = 1,
            int bonusPerInversion = 3475)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGroupoidSO>();
            typeof(ZoneControlCaptureGroupoidSO)
                .GetField("_morphismsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, morphismsNeeded);
            typeof(ZoneControlCaptureGroupoidSO)
                .GetField("_composePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, composePerBot);
            typeof(ZoneControlCaptureGroupoidSO)
                .GetField("_bonusPerInversion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInversion);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGroupoidController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGroupoidController>();
        }

        [Test]
        public void SO_FreshInstance_Morphisms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InversionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InversionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(morphismsNeeded: 3, bonusPerInversion: 3475);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3475));
            Assert.That(so.InversionCount,  Is.EqualTo(1));
            Assert.That(so.Morphisms,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(morphismsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ComposesAwayMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 5, composePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(morphismsNeeded: 5, composePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GroupoidProgress_Clamped()
        {
            var so = CreateSO(morphismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.GroupoidProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGroupoidInverted_FiresEvent()
        {
            var so    = CreateSO(morphismsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGroupoidSO)
                .GetField("_onGroupoidInverted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(morphismsNeeded: 2, bonusPerInversion: 3475);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Morphisms,         Is.EqualTo(0));
            Assert.That(so.InversionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInversions_Accumulate()
        {
            var so = CreateSO(morphismsNeeded: 2, bonusPerInversion: 3475);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InversionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6950));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GroupoidSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GroupoidSO, Is.Null);
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
            typeof(ZoneControlCaptureGroupoidController)
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

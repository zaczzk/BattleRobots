using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRepresentableTests
    {
        private static ZoneControlCaptureRepresentableSO CreateSO(
            int setsNeeded            = 5,
            int prunePerBot           = 1,
            int bonusPerRepresentation = 2995)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRepresentableSO>();
            typeof(ZoneControlCaptureRepresentableSO)
                .GetField("_setsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, setsNeeded);
            typeof(ZoneControlCaptureRepresentableSO)
                .GetField("_prunePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, prunePerBot);
            typeof(ZoneControlCaptureRepresentableSO)
                .GetField("_bonusPerRepresentation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRepresentation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRepresentableController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRepresentableController>();
        }

        [Test]
        public void SO_FreshInstance_Sets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Sets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RepresentationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RepresentationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSets()
        {
            var so = CreateSO(setsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Sets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(setsNeeded: 3, bonusPerRepresentation: 2995);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(2995));
            Assert.That(so.RepresentationCount,   Is.EqualTo(1));
            Assert.That(so.Sets,                  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(setsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSets()
        {
            var so = CreateSO(setsNeeded: 5, prunePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Sets, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(setsNeeded: 5, prunePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Sets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetProgress_Clamped()
        {
            var so = CreateSO(setsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SetProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRepresentableRepresented_FiresEvent()
        {
            var so    = CreateSO(setsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRepresentableSO)
                .GetField("_onRepresentableRepresented", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(setsNeeded: 2, bonusPerRepresentation: 2995);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Sets,                  Is.EqualTo(0));
            Assert.That(so.RepresentationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRepresentations_Accumulate()
        {
            var so = CreateSO(setsNeeded: 2, bonusPerRepresentation: 2995);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RepresentationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(5990));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RepresentableSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RepresentableSO, Is.Null);
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
            typeof(ZoneControlCaptureRepresentableController)
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

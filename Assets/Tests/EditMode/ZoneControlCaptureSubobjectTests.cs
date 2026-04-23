using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSubobjectTests
    {
        private static ZoneControlCaptureSubobjectSO CreateSO(
            int inclusionsNeeded  = 6,
            int excludePerBot     = 2,
            int bonusPerSubobject = 2920)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSubobjectSO>();
            typeof(ZoneControlCaptureSubobjectSO)
                .GetField("_inclusionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, inclusionsNeeded);
            typeof(ZoneControlCaptureSubobjectSO)
                .GetField("_excludePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, excludePerBot);
            typeof(ZoneControlCaptureSubobjectSO)
                .GetField("_bonusPerSubobject", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSubobject);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSubobjectController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSubobjectController>();
        }

        [Test]
        public void SO_FreshInstance_Inclusions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Inclusions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SubobjectCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SubobjectCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesInclusions()
        {
            var so = CreateSO(inclusionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Inclusions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(inclusionsNeeded: 3, bonusPerSubobject: 2920);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(2920));
            Assert.That(so.SubobjectCount, Is.EqualTo(1));
            Assert.That(so.Inclusions,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(inclusionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesInclusions()
        {
            var so = CreateSO(inclusionsNeeded: 6, excludePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Inclusions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(inclusionsNeeded: 6, excludePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Inclusions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_InclusionProgress_Clamped()
        {
            var so = CreateSO(inclusionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.InclusionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSubobjectClassified_FiresEvent()
        {
            var so    = CreateSO(inclusionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSubobjectSO)
                .GetField("_onSubobjectClassified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(inclusionsNeeded: 2, bonusPerSubobject: 2920);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Inclusions,        Is.EqualTo(0));
            Assert.That(so.SubobjectCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSubobjects_Accumulate()
        {
            var so = CreateSO(inclusionsNeeded: 2, bonusPerSubobject: 2920);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SubobjectCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5840));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SubobjectSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SubobjectSO, Is.Null);
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
            typeof(ZoneControlCaptureSubobjectController)
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

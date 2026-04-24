using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureExcisionTests
    {
        private static ZoneControlCaptureExcisionSO CreateSO(
            int subsetsNeeded     = 7,
            int reintroducePerBot = 2,
            int bonusPerExcision  = 3970)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureExcisionSO>();
            typeof(ZoneControlCaptureExcisionSO)
                .GetField("_subsetsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, subsetsNeeded);
            typeof(ZoneControlCaptureExcisionSO)
                .GetField("_reintroducePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reintroducePerBot);
            typeof(ZoneControlCaptureExcisionSO)
                .GetField("_bonusPerExcision", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExcision);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureExcisionController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureExcisionController>();
        }

        [Test]
        public void SO_FreshInstance_Subsets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Subsets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExcisionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExcisionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSubsets()
        {
            var so = CreateSO(subsetsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Subsets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(subsetsNeeded: 3, bonusPerExcision: 3970);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(3970));
            Assert.That(so.ExcisionCount,  Is.EqualTo(1));
            Assert.That(so.Subsets,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(subsetsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReintroducesSubsets()
        {
            var so = CreateSO(subsetsNeeded: 7, reintroducePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Subsets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(subsetsNeeded: 7, reintroducePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Subsets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SubsetProgress_Clamped()
        {
            var so = CreateSO(subsetsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.SubsetProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnExcisionComplete_FiresEvent()
        {
            var so    = CreateSO(subsetsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureExcisionSO)
                .GetField("_onExcisionComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(subsetsNeeded: 2, bonusPerExcision: 3970);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Subsets,           Is.EqualTo(0));
            Assert.That(so.ExcisionCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExcisions_Accumulate()
        {
            var so = CreateSO(subsetsNeeded: 2, bonusPerExcision: 3970);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExcisionCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7940));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ExcisionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ExcisionSO, Is.Null);
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
            typeof(ZoneControlCaptureExcisionController)
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

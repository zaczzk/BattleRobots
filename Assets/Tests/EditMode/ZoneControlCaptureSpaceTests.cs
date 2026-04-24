using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpaceTests
    {
        private static ZoneControlCaptureSpaceSO CreateSO(
            int pointsNeeded  = 5,
            int contractPerBot = 1,
            int bonusPerOpen  = 3505)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpaceSO>();
            typeof(ZoneControlCaptureSpaceSO)
                .GetField("_pointsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pointsNeeded);
            typeof(ZoneControlCaptureSpaceSO)
                .GetField("_contractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, contractPerBot);
            typeof(ZoneControlCaptureSpaceSO)
                .GetField("_bonusPerOpen", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerOpen);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpaceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpaceController>();
        }

        [Test]
        public void SO_FreshInstance_Points_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Points, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_OpenCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OpenCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPoints()
        {
            var so = CreateSO(pointsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Points, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(pointsNeeded: 3, bonusPerOpen: 3505);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(3505));
            Assert.That(so.OpenCount, Is.EqualTo(1));
            Assert.That(so.Points,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(pointsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ContractsPoints()
        {
            var so = CreateSO(pointsNeeded: 5, contractPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Points, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(pointsNeeded: 5, contractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Points, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SpaceProgress_Clamped()
        {
            var so = CreateSO(pointsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SpaceProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpaceOpened_FiresEvent()
        {
            var so    = CreateSO(pointsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpaceSO)
                .GetField("_onSpaceOpened", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(pointsNeeded: 2, bonusPerOpen: 3505);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Points,            Is.EqualTo(0));
            Assert.That(so.OpenCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleOpens_Accumulate()
        {
            var so = CreateSO(pointsNeeded: 2, bonusPerOpen: 3505);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.OpenCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7010));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpaceSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpaceSO, Is.Null);
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
            typeof(ZoneControlCaptureSpaceController)
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

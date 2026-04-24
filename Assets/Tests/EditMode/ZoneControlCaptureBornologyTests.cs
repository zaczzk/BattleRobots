using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBornologyTests
    {
        private static ZoneControlCaptureBornologySO CreateSO(
            int setsNeeded    = 6,
            int unboundPerBot = 1,
            int bonusPerBound = 3430)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBornologySO>();
            typeof(ZoneControlCaptureBornologySO)
                .GetField("_setsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, setsNeeded);
            typeof(ZoneControlCaptureBornologySO)
                .GetField("_unboundPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unboundPerBot);
            typeof(ZoneControlCaptureBornologySO)
                .GetField("_bonusPerBound", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBound);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBornologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBornologyController>();
        }

        [Test]
        public void SO_FreshInstance_Sets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Sets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BoundCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BoundCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSets()
        {
            var so = CreateSO(setsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Sets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(setsNeeded: 3, bonusPerBound: 3430);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(3430));
            Assert.That(so.BoundCount, Is.EqualTo(1));
            Assert.That(so.Sets,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(setsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_UnboundsSets()
        {
            var so = CreateSO(setsNeeded: 6, unboundPerBot: 1);
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
            var so = CreateSO(setsNeeded: 6, unboundPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Sets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BornologyProgress_Clamped()
        {
            var so = CreateSO(setsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.BornologyProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBornologyBounded_FiresEvent()
        {
            var so    = CreateSO(setsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBornologySO)
                .GetField("_onBornologyBounded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(setsNeeded: 2, bonusPerBound: 3430);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Sets,              Is.EqualTo(0));
            Assert.That(so.BoundCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBounds_Accumulate()
        {
            var so = CreateSO(setsNeeded: 2, bonusPerBound: 3430);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BoundCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6860));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BornologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BornologySO, Is.Null);
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
            typeof(ZoneControlCaptureBornologyController)
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

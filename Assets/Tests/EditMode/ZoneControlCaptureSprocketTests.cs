using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSprocketTests
    {
        private static ZoneControlCaptureSprocketSO CreateSO(
            int teethNeeded    = 5,
            int wearPerBot     = 1,
            int bonusPerEngage = 1255)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSprocketSO>();
            typeof(ZoneControlCaptureSprocketSO)
                .GetField("_teethNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, teethNeeded);
            typeof(ZoneControlCaptureSprocketSO)
                .GetField("_wearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wearPerBot);
            typeof(ZoneControlCaptureSprocketSO)
                .GetField("_bonusPerEngage", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEngage);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSprocketController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSprocketController>();
        }

        [Test]
        public void SO_FreshInstance_Teeth_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Teeth, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EngageCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EngageCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTeeth()
        {
            var so = CreateSO(teethNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Teeth, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_TeethAtThreshold()
        {
            var so    = CreateSO(teethNeeded: 3, bonusPerEngage: 1255);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1255));
            Assert.That(so.EngageCount, Is.EqualTo(1));
            Assert.That(so.Teeth,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(teethNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesTeeth()
        {
            var so = CreateSO(teethNeeded: 5, wearPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Teeth, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(teethNeeded: 5, wearPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Teeth, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ToothProgress_Clamped()
        {
            var so = CreateSO(teethNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ToothProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSprocketEngaged_FiresEvent()
        {
            var so    = CreateSO(teethNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSprocketSO)
                .GetField("_onSprocketEngaged", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(teethNeeded: 2, bonusPerEngage: 1255);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Teeth,             Is.EqualTo(0));
            Assert.That(so.EngageCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEngagements_Accumulate()
        {
            var so = CreateSO(teethNeeded: 2, bonusPerEngage: 1255);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EngageCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2510));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SprocketSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SprocketSO, Is.Null);
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
            typeof(ZoneControlCaptureSprocketController)
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

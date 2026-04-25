using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePerverseSheafTests
    {
        private static ZoneControlCapturePerverseSheafSO CreateSO(
            int stalkConditionsNeeded    = 7,
            int supportConditionsPerBot  = 2,
            int bonusPerPerversification = 4210)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePerverseSheafSO>();
            typeof(ZoneControlCapturePerverseSheafSO)
                .GetField("_stalkConditionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stalkConditionsNeeded);
            typeof(ZoneControlCapturePerverseSheafSO)
                .GetField("_supportConditionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, supportConditionsPerBot);
            typeof(ZoneControlCapturePerverseSheafSO)
                .GetField("_bonusPerPerversification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPerversification);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePerverseSheafController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePerverseSheafController>();
        }

        [Test]
        public void SO_FreshInstance_StalkConditions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StalkConditions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PerversificationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PerversificationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStalkConditions()
        {
            var so = CreateSO(stalkConditionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.StalkConditions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(stalkConditionsNeeded: 3, bonusPerPerversification: 4210);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(4210));
            Assert.That(so.PerversificationCount, Is.EqualTo(1));
            Assert.That(so.StalkConditions,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stalkConditionsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSupportConditions()
        {
            var so = CreateSO(stalkConditionsNeeded: 7, supportConditionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StalkConditions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stalkConditionsNeeded: 7, supportConditionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StalkConditions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StalkConditionProgress_Clamped()
        {
            var so = CreateSO(stalkConditionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.StalkConditionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPerverseSheafPerversified_FiresEvent()
        {
            var so    = CreateSO(stalkConditionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePerverseSheafSO)
                .GetField("_onPerverseSheafPerversified", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stalkConditionsNeeded: 2, bonusPerPerversification: 4210);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.StalkConditions,       Is.EqualTo(0));
            Assert.That(so.PerversificationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePerversifications_Accumulate()
        {
            var so = CreateSO(stalkConditionsNeeded: 2, bonusPerPerversification: 4210);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PerversificationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(8420));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PerverseSheafSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PerverseSheafSO, Is.Null);
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
            typeof(ZoneControlCapturePerverseSheafController)
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

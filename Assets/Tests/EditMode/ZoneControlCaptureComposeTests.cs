using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureComposeTests
    {
        private static ZoneControlCaptureComposeSO CreateSO(
            int stepsNeeded     = 5,
            int decomposePerBot = 1,
            int bonusPerCompose = 2155)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureComposeSO>();
            typeof(ZoneControlCaptureComposeSO)
                .GetField("_stepsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stepsNeeded);
            typeof(ZoneControlCaptureComposeSO)
                .GetField("_decomposePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decomposePerBot);
            typeof(ZoneControlCaptureComposeSO)
                .GetField("_bonusPerCompose", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompose);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureComposeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureComposeController>();
        }

        [Test]
        public void SO_FreshInstance_Steps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Steps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComposeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComposeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSteps()
        {
            var so = CreateSO(stepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Steps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(stepsNeeded: 3, bonusPerCompose: 2155);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(2155));
            Assert.That(so.ComposeCount,  Is.EqualTo(1));
            Assert.That(so.Steps,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stepsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DecomposesSteps()
        {
            var so = CreateSO(stepsNeeded: 5, decomposePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Steps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stepsNeeded: 5, decomposePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Steps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComposeProgress_Clamped()
        {
            var so = CreateSO(stepsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ComposeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnComposeComplete_FiresEvent()
        {
            var so    = CreateSO(stepsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureComposeSO)
                .GetField("_onComposeComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stepsNeeded: 2, bonusPerCompose: 2155);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Steps,             Is.EqualTo(0));
            Assert.That(so.ComposeCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleComposes_Accumulate()
        {
            var so = CreateSO(stepsNeeded: 2, bonusPerCompose: 2155);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComposeCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4310));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ComposeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ComposeSO, Is.Null);
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
            typeof(ZoneControlCaptureComposeController)
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

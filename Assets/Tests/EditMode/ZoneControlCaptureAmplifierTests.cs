using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAmplifierTests
    {
        private static ZoneControlCaptureAmplifierSO CreateSO(
            int stagesNeeded          = 7,
            int attenuatePerBot       = 2,
            int bonusPerAmplification = 1615)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAmplifierSO>();
            typeof(ZoneControlCaptureAmplifierSO)
                .GetField("_stagesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stagesNeeded);
            typeof(ZoneControlCaptureAmplifierSO)
                .GetField("_attenuatePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, attenuatePerBot);
            typeof(ZoneControlCaptureAmplifierSO)
                .GetField("_bonusPerAmplification", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAmplification);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAmplifierController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAmplifierController>();
        }

        [Test]
        public void SO_FreshInstance_Stages_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Stages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AmplificationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AmplificationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStages()
        {
            var so = CreateSO(stagesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Stages, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_StagesAtThreshold()
        {
            var so    = CreateSO(stagesNeeded: 3, bonusPerAmplification: 1615);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(1615));
            Assert.That(so.AmplificationCount,  Is.EqualTo(1));
            Assert.That(so.Stages,              Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stagesNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AttenuatesStages()
        {
            var so = CreateSO(stagesNeeded: 7, attenuatePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stages, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stagesNeeded: 7, attenuatePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StageProgress_Clamped()
        {
            var so = CreateSO(stagesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.StageProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAmplifierBoosted_FiresEvent()
        {
            var so    = CreateSO(stagesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAmplifierSO)
                .GetField("_onAmplifierBoosted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stagesNeeded: 2, bonusPerAmplification: 1615);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Stages,              Is.EqualTo(0));
            Assert.That(so.AmplificationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAmplifications_Accumulate()
        {
            var so = CreateSO(stagesNeeded: 2, bonusPerAmplification: 1615);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AmplificationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(3230));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AmplifierSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AmplifierSO, Is.Null);
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
            typeof(ZoneControlCaptureAmplifierController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNexusTests
    {
        private static ZoneControlCaptureNexusSO CreateSO(int bonus = 300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNexusSO>();
            typeof(ZoneControlCaptureNexusSO)
                .GetField("_bonusPerNexus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNexusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNexusController>();
        }

        [Test]
        public void SO_FreshInstance_NexusStep_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NexusStep, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_NexusCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NexusCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Step0_AdvancesTo1()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.NexusStep, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_Step1_AdvancesTo2()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.NexusStep, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Step2_CompletesNexus()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.NexusStep,  Is.EqualTo(0));
            Assert.That(so.NexusCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NexusComplete_AccumulatesBonus()
        {
            var so = CreateSO(bonus: 150);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NexusComplete_FiresEvent()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNexusSO)
                .GetField("_onNexusComplete", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_OutOfSequence_ResetsStep()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.NexusStep,  Is.EqualTo(0));
            Assert.That(so.NexusCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NexusProgress_Computed()
        {
            var so = CreateSO();
            Assert.That(so.NexusProgress, Is.EqualTo(0f).Within(0.001f));
            so.RecordPlayerCapture();
            Assert.That(so.NexusProgress, Is.EqualTo(1f / 3f).Within(0.001f));
            so.RecordBotCapture();
            Assert.That(so.NexusProgress, Is.EqualTo(2f / 3f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bonus: 300);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.NexusStep,         Is.EqualTo(0));
            Assert.That(so.NexusCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NexusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NexusSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureNexusController)
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

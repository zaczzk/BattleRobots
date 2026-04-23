using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNaturalTests
    {
        private static ZoneControlCaptureNaturalSO CreateSO(
            int componentsNeeded      = 7,
            int perturbPerBot         = 2,
            int bonusPerTransformation = 2935)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNaturalSO>();
            typeof(ZoneControlCaptureNaturalSO)
                .GetField("_componentsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, componentsNeeded);
            typeof(ZoneControlCaptureNaturalSO)
                .GetField("_perturbPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, perturbPerBot);
            typeof(ZoneControlCaptureNaturalSO)
                .GetField("_bonusPerTransformation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTransformation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNaturalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNaturalController>();
        }

        [Test]
        public void SO_FreshInstance_Components_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Components, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TransformationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TransformationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesComponents()
        {
            var so = CreateSO(componentsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Components, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(componentsNeeded: 3, bonusPerTransformation: 2935);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(2935));
            Assert.That(so.TransformationCount,  Is.EqualTo(1));
            Assert.That(so.Components,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(componentsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesComponents()
        {
            var so = CreateSO(componentsNeeded: 7, perturbPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Components, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(componentsNeeded: 7, perturbPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Components, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComponentProgress_Clamped()
        {
            var so = CreateSO(componentsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ComponentProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnNaturalTransformed_FiresEvent()
        {
            var so    = CreateSO(componentsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNaturalSO)
                .GetField("_onNaturalTransformed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(componentsNeeded: 2, bonusPerTransformation: 2935);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Components,          Is.EqualTo(0));
            Assert.That(so.TransformationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTransformations_Accumulate()
        {
            var so = CreateSO(componentsNeeded: 2, bonusPerTransformation: 2935);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TransformationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(5870));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NaturalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NaturalSO, Is.Null);
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
            typeof(ZoneControlCaptureNaturalController)
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

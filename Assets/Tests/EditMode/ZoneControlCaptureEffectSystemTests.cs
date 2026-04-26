using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEffectSystemTests
    {
        private static ZoneControlCaptureEffectSystemSO CreateSO(
            int pureEffectAnnotationsNeeded = 6,
            int effectLeaksPerBot           = 1,
            int bonusPerAnnotation          = 5230)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEffectSystemSO>();
            typeof(ZoneControlCaptureEffectSystemSO)
                .GetField("_pureEffectAnnotationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pureEffectAnnotationsNeeded);
            typeof(ZoneControlCaptureEffectSystemSO)
                .GetField("_effectLeaksPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, effectLeaksPerBot);
            typeof(ZoneControlCaptureEffectSystemSO)
                .GetField("_bonusPerAnnotation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAnnotation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEffectSystemController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEffectSystemController>();
        }

        [Test]
        public void SO_FreshInstance_PureEffectAnnotations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PureEffectAnnotations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AnnotationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AnnotationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPureEffectAnnotations()
        {
            var so = CreateSO(pureEffectAnnotationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PureEffectAnnotations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(pureEffectAnnotationsNeeded: 3, bonusPerAnnotation: 5230);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(5230));
            Assert.That(so.AnnotationCount,       Is.EqualTo(1));
            Assert.That(so.PureEffectAnnotations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(pureEffectAnnotationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesEffectLeaks()
        {
            var so = CreateSO(pureEffectAnnotationsNeeded: 6, effectLeaksPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PureEffectAnnotations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(pureEffectAnnotationsNeeded: 6, effectLeaksPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PureEffectAnnotations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PureEffectAnnotationProgress_Clamped()
        {
            var so = CreateSO(pureEffectAnnotationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PureEffectAnnotationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEffectSystemCompleted_FiresEvent()
        {
            var so    = CreateSO(pureEffectAnnotationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEffectSystemSO)
                .GetField("_onEffectSystemCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(pureEffectAnnotationsNeeded: 2, bonusPerAnnotation: 5230);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PureEffectAnnotations, Is.EqualTo(0));
            Assert.That(so.AnnotationCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAnnotations_Accumulate()
        {
            var so = CreateSO(pureEffectAnnotationsNeeded: 2, bonusPerAnnotation: 5230);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AnnotationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10460));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EffectSystemSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EffectSystemSO, Is.Null);
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
            typeof(ZoneControlCaptureEffectSystemController)
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

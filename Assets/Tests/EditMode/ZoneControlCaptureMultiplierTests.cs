using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T462: <see cref="ZoneControlCaptureMultiplierSO"/> and
    /// <see cref="ZoneControlCaptureMultiplierController"/>.
    ///
    /// ZoneControlCaptureMultiplierTests (12):
    ///   SO_FreshInstance_CurrentMultiplier_One                                       x1
    ///   SO_RecordCapture_IncrementsMultiplier                                        x1
    ///   SO_RecordCapture_ClampsAtMaxMultiplier                                       x1
    ///   SO_RecordCapture_MultipleSteps_Accumulate                                    x1
    ///   SO_RewardForCapture_ScalesByMultiplier                                       x1
    ///   SO_RewardForCapture_AtMaxMultiplier_UsesMax                                  x1
    ///   SO_Reset_RestoresDefaultMultiplier                                           x1
    ///   SO_Reset_AfterCaptures_RestoresOne                                           x1
    ///   SO_RewardForCapture_ZeroBase_ReturnsZero                                     x1
    ///   Controller_FreshInstance_MultiplierSO_Null                                   x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                    x1
    ///   Controller_Refresh_NullSO_HidesPanel                                         x1
    /// </summary>
    public sealed class ZoneControlCaptureMultiplierTests
    {
        private static ZoneControlCaptureMultiplierSO CreateSO(float step = 0.1f, float maxMultiplier = 3f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMultiplierSO>();
            typeof(ZoneControlCaptureMultiplierSO)
                .GetField("_multiplierStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, step);
            typeof(ZoneControlCaptureMultiplierSO)
                .GetField("_maxMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxMultiplier);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMultiplierController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMultiplierController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentMultiplier_One()
        {
            var so = CreateSO();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsMultiplier()
        {
            var so = CreateSO(step: 0.1f, maxMultiplier: 3f);
            so.RecordCapture();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1.1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ClampsAtMaxMultiplier()
        {
            var so = CreateSO(step: 1f, maxMultiplier: 2f);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(2f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_MultipleSteps_Accumulate()
        {
            var so = CreateSO(step: 0.5f, maxMultiplier: 10f);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(2f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RewardForCapture_ScalesByMultiplier()
        {
            var so = CreateSO(step: 1f, maxMultiplier: 5f);
            so.RecordCapture();
            int reward = so.RewardForCapture(100);
            Assert.That(reward, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RewardForCapture_AtMaxMultiplier_UsesMax()
        {
            var so = CreateSO(step: 5f, maxMultiplier: 3f);
            so.RecordCapture();
            int reward = so.RewardForCapture(100);
            Assert.That(reward, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_RestoresDefaultMultiplier()
        {
            var so = CreateSO(step: 0.1f, maxMultiplier: 3f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_AfterCaptures_RestoresOne()
        {
            var so = CreateSO(step: 0.5f, maxMultiplier: 5f);
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RewardForCapture_ZeroBase_ReturnsZero()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.RewardForCapture(0), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MultiplierSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MultiplierSO, Is.Null);
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
            typeof(ZoneControlCaptureMultiplierController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T407: <see cref="ZoneControlAdaptiveRewardSO"/> and
    /// <see cref="ZoneControlAdaptiveRewardController"/>.
    ///
    /// ZoneControlAdaptiveRewardTests (12):
    ///   SO_FreshInstance_CurrentScaleFactor_One            x1
    ///   SO_SetPerformanceRatio_Zero_UsesMinScale           x1
    ///   SO_SetPerformanceRatio_One_UsesMaxScale            x1
    ///   SO_SetPerformanceRatio_Half_InterpolatesScale      x1
    ///   SO_ApplyReward_ScalesAmount                        x1
    ///   SO_ApplyReward_ZeroAmount_ReturnsZero              x1
    ///   SO_Reset_RestoresScaleToOne                        x1
    ///   SO_SetPerformanceRatio_FiresEvent                  x1
    ///   Controller_FreshInstance_RewardSO_Null             x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow          x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow         x1
    ///   Controller_Refresh_NullSO_HidesPanel               x1
    /// </summary>
    public sealed class ZoneControlAdaptiveRewardTests
    {
        private static ZoneControlAdaptiveRewardSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlAdaptiveRewardSO>();

        private static ZoneControlAdaptiveRewardController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlAdaptiveRewardController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentScaleFactor_One()
        {
            var so = CreateSO();
            Assert.That(so.CurrentScaleFactor, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPerformanceRatio_Zero_UsesMinScale()
        {
            var so = CreateSO();
            so.SetPerformanceRatio(0f);
            Assert.That(so.CurrentScaleFactor, Is.EqualTo(so.MinScaleFactor).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPerformanceRatio_One_UsesMaxScale()
        {
            var so = CreateSO();
            so.SetPerformanceRatio(1f);
            Assert.That(so.CurrentScaleFactor, Is.EqualTo(so.MaxScaleFactor).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPerformanceRatio_Half_InterpolatesScale()
        {
            var   so       = CreateSO();
            float expected = (so.MinScaleFactor + so.MaxScaleFactor) * 0.5f;
            so.SetPerformanceRatio(0.5f);
            Assert.That(so.CurrentScaleFactor, Is.EqualTo(expected).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyReward_ScalesAmount()
        {
            var so = CreateSO();
            so.SetPerformanceRatio(1f);
            int scaled = so.ApplyReward(100);
            Assert.That(scaled, Is.EqualTo(Mathf.RoundToInt(100 * so.MaxScaleFactor)));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyReward_ZeroAmount_ReturnsZero()
        {
            var so = CreateSO();
            so.SetPerformanceRatio(1f);
            Assert.That(so.ApplyReward(0), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_RestoresScaleToOne()
        {
            var so = CreateSO();
            so.SetPerformanceRatio(1f);
            so.Reset();
            Assert.That(so.CurrentScaleFactor, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPerformanceRatio_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlAdaptiveRewardSO)
                .GetField("_onScaleChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetPerformanceRatio(0.7f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_FreshInstance_RewardSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RewardSO, Is.Null);
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
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlAdaptiveRewardController)
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

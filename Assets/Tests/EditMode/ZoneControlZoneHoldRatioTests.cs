using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T435: <see cref="ZoneControlZoneHoldRatioSO"/> and
    /// <see cref="ZoneControlZoneHoldRatioController"/>.
    ///
    /// ZoneControlZoneHoldRatioTests (12):
    ///   SO_FreshInstance_HoldRatio_Zero                             x1
    ///   SO_FreshInstance_IsRunning_False                            x1
    ///   SO_StartMatch_SetsRunning                                   x1
    ///   SO_Tick_NoMajority_AccruesTotalTimeOnly                     x1
    ///   SO_Tick_WithMajority_AccruesBothTimes                       x1
    ///   SO_HoldRatio_CalculatesCorrectly                            x1
    ///   SO_EndMatch_StopsAccrual                                    x1
    ///   SO_SetMajority_UpdatesFlag                                  x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_HoldRatioSO_Null                  x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlZoneHoldRatioTests
    {
        private static ZoneControlZoneHoldRatioSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneHoldRatioSO>();

        private static ZoneControlZoneHoldRatioController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneHoldRatioController>();
        }

        [Test]
        public void SO_FreshInstance_HoldRatio_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HoldRatio, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsRunning_False()
        {
            var so = CreateSO();
            Assert.That(so.IsRunning, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartMatch_SetsRunning()
        {
            var so = CreateSO();
            so.StartMatch();
            Assert.That(so.IsRunning, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_NoMajority_AccruesTotalTimeOnly()
        {
            var so = CreateSO();
            so.StartMatch();
            so.SetMajority(false);
            so.Tick(5f);
            Assert.That(so.TotalMatchTime, Is.EqualTo(5f).Within(0.001f));
            Assert.That(so.TotalHoldTime,  Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WithMajority_AccruesBothTimes()
        {
            var so = CreateSO();
            so.StartMatch();
            so.SetMajority(true);
            so.Tick(10f);
            Assert.That(so.TotalMatchTime, Is.EqualTo(10f).Within(0.001f));
            Assert.That(so.TotalHoldTime,  Is.EqualTo(10f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HoldRatio_CalculatesCorrectly()
        {
            var so = CreateSO();
            so.StartMatch();
            so.SetMajority(true);
            so.Tick(5f); // hold 5
            so.SetMajority(false);
            so.Tick(5f); // no hold 5 — total 10, hold 5
            Assert.That(so.HoldRatio, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EndMatch_StopsAccrual()
        {
            var so = CreateSO();
            so.StartMatch();
            so.SetMajority(true);
            so.Tick(5f);
            so.EndMatch();
            so.Tick(10f); // should not accrue after EndMatch
            Assert.That(so.TotalMatchTime, Is.EqualTo(5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetMajority_UpdatesFlag()
        {
            var so = CreateSO();
            so.StartMatch();
            Assert.That(so.HasMajority, Is.False);
            so.SetMajority(true);
            Assert.That(so.HasMajority, Is.True);
            so.SetMajority(false);
            Assert.That(so.HasMajority, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartMatch();
            so.SetMajority(true);
            so.Tick(10f);
            so.Reset();
            Assert.That(so.IsRunning,      Is.False);
            Assert.That(so.TotalMatchTime, Is.EqualTo(0f));
            Assert.That(so.TotalHoldTime,  Is.EqualTo(0f));
            Assert.That(so.HoldRatio,      Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HoldRatioSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HoldRatioSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
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
            typeof(ZoneControlZoneHoldRatioController)
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

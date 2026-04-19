using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T469: <see cref="ZoneControlMatchEndRushSO"/> and
    /// <see cref="ZoneControlMatchEndRushController"/>.
    ///
    /// ZoneControlMatchEndRushTests (12):
    ///   SO_FreshInstance_IsActive_False                                   x1
    ///   SO_FreshInstance_RushCaptureCount_Zero                            x1
    ///   SO_StartRush_SetsActive                                           x1
    ///   SO_StartRush_Idempotent                                           x1
    ///   SO_RecordCapture_WhenActive_ReturnsBonus                         x1
    ///   SO_RecordCapture_WhenInactive_ReturnsZero                        x1
    ///   SO_RecordCapture_IncrementsCount                                  x1
    ///   SO_EndRush_ClearsActive                                           x1
    ///   SO_EndRush_PreventsStartRushAfter                                 x1
    ///   SO_Reset_ClearsAll                                                x1
    ///   Controller_FreshInstance_RushSO_Null                              x1
    ///   Controller_Refresh_NullSO_HidesPanel                              x1
    /// </summary>
    public sealed class ZoneControlMatchEndRushTests
    {
        private static ZoneControlMatchEndRushSO CreateSO(
            float rushWindowSeconds = 30f, int bonusPerRushCapture = 75)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchEndRushSO>();
            var t = typeof(ZoneControlMatchEndRushSO);
            t.GetField("_rushWindowSeconds",   BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, rushWindowSeconds);
            t.GetField("_bonusPerRushCapture", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, bonusPerRushCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchEndRushController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchEndRushController>();
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RushCaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RushCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartRush_SetsActive()
        {
            var so = CreateSO();
            so.StartRush();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartRush_Idempotent()
        {
            var so = CreateSO();
            int fireCount = 0;
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchEndRushSO)
                .GetField("_onRushStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fireCount++);
            so.StartRush();
            so.StartRush();
            Assert.That(fireCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordCapture_WhenActive_ReturnsBonus()
        {
            var so = CreateSO(bonusPerRushCapture: 75);
            so.StartRush();
            int bonus = so.RecordCapture();
            Assert.That(bonus, Is.EqualTo(75));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_WhenInactive_ReturnsZero()
        {
            var so = CreateSO(bonusPerRushCapture: 75);
            int bonus = so.RecordCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCount()
        {
            var so = CreateSO();
            so.StartRush();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.RushCaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EndRush_ClearsActive()
        {
            var so = CreateSO();
            so.StartRush();
            so.EndRush();
            Assert.That(so.IsActive,  Is.False);
            Assert.That(so.HasEnded,  Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EndRush_PreventsStartRushAfter()
        {
            var so = CreateSO();
            so.StartRush();
            so.EndRush();
            so.StartRush();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bonusPerRushCapture: 75);
            so.StartRush();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.IsActive,         Is.False);
            Assert.That(so.HasEnded,         Is.False);
            Assert.That(so.RushCaptureCount, Is.EqualTo(0));
            Assert.That(so.TotalRushBonus,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RushSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RushSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchEndRushController)
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

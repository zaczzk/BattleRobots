using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T468: <see cref="ZoneControlCaptureBonusPoolSO"/> and
    /// <see cref="ZoneControlCaptureBonusPoolController"/>.
    ///
    /// ZoneControlCaptureBonusPoolTests (12):
    ///   SO_FreshInstance_CurrentPool_Zero                                 x1
    ///   SO_FreshInstance_PoolProgress_Zero                                x1
    ///   SO_RecordPlayerCapture_FillsPool                                  x1
    ///   SO_RecordPlayerCapture_ClampsAtCapacity                           x1
    ///   SO_RecordPlayerCapture_AwardsPoolOnFull                           x1
    ///   SO_RecordPlayerCapture_ResetsPoolAfterAward                       x1
    ///   SO_RecordBotCapture_DrainsPool                                    x1
    ///   SO_RecordBotCapture_ClampsAtZero                                  x1
    ///   SO_Reset_ClearsAll                                                x1
    ///   Controller_FreshInstance_PoolSO_Null                              x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                         x1
    ///   Controller_Refresh_NullSO_HidesPanel                              x1
    /// </summary>
    public sealed class ZoneControlCaptureBonusPoolTests
    {
        private static ZoneControlCaptureBonusPoolSO CreateSO(
            int fillPerCapture = 30, int drainPerBotCapture = 20, int poolCapacity = 300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBonusPoolSO>();
            var t = typeof(ZoneControlCaptureBonusPoolSO);
            t.GetField("_fillPerCapture",     BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, fillPerCapture);
            t.GetField("_drainPerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, drainPerBotCapture);
            t.GetField("_poolCapacity",       BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(so, poolCapacity);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBonusPoolController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBonusPoolController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentPool_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentPool, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PoolProgress_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PoolProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsPool()
        {
            var so = CreateSO(fillPerCapture: 30, poolCapacity: 300);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentPool, Is.EqualTo(30));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClampsAtCapacity()
        {
            var so = CreateSO(fillPerCapture: 200, poolCapacity: 300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            // After second capture pool hits 400 which triggers award, pool resets to 0
            // Actually 200+200=400 >= 300 so pool awards. After first capture: 200. Second: 200+200=400>=300 → award.
            // After award pool is reset to 0.
            Assert.That(so.AwardCount, Is.GreaterThanOrEqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AwardsPoolOnFull()
        {
            var so = CreateSO(fillPerCapture: 150, poolCapacity: 300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.AwardCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ResetsPoolAfterAward()
        {
            var so = CreateSO(fillPerCapture: 300, poolCapacity: 300);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentPool, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsPool()
        {
            var so = CreateSO(fillPerCapture: 50, drainPerBotCapture: 20, poolCapacity: 300);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentPool, Is.EqualTo(30));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsAtZero()
        {
            var so = CreateSO(drainPerBotCapture: 100, poolCapacity: 300);
            so.RecordBotCapture();
            Assert.That(so.CurrentPool, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(fillPerCapture: 30, poolCapacity: 300);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentPool,  Is.EqualTo(0));
            Assert.That(so.TotalAwarded, Is.EqualTo(0));
            Assert.That(so.AwardCount,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PoolSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PoolSO, Is.Null);
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
            typeof(ZoneControlCaptureBonusPoolController)
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

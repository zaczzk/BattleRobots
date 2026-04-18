using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T447: <see cref="ZoneControlFirstBloodBonusSO"/> and
    /// <see cref="ZoneControlFirstBloodController"/>.
    ///
    /// ZoneControlFirstBloodBonusTests (12):
    ///   SO_FreshInstance_FirstBloodFired_False                           x1
    ///   SO_RecordPlayerCapture_SetsFirstBloodFired                       x1
    ///   SO_RecordPlayerCapture_SetsPlayerWasFirst                        x1
    ///   SO_RecordPlayerCapture_ReturnsPlayerBonus                        x1
    ///   SO_RecordPlayerCapture_Idempotent_SecondCallReturnsZero          x1
    ///   SO_RecordBotCapture_SetsFirstBloodFired_NotPlayerFirst           x1
    ///   SO_RecordBotCapture_Idempotent_AfterPlayerCapture                x1
    ///   SO_RecordPlayerCapture_FiresOnFirstBloodPlayer                   x1
    ///   SO_Reset_ClearsAll                                               x1
    ///   Controller_FreshInstance_FirstBloodSO_Null                       x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                        x1
    ///   Controller_Refresh_NullSO_HidesPanel                             x1
    /// </summary>
    public sealed class ZoneControlFirstBloodBonusTests
    {
        private static ZoneControlFirstBloodBonusSO CreateSO(int playerBonus = 300, int botBonus = 0)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlFirstBloodBonusSO>();
            typeof(ZoneControlFirstBloodBonusSO)
                .GetField("_playerFirstBloodBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, playerBonus);
            typeof(ZoneControlFirstBloodBonusSO)
                .GetField("_botFirstBloodBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, botBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlFirstBloodController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlFirstBloodController>();
        }

        [Test]
        public void SO_FreshInstance_FirstBloodFired_False()
        {
            var so = CreateSO();
            Assert.That(so.FirstBloodFired, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SetsFirstBloodFired()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.FirstBloodFired, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SetsPlayerWasFirst()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerWasFirst, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsPlayerBonus()
        {
            var so    = CreateSO(playerBonus: 300);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Idempotent_SecondCallReturnsZero()
        {
            var so = CreateSO(playerBonus: 300);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SetsFirstBloodFired_NotPlayerFirst()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.FirstBloodFired, Is.True);
            Assert.That(so.PlayerWasFirst,  Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_Idempotent_AfterPlayerCapture()
        {
            var so = CreateSO(playerBonus: 300, botBonus: 100);
            so.RecordPlayerCapture();
            int bonus = so.RecordBotCapture();
            Assert.That(bonus,             Is.EqualTo(0));
            Assert.That(so.PlayerWasFirst, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresOnFirstBloodPlayer()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlFirstBloodBonusSO)
                .GetField("_onFirstBloodPlayer", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();

            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.FirstBloodFired, Is.False);
            Assert.That(so.PlayerWasFirst,  Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FirstBloodSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FirstBloodSO, Is.Null);
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
            typeof(ZoneControlFirstBloodController)
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

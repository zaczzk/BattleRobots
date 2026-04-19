using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePingPongTests
    {
        private static ZoneControlCapturePingPongSO CreateSO(int bonus = 125)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePingPongSO>();
            typeof(ZoneControlCapturePingPongSO)
                .GetField("_bonusPerPingPong", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePingPongController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePingPongController>();
        }

        [Test]
        public void SO_FreshInstance_PingPongCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PingPongCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstCapture_NoAlternation()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.PingPongCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerThenBot_IncrementsPingPong()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PingPongCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotThenPlayer_IncrementsPingPong()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PingPongCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SameSideTwice_NoPingPong()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PingPongCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FullAlternation_PlayerBotPlayerBot_TwoPingPong()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PingPongCount, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PingPong_AccumulatesBonus()
        {
            var so = CreateSO(bonus: 100);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.PingPongCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.HasFirst,          Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PingPongSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PingPongSO, Is.Null);
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
            typeof(ZoneControlCapturePingPongController)
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

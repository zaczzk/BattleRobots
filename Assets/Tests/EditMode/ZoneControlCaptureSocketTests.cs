using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSocketTests
    {
        private static ZoneControlCaptureSocketSO CreateSO(
            int connectionsNeeded = 5,
            int closePerBot       = 1,
            int bonusPerSession   = 1900)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSocketSO>();
            typeof(ZoneControlCaptureSocketSO)
                .GetField("_connectionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, connectionsNeeded);
            typeof(ZoneControlCaptureSocketSO)
                .GetField("_closePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, closePerBot);
            typeof(ZoneControlCaptureSocketSO)
                .GetField("_bonusPerSession", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSession);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSocketController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSocketController>();
        }

        [Test]
        public void SO_FreshInstance_Connections_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Connections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SessionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SessionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesConnections()
        {
            var so = CreateSO(connectionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Connections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(connectionsNeeded: 3, bonusPerSession: 1900);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(1900));
            Assert.That(so.SessionCount,  Is.EqualTo(1));
            Assert.That(so.Connections,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(connectionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesConnections()
        {
            var so = CreateSO(connectionsNeeded: 5, closePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Connections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(connectionsNeeded: 5, closePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Connections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ConnectionProgress_Clamped()
        {
            var so = CreateSO(connectionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ConnectionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSocketBound_FiresEvent()
        {
            var so    = CreateSO(connectionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSocketSO)
                .GetField("_onSocketBound", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(connectionsNeeded: 2, bonusPerSession: 1900);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Connections,       Is.EqualTo(0));
            Assert.That(so.SessionCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSessions_Accumulate()
        {
            var so = CreateSO(connectionsNeeded: 2, bonusPerSession: 1900);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SessionCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3800));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SocketSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SocketSO, Is.Null);
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
            typeof(ZoneControlCaptureSocketController)
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

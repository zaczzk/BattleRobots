using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLinkTests
    {
        private static ZoneControlCaptureLinkSO CreateSO(
            int connectionsNeeded = 5,
            int breakPerBot       = 1,
            int bonusPerList      = 2065)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLinkSO>();
            typeof(ZoneControlCaptureLinkSO)
                .GetField("_connectionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, connectionsNeeded);
            typeof(ZoneControlCaptureLinkSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureLinkSO)
                .GetField("_bonusPerList", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerList);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLinkController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLinkController>();
        }

        [Test]
        public void SO_FreshInstance_Connections_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Connections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ListCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ListCount, Is.EqualTo(0));
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
            var so    = CreateSO(connectionsNeeded: 3, bonusPerList: 2065);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(2065));
            Assert.That(so.ListCount,  Is.EqualTo(1));
            Assert.That(so.Connections, Is.EqualTo(0));
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
        public void SO_RecordBotCapture_BreaksConnections()
        {
            var so = CreateSO(connectionsNeeded: 5, breakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Connections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(connectionsNeeded: 5, breakPerBot: 10);
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
        public void SO_OnListFormed_FiresEvent()
        {
            var so    = CreateSO(connectionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLinkSO)
                .GetField("_onListFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(connectionsNeeded: 2, bonusPerList: 2065);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Connections,       Is.EqualTo(0));
            Assert.That(so.ListCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLists_Accumulate()
        {
            var so = CreateSO(connectionsNeeded: 2, bonusPerList: 2065);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ListCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4130));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LinkSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LinkSO, Is.Null);
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
            typeof(ZoneControlCaptureLinkController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRadarTests
    {
        private static ZoneControlCaptureRadarSO CreateSO(int windowCaptures = 6, int bonusPerPing = 260)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRadarSO>();
            typeof(ZoneControlCaptureRadarSO)
                .GetField("_windowCaptures", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, windowCaptures);
            typeof(ZoneControlCaptureRadarSO)
                .GetField("_bonusPerPing", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPing);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRadarController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRadarController>();
        }

        [Test]
        public void SO_FreshInstance_PingCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WindowCaptures_AllPlayer_AwardsPing()
        {
            var so = CreateSO(windowCaptures: 3, bonusPerPing: 260);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PingCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WindowCaptures_AllBot_NoPing()
        {
            var so = CreateSO(windowCaptures: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.PingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WindowCaptures_Tied_NoPing()
        {
            var so = CreateSO(windowCaptures: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.PingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WindowCaptures_PlayerMajority_AwardsPing()
        {
            var so = CreateSO(windowCaptures: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PingCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WindowCaptures_FiresEvent()
        {
            var so    = CreateSO(windowCaptures: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRadarSO)
                .GetField("_onRadarPing", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_WindowCaptures_ResetsWindow()
        {
            var so = CreateSO(windowCaptures: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.WindowPlayerCaptures, Is.EqualTo(0));
            Assert.That(so.WindowBotCaptures,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleWindows_AccumulatesPingCount()
        {
            var so = CreateSO(windowCaptures: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PingCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(windowCaptures: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PingCount,            Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Assert.That(so.WindowPlayerCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RadarSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RadarSO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureRadarController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHearthTests
    {
        private static ZoneControlCaptureHearthSO CreateSO(
            int capturesPerLog = 3,
            int maxLogs        = 5,
            int bonusPerIgnite = 500)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHearthSO>();
            typeof(ZoneControlCaptureHearthSO)
                .GetField("_capturesPerLog",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesPerLog);
            typeof(ZoneControlCaptureHearthSO)
                .GetField("_maxLogs",          BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxLogs);
            typeof(ZoneControlCaptureHearthSO)
                .GetField("_bonusPerIgnite",   BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIgnite);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHearthController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHearthController>();
        }

        [Test]
        public void SO_FreshInstance_RawCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RawCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LogCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LogCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRawCaptures()
        {
            var so = CreateSO(capturesPerLog: 3);
            so.RecordPlayerCapture();
            Assert.That(so.RawCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ConvertsToLog()
        {
            var so = CreateSO(capturesPerLog: 3, maxLogs: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.LogCount,    Is.EqualTo(1));
            Assert.That(so.RawCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IgnitesAtMaxLogs()
        {
            var so    = CreateSO(capturesPerLog: 1, maxLogs: 3, bonusPerIgnite: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(500));
            Assert.That(so.IgniteCount,  Is.EqualTo(1));
            Assert.That(so.LogCount,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhenBuilding()
        {
            var so    = CreateSO(capturesPerLog: 3, maxLogs: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ScattersLogs()
        {
            var so = CreateSO(capturesPerLog: 1, maxLogs: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LogCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsRawCaptures()
        {
            var so = CreateSO(capturesPerLog: 3, maxLogs: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RawCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LogProgress_Clamped()
        {
            var so = CreateSO(capturesPerLog: 1, maxLogs: 5);
            Assert.That(so.LogProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.LogProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnIgnite_FiresEvent()
        {
            var so    = CreateSO(capturesPerLog: 1, maxLogs: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHearthSO)
                .GetField("_onIgnite", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(capturesPerLog: 1, maxLogs: 2, bonusPerIgnite: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RawCaptures,       Is.EqualTo(0));
            Assert.That(so.LogCount,          Is.EqualTo(0));
            Assert.That(so.IgniteCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIgnites_Accumulate()
        {
            var so = CreateSO(capturesPerLog: 1, maxLogs: 2, bonusPerIgnite: 500);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IgniteCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1000));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HearthSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HearthSO, Is.Null);
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
            typeof(ZoneControlCaptureHearthController)
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

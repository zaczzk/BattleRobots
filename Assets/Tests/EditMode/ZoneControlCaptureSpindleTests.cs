using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpindleTests
    {
        private static ZoneControlCaptureSpindleSO CreateSO(
            int windsNeeded   = 7,
            int unravelPerBot = 2,
            int bonusPerBolt  = 1075)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpindleSO>();
            typeof(ZoneControlCaptureSpindleSO)
                .GetField("_windsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, windsNeeded);
            typeof(ZoneControlCaptureSpindleSO)
                .GetField("_unravelPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unravelPerBot);
            typeof(ZoneControlCaptureSpindleSO)
                .GetField("_bonusPerBolt", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBolt);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpindleController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpindleController>();
        }

        [Test]
        public void SO_FreshInstance_Winds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Winds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BoltCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BoltCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWinds()
        {
            var so = CreateSO(windsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Winds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WindsAtThreshold()
        {
            var so    = CreateSO(windsNeeded: 3, bonusPerBolt: 1075);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1075));
            Assert.That(so.BoltCount,   Is.EqualTo(1));
            Assert.That(so.Winds,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(windsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesWinds()
        {
            var so = CreateSO(windsNeeded: 7, unravelPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Winds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(windsNeeded: 7, unravelPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Winds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WindProgress_Clamped()
        {
            var so = CreateSO(windsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.WindProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpindleWound_FiresEvent()
        {
            var so    = CreateSO(windsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpindleSO)
                .GetField("_onSpindleWound", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(windsNeeded: 2, bonusPerBolt: 1075);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Winds,             Is.EqualTo(0));
            Assert.That(so.BoltCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBolts_Accumulate()
        {
            var so = CreateSO(windsNeeded: 2, bonusPerBolt: 1075);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BoltCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpindleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpindleSO, Is.Null);
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
            typeof(ZoneControlCaptureSpindleController)
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

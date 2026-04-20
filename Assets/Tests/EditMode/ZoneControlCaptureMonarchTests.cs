using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMonarchTests
    {
        private static ZoneControlCaptureMonarchSO CreateSO(
            int capturesForThrone = 4,
            int bonusPerTurn      = 60)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMonarchSO>();
            typeof(ZoneControlCaptureMonarchSO)
                .GetField("_capturesForThrone", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForThrone);
            typeof(ZoneControlCaptureMonarchSO)
                .GetField("_bonusPerTurn", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTurn);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMonarchController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMonarchController>();
        }

        [Test]
        public void SO_FreshInstance_IsOnThrone_False()
        {
            var so = CreateSO();
            Assert.That(so.IsOnThrone, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so = CreateSO(capturesForThrone: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_TakesThrone()
        {
            var so = CreateSO(capturesForThrone: 3);
            for (int i = 0; i < 3; i++) so.RecordPlayerCapture();
            Assert.That(so.IsOnThrone, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OnThrone_ReturnsBonusPerTurn()
        {
            var so = CreateSO(capturesForThrone: 2, bonusPerTurn: 60);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(60));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OnThrone_IncrementsTurnCount()
        {
            var so = CreateSO(capturesForThrone: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TurnCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BelowThreshold_DrainsBuildCount()
        {
            var so = CreateSO(capturesForThrone: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_OnThrone_Topples()
        {
            var so = CreateSO(capturesForThrone: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsOnThrone, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsBuildCountAtZero()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnThroneTaken_FiresEvent()
        {
            var so    = CreateSO(capturesForThrone: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMonarchSO)
                .GetField("_onThroneTaken", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_OnThroneToppled_FiresEvent()
        {
            var so    = CreateSO(capturesForThrone: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMonarchSO)
                .GetField("_onThroneToppled", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesForThrone: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsOnThrone,        Is.False);
            Assert.That(so.BuildCount,        Is.EqualTo(0));
            Assert.That(so.ThroneCount,       Is.EqualTo(0));
            Assert.That(so.TurnCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MonarchSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MonarchSO, Is.Null);
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
            typeof(ZoneControlCaptureMonarchController)
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

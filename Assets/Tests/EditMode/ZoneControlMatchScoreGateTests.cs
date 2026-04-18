using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T437: <see cref="ZoneControlMatchScoreGateSO"/> and
    /// <see cref="ZoneControlMatchScoreGateController"/>.
    ///
    /// ZoneControlMatchScoreGateTests (12):
    ///   SO_FreshInstance_GatePassed_False                           x1
    ///   SO_FreshInstance_CaptureCount_Zero                          x1
    ///   SO_RecordCapture_IncrementsCount                            x1
    ///   SO_RecordCapture_BelowTarget_GateNotPassed                  x1
    ///   SO_RecordCapture_AtTarget_GatePassed                        x1
    ///   SO_RecordCapture_Idempotent_AfterGatePassed                 x1
    ///   SO_RecordCapture_FiresEvent_OnGatePassed                    x1
    ///   SO_GateProgress_CalculatesCorrectly                         x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_GateSO_Null                       x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlMatchScoreGateTests
    {
        private static ZoneControlMatchScoreGateSO CreateSO(int target = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchScoreGateSO>();
            typeof(ZoneControlMatchScoreGateSO)
                .GetField("_gateTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, target);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchScoreGateController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchScoreGateController>();
        }

        [Test]
        public void SO_FreshInstance_GatePassed_False()
        {
            var so = CreateSO();
            Assert.That(so.GatePassed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.CaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowTarget_GateNotPassed()
        {
            var so = CreateSO(target: 5);
            for (int i = 0; i < 4; i++) so.RecordCapture();
            Assert.That(so.GatePassed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtTarget_GatePassed()
        {
            var so = CreateSO(target: 5);
            for (int i = 0; i < 5; i++) so.RecordCapture();
            Assert.That(so.GatePassed, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_Idempotent_AfterGatePassed()
        {
            var so = CreateSO(target: 3);
            for (int i = 0; i < 3; i++) so.RecordCapture();
            int countAfterPass = so.CaptureCount;
            so.RecordCapture(); // should not increment after gate passed
            Assert.That(so.CaptureCount, Is.EqualTo(countAfterPass));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresEvent_OnGatePassed()
        {
            var so      = CreateSO(target: 2);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchScoreGateSO)
                .GetField("_onGatePassed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCapture();
            Assert.That(fired, Is.EqualTo(0));
            so.RecordCapture(); // meets target
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GateProgress_CalculatesCorrectly()
        {
            var so = CreateSO(target: 4);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.GateProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(target: 2);
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.GatePassed,   Is.False);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Assert.That(so.GateProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GateSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GateSO, Is.Null);
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
            typeof(ZoneControlMatchScoreGateController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T427: <see cref="ZoneControlZoneThreatLevelSO"/> and
    /// <see cref="ZoneControlZoneThreatLevelController"/>.
    ///
    /// ZoneControlZoneThreatLevelTests (12):
    ///   SO_FreshInstance_ThreatValue_Zero                         x1
    ///   SO_FreshInstance_CurrentThreat_Low                        x1
    ///   SO_RecordBotCapture_IncreasesThreatValue                  x1
    ///   SO_RecordPlayerCapture_ReducesThreatValue                 x1
    ///   SO_RecordBotCapture_ReachesThreshold_ChangesThreat        x1
    ///   SO_RecordBotCapture_FiresThreatChangedEvent               x1
    ///   SO_Tick_DecaysThreat                                      x1
    ///   SO_Reset_ClearsThreat                                     x1
    ///   SO_GetThreatLabel_LowByDefault                            x1
    ///   SO_ThreatProgress_ClampedToOne                            x1
    ///   Controller_FreshInstance_ThreatLevelSO_Null               x1
    ///   Controller_Refresh_NullSO_HidesPanel                      x1
    /// </summary>
    public sealed class ZoneControlZoneThreatLevelTests
    {
        private static ZoneControlZoneThreatLevelSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneThreatLevelSO>();

        private static ZoneControlZoneThreatLevelController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneThreatLevelController>();
        }

        [Test]
        public void SO_FreshInstance_ThreatValue_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ThreatValue, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentThreat_Low()
        {
            var so = CreateSO();
            Assert.That(so.CurrentThreat, Is.EqualTo(ZoneControlThreatLevel.Low));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncreasesThreatValue()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.ThreatValue, Is.GreaterThan(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReducesThreatValue()
        {
            var so = CreateSO();
            so.RecordBotCapture(); // raise threat first
            float before = so.ThreatValue;
            so.RecordPlayerCapture();
            Assert.That(so.ThreatValue, Is.LessThan(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReachesThreshold_ChangesThreat()
        {
            var so = CreateSO();
            // Set thresholds low so two bot captures reach Medium
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_threatIncreasePerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 30f);
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_mediumThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 30f);
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_highThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 70f);

            so.RecordBotCapture(); // threat = 30 → reaches Medium threshold exactly
            Assert.That(so.CurrentThreat, Is.EqualTo(ZoneControlThreatLevel.Medium));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresThreatChangedEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_onThreatChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_threatIncreasePerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 50f);
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_mediumThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 40f);
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_highThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 80f);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotCapture(); // 50 >= 40 → Medium → fires

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_DecaysThreat()
        {
            var so = CreateSO();
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_threatIncreasePerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 50f);
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_threatDecayRate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 10f);

            so.RecordBotCapture(); // threat = 50
            float before = so.ThreatValue;
            so.Tick(2f); // decays by 20
            Assert.That(so.ThreatValue, Is.EqualTo(before - 20f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsThreat()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.ThreatValue,   Is.EqualTo(0f));
            Assert.That(so.CurrentThreat, Is.EqualTo(ZoneControlThreatLevel.Low));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetThreatLabel_LowByDefault()
        {
            var so = CreateSO();
            Assert.That(so.GetThreatLabel(), Is.EqualTo("LOW"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ThreatProgress_ClampedToOne()
        {
            var so = CreateSO();
            typeof(ZoneControlZoneThreatLevelSO)
                .GetField("_threatIncreasePerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 200f); // exceeds 100 cap

            so.RecordBotCapture();
            Assert.That(so.ThreatProgress, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ThreatLevelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ThreatLevelSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneThreatLevelController)
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

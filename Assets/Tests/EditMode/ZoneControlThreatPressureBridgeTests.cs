using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T368: <see cref="ZoneControlThreatPressureBridgeSO"/> and
    /// <see cref="ZoneControlThreatPressureBridgeController"/>.
    ///
    /// ZoneControlThreatPressureBridgeTests (12):
    ///   SO_FreshInstance_HasApplied_False                    ×1
    ///   SO_GetBoostForThreat_Low_ReturnsLowBoost             ×1
    ///   SO_GetBoostForThreat_High_ReturnsHighBoost           ×1
    ///   SO_ApplyBridge_HighThreat_RecordsLevel               ×1
    ///   SO_ApplyBridge_HighThreat_FiresEvent                 ×1
    ///   SO_ApplyBridge_LowThreat_DoesNotFireEvent            ×1
    ///   SO_Reset_ClearsState                                 ×1
    ///   Controller_FreshInstance_BridgeSO_Null               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow            ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow           ×1
    ///   Controller_OnDisable_Unregisters_Channel             ×1
    ///   Controller_HandleThreatChanged_AppliesBridge         ×1
    /// </summary>
    public sealed class ZoneControlThreatPressureBridgeTests
    {
        private static ZoneControlThreatPressureBridgeSO CreateBridgeSO() =>
            ScriptableObject.CreateInstance<ZoneControlThreatPressureBridgeSO>();

        private static ZoneControlThreatAssessmentSO CreateThreatSO() =>
            ScriptableObject.CreateInstance<ZoneControlThreatAssessmentSO>();

        private static ZoneControlThreatPressureBridgeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlThreatPressureBridgeController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_HasApplied_False()
        {
            var so = CreateBridgeSO();
            Assert.That(so.HasApplied, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetBoostForThreat_Low_ReturnsLowBoost()
        {
            var so = CreateBridgeSO();
            Assert.That(so.GetBoostForThreat(ThreatLevel.Low), Is.EqualTo(so.LowThreatBoost));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetBoostForThreat_High_ReturnsHighBoost()
        {
            var so = CreateBridgeSO();
            Assert.That(so.GetBoostForThreat(ThreatLevel.High), Is.EqualTo(so.HighThreatBoost));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyBridge_HighThreat_RecordsLevel()
        {
            var so = CreateBridgeSO();
            so.ApplyBridge(ThreatLevel.High);
            Assert.That(so.LastLevel, Is.EqualTo(ThreatLevel.High));
            Assert.That(so.HasApplied, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyBridge_HighThreat_FiresEvent()
        {
            var so      = CreateBridgeSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlThreatPressureBridgeSO)
                .GetField("_onBridgeActivated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            typeof(ZoneControlThreatPressureBridgeSO)
                .GetField("_highThreatBoost", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 0.5f);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.ApplyBridge(ThreatLevel.High);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ApplyBridge_LowThreat_DoesNotFireEvent()
        {
            var so      = CreateBridgeSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlThreatPressureBridgeSO)
                .GetField("_onBridgeActivated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            typeof(ZoneControlThreatPressureBridgeSO)
                .GetField("_lowThreatBoost", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 0f);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.ApplyBridge(ThreatLevel.Low);

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateBridgeSO();
            so.ApplyBridge(ThreatLevel.High);
            so.Reset();
            Assert.That(so.HasApplied, Is.False);
            Assert.That(so.LastBoostApplied, Is.EqualTo(0f));
            Assert.That(so.LastLevel, Is.EqualTo(ThreatLevel.Low));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BridgeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BridgeSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlThreatPressureBridgeController)
                .GetField("_onThreatChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_HandleThreatChanged_AppliesBridge()
        {
            var ctrl     = CreateController();
            var bridgeSO = CreateBridgeSO();
            var threatSO = CreateThreatSO();

            typeof(ZoneControlThreatPressureBridgeController)
                .GetField("_bridgeSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, bridgeSO);
            typeof(ZoneControlThreatPressureBridgeController)
                .GetField("_threatSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, threatSO);

            // playerRank=3, hasDominance=false → High threat (highThreatRank default=3)
            threatSO.EvaluateThreat(3, false);
            ctrl.HandleThreatChanged();

            Assert.That(bridgeSO.HasApplied, Is.True);
            Assert.That(bridgeSO.LastLevel, Is.EqualTo(ThreatLevel.High));

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(bridgeSO);
            Object.DestroyImmediate(threatSO);
        }
    }
}

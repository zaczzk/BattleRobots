using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T452: <see cref="ZoneControlMatchVolatilitySO"/> and
    /// <see cref="ZoneControlMatchVolatilityController"/>.
    ///
    /// ZoneControlMatchVolatilityTests (12):
    ///   SO_FreshInstance_LeadChanges_Zero                                   x1
    ///   SO_FreshInstance_IsHighVolatility_False                             x1
    ///   SO_RecordLeadChange_FirstCall_Silent                                x1
    ///   SO_RecordLeadChange_SameDirection_Silent                            x1
    ///   SO_RecordLeadChange_OppositeDirection_IncrementsChanges             x1
    ///   SO_RecordLeadChange_AtThreshold_FiresOnHighVolatility               x1
    ///   SO_RecordLeadChange_IdempotentAboveThreshold                        x1
    ///   SO_Reset_ClearsAll                                                  x1
    ///   SO_FiresOnVolatilityUpdated_OnLeadChange                            x1
    ///   Controller_FreshInstance_VolatilitySO_Null                          x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlMatchVolatilityTests
    {
        private static ZoneControlMatchVolatilitySO CreateSO(int threshold = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchVolatilitySO>();
            typeof(ZoneControlMatchVolatilitySO)
                .GetField("_volatilityThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchVolatilityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchVolatilityController>();
        }

        [Test]
        public void SO_FreshInstance_LeadChanges_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LeadChanges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsHighVolatility_False()
        {
            var so = CreateSO();
            Assert.That(so.IsHighVolatility, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadChange_FirstCall_Silent()
        {
            var so = CreateSO(threshold: 5);
            so.RecordLeadChange(true);
            Assert.That(so.LeadChanges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadChange_SameDirection_Silent()
        {
            var so = CreateSO(threshold: 5);
            so.RecordLeadChange(true);
            so.RecordLeadChange(true);
            Assert.That(so.LeadChanges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadChange_OppositeDirection_IncrementsChanges()
        {
            var so = CreateSO(threshold: 5);
            so.RecordLeadChange(true);
            so.RecordLeadChange(false);
            Assert.That(so.LeadChanges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadChange_AtThreshold_FiresOnHighVolatility()
        {
            var so      = CreateSO(threshold: 2);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchVolatilitySO)
                .GetField("_onHighVolatility", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordLeadChange(true);
            so.RecordLeadChange(false);
            so.RecordLeadChange(true);

            Assert.That(so.LeadChanges,      Is.EqualTo(2));
            Assert.That(so.IsHighVolatility, Is.True);
            Assert.That(fired,               Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordLeadChange_IdempotentAboveThreshold()
        {
            var so      = CreateSO(threshold: 1);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchVolatilitySO)
                .GetField("_onHighVolatility", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordLeadChange(true);
            so.RecordLeadChange(false);
            so.RecordLeadChange(true);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(threshold: 1);
            so.RecordLeadChange(true);
            so.RecordLeadChange(false);
            so.Reset();
            Assert.That(so.LeadChanges,      Is.EqualTo(0));
            Assert.That(so.IsHighVolatility, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FiresOnVolatilityUpdated_OnLeadChange()
        {
            var so      = CreateSO(threshold: 10);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchVolatilitySO)
                .GetField("_onVolatilityUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordLeadChange(true);
            so.RecordLeadChange(false);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_FreshInstance_VolatilitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.VolatilitySO, Is.Null);
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
            typeof(ZoneControlMatchVolatilityController)
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

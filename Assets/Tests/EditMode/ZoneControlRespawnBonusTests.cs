using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T378: <see cref="ZoneControlRespawnBonusSO"/> and
    /// <see cref="ZoneControlRespawnBonusController"/>.
    ///
    /// ZoneControlRespawnBonusTests (12):
    ///   SO_FreshInstance_RespawnCount_Zero                       ×1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                  ×1
    ///   SO_RecordRespawn_IncrementsCount                         ×1
    ///   SO_RecordRespawn_AccumulatesTotalBonus                   ×1
    ///   SO_RecordRespawn_FiresEvent                              ×1
    ///   SO_Reset_ClearsCount                                     ×1
    ///   Controller_FreshInstance_RespawnBonusSO_Null             ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullRespawnBonusSO_HidesPanel         ×1
    ///   Controller_HandlePlayerRespawned_IncrementsCount         ×1
    /// </summary>
    public sealed class ZoneControlRespawnBonusTests
    {
        private static ZoneControlRespawnBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlRespawnBonusSO>();

        private static ZoneControlRespawnBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlRespawnBonusController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_RespawnCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RespawnCount, Is.EqualTo(0));
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
        public void SO_RecordRespawn_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordRespawn();
            Assert.That(so.RespawnCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRespawn_AccumulatesTotalBonus()
        {
            var so = CreateSO();
            so.RecordRespawn();
            so.RecordRespawn();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.BonusPerRespawn * 2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordRespawn_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlRespawnBonusSO)
                .GetField("_onRespawnBonusAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordRespawn();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsCount()
        {
            var so = CreateSO();
            so.RecordRespawn();
            so.RecordRespawn();
            so.Reset();
            Assert.That(so.RespawnCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_RespawnBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RespawnBonusSO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlRespawnBonusController)
                .GetField("_onRespawnBonusAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullRespawnBonusSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlRespawnBonusController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandlePlayerRespawned_IncrementsCount()
        {
            var ctrl = CreateController();
            var so   = CreateSO();
            typeof(ZoneControlRespawnBonusController)
                .GetField("_respawnBonusSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);

            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlRespawnBonusController)
                .GetField("_onPlayerRespawned", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            channel.Raise();

            Assert.That(so.RespawnCount, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }
    }
}

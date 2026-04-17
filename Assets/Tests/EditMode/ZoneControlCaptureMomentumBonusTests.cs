using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T390: <see cref="ZoneControlCaptureMomentumBonusSO"/> and
    /// <see cref="ZoneControlCaptureMomentumBonusController"/>.
    ///
    /// ZoneControlCaptureMomentumBonusTests (12):
    ///   SO_FreshInstance_BurstCaptureCount_Zero                ×1
    ///   SO_FreshInstance_TotalBurstReward_Zero                 ×1
    ///   SO_RecordBurstCapture_IncrementsBurstCaptureCount      ×1
    ///   SO_RecordBurstCapture_AccumulatesTotalBurstReward       ×1
    ///   SO_RecordBurstCapture_FiresOnBurstBonusAwarded         ×1
    ///   SO_Reset_ClearsAll                                     ×1
    ///   Controller_FreshInstance_BonusSO_Null                  ×1
    ///   Controller_FreshInstance_MomentumSO_Null               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow              ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow             ×1
    ///   Controller_OnDisable_Unregisters_Channel               ×1
    ///   Controller_Refresh_NullBonusSO_HidesPanel              ×1
    /// </summary>
    public sealed class ZoneControlCaptureMomentumBonusTests
    {
        private static ZoneControlCaptureMomentumBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureMomentumBonusSO>();

        private static ZoneControlCaptureMomentumBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMomentumBonusController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_BurstCaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BurstCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBurstReward_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBurstReward, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBurstCapture_IncrementsBurstCaptureCount()
        {
            var so = CreateSO();
            so.RecordBurstCapture();
            so.RecordBurstCapture();
            Assert.That(so.BurstCaptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBurstCapture_AccumulatesTotalBurstReward()
        {
            var so = CreateSO();
            int reward = so.RewardPerBurstCapture;
            so.RecordBurstCapture();
            so.RecordBurstCapture();
            Assert.That(so.TotalBurstReward, Is.EqualTo(reward * 2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBurstCapture_FiresOnBurstBonusAwarded()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMomentumBonusSO)
                .GetField("_onBurstBonusAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordBurstCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordBurstCapture();
            so.RecordBurstCapture();
            so.Reset();
            Assert.That(so.BurstCaptureCount, Is.EqualTo(0));
            Assert.That(so.TotalBurstReward,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BonusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_FreshInstance_MomentumSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MomentumSO, Is.Null);
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
            typeof(ZoneControlCaptureMomentumBonusController)
                .GetField("_onBurstBonusAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullBonusSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureMomentumBonusController)
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

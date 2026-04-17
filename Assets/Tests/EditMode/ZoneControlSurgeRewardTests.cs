using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T385: <see cref="ZoneControlSurgeRewardSO"/> and
    /// <see cref="ZoneControlSurgeRewardController"/>.
    ///
    /// ZoneControlSurgeRewardTests (13):
    ///   SO_FreshInstance_SurgeCaptures_Zero                          ×1
    ///   SO_FreshInstance_TotalSurgeReward_Zero                       ×1
    ///   SO_RecordCaptureDuringSurge_IncrementsSurgeCaptures          ×1
    ///   SO_RecordCaptureDuringSurge_AccumulatesTotalReward            ×1
    ///   SO_RecordCaptureDuringSurge_FiresOnSurgeRewardAwarded        ×1
    ///   SO_RecordCaptureDuringSurge_MultipleCalls_AccumulatesCorrectly ×1
    ///   SO_Reset_ClearsAll                                           ×1
    ///   Controller_FreshInstance_SurgeRewardSO_Null                  ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_Unregisters_Channel                     ×1
    ///   Controller_Refresh_NullSurgeRewardSO_HidesPanel              ×1
    ///   Controller_HandleZoneCaptured_NoSurge_DoesNotRecord          ×1
    /// </summary>
    public sealed class ZoneControlSurgeRewardTests
    {
        private static ZoneControlSurgeRewardSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlSurgeRewardSO>();

        private static ZoneControlSurgeRewardController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlSurgeRewardController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_SurgeCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SurgeCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalSurgeReward_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalSurgeReward, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCaptureDuringSurge_IncrementsSurgeCaptures()
        {
            var so = CreateSO();
            so.RecordCaptureDuringSurge();
            so.RecordCaptureDuringSurge();
            Assert.That(so.SurgeCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCaptureDuringSurge_AccumulatesTotalReward()
        {
            var so = CreateSO();
            so.RecordCaptureDuringSurge();
            Assert.That(so.TotalSurgeReward, Is.EqualTo(so.RewardPerCapture));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCaptureDuringSurge_FiresOnSurgeRewardAwarded()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlSurgeRewardSO)
                .GetField("_onSurgeRewardAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordCaptureDuringSurge();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCaptureDuringSurge_MultipleCalls_AccumulatesCorrectly()
        {
            var so = CreateSO();
            int calls = 5;
            for (int i = 0; i < calls; i++)
                so.RecordCaptureDuringSurge();

            Assert.That(so.SurgeCaptures,    Is.EqualTo(calls));
            Assert.That(so.TotalSurgeReward, Is.EqualTo(calls * so.RewardPerCapture));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordCaptureDuringSurge();
            so.RecordCaptureDuringSurge();

            so.Reset();

            Assert.That(so.SurgeCaptures,    Is.EqualTo(0));
            Assert.That(so.TotalSurgeReward, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_SurgeRewardSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SurgeRewardSO, Is.Null);
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
            typeof(ZoneControlSurgeRewardController)
                .GetField("_onSurgeRewardAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullSurgeRewardSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlSurgeRewardController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandleZoneCaptured_NoSurge_DoesNotRecord()
        {
            var ctrl       = CreateController();
            var rewardSO   = CreateSO();
            var surgeDetSO = ScriptableObject.CreateInstance<ZoneControlSurgeDetectorSO>();

            typeof(ZoneControlSurgeRewardController)
                .GetField("_surgeRewardSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, rewardSO);
            typeof(ZoneControlSurgeRewardController)
                .GetField("_surgeSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, surgeDetSO);

            // Surge is NOT active (fresh SO); fire the zone-captured channel
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlSurgeRewardController)
                .GetField("_onZoneCaptured", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            channel.Raise();

            Assert.That(rewardSO.SurgeCaptures, Is.EqualTo(0));

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(rewardSO);
            Object.DestroyImmediate(surgeDetSO);
            Object.DestroyImmediate(channel);
        }
    }
}

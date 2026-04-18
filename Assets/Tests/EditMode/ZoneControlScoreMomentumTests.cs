using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T412: <see cref="ZoneControlScoreMomentumSO"/> and
    /// <see cref="ZoneControlScoreMomentumController"/>.
    ///
    /// ZoneControlScoreMomentumTests (12):
    ///   SO_FreshInstance_IsLeading_False                    x1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero             x1
    ///   SO_SetLeading_True_FiresMomentumGained              x1
    ///   SO_SetLeading_False_FromTrue_FiresMomentumLost      x1
    ///   SO_SetLeading_Idempotent_DoesNotFireTwice           x1
    ///   SO_Tick_WhenNotLeading_DoesNotAdvance               x1
    ///   SO_Tick_CompletesInterval_FiresBonus                x1
    ///   SO_Tick_AccumulatesTotalBonus                       x1
    ///   SO_Reset_ClearsAll                                  x1
    ///   Controller_FreshInstance_MomentumSO_Null            x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow           x1
    ///   Controller_Refresh_NullSO_HidesPanel                x1
    /// </summary>
    public sealed class ZoneControlScoreMomentumTests
    {
        private static ZoneControlScoreMomentumSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlScoreMomentumSO>();

        private static ZoneControlScoreMomentumController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlScoreMomentumController>();
        }

        [Test]
        public void SO_FreshInstance_IsLeading_False()
        {
            var so = CreateSO();
            Assert.That(so.IsLeading, Is.False);
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
        public void SO_SetLeading_True_FiresMomentumGained()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoreMomentumSO)
                .GetField("_onMomentumGained", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetLeading(true);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_SetLeading_False_FromTrue_FiresMomentumLost()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoreMomentumSO)
                .GetField("_onMomentumLost", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetLeading(true);
            so.SetLeading(false);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_SetLeading_Idempotent_DoesNotFireTwice()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoreMomentumSO)
                .GetField("_onMomentumGained", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetLeading(true);
            so.SetLeading(true);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_WhenNotLeading_DoesNotAdvance()
        {
            var so = CreateSO();
            so.Tick(100f);
            Assert.That(so.ElapsedWhileLeading, Is.EqualTo(0f));
            Assert.That(so.IntervalsCompleted,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_CompletesInterval_FiresBonus()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoreMomentumSO)
                .GetField("_onMomentumBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetLeading(true);
            so.Tick(so.BonusInterval);
            Assert.That(fired,                Is.EqualTo(1));
            Assert.That(so.IntervalsCompleted, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_AccumulatesTotalBonus()
        {
            var so = CreateSO();
            so.SetLeading(true);
            so.Tick(so.BonusInterval * 2f);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.BonusPerInterval * 2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.SetLeading(true);
            so.Tick(so.BonusInterval * 2f);
            so.Reset();
            Assert.That(so.IsLeading,           Is.False);
            Assert.That(so.ElapsedWhileLeading, Is.EqualTo(0f));
            Assert.That(so.IntervalsCompleted,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
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
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlScoreMomentumController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureShardTests
    {
        private static ZoneControlCaptureShardSO CreateSO(
            int shardsNeeded            = 5,
            int shatterPerBot           = 1,
            int bonusPerCrystallization = 835)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureShardSO>();
            typeof(ZoneControlCaptureShardSO)
                .GetField("_shardsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, shardsNeeded);
            typeof(ZoneControlCaptureShardSO)
                .GetField("_shatterPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, shatterPerBot);
            typeof(ZoneControlCaptureShardSO)
                .GetField("_bonusPerCrystallization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCrystallization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureShardController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureShardController>();
        }

        [Test]
        public void SO_FreshInstance_Shards_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Shards, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CrystallizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CrystallizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesShards()
        {
            var so = CreateSO(shardsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Shards, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CrystallizesAtThreshold()
        {
            var so    = CreateSO(shardsNeeded: 3, bonusPerCrystallization: 835);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(835));
            Assert.That(so.CrystallizationCount,  Is.EqualTo(1));
            Assert.That(so.Shards,                Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(shardsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ShatterShards()
        {
            var so = CreateSO(shardsNeeded: 5, shatterPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Shards, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(shardsNeeded: 5, shatterPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Shards, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ShardProgress_Clamped()
        {
            var so = CreateSO(shardsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ShardProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnShardCrystallized_FiresEvent()
        {
            var so    = CreateSO(shardsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureShardSO)
                .GetField("_onShardCrystallized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(shardsNeeded: 2, bonusPerCrystallization: 835);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Shards,                Is.EqualTo(0));
            Assert.That(so.CrystallizationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCrystallizations_Accumulate()
        {
            var so = CreateSO(shardsNeeded: 2, bonusPerCrystallization: 835);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CrystallizationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(1670));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ShardSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ShardSO, Is.Null);
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
            typeof(ZoneControlCaptureShardController)
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

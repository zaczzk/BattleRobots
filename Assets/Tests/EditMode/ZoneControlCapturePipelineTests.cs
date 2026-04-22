using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePipelineTests
    {
        private static ZoneControlCapturePipelineSO CreateSO(
            int stagesNeeded     = 5,
            int flushPerBot      = 1,
            int bonusPerPipeline = 1780)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePipelineSO>();
            typeof(ZoneControlCapturePipelineSO)
                .GetField("_stagesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stagesNeeded);
            typeof(ZoneControlCapturePipelineSO)
                .GetField("_flushPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, flushPerBot);
            typeof(ZoneControlCapturePipelineSO)
                .GetField("_bonusPerPipeline", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPipeline);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePipelineController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePipelineController>();
        }

        [Test]
        public void SO_FreshInstance_Stages_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Stages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FlushCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FlushCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStages()
        {
            var so = CreateSO(stagesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Stages, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(stagesNeeded: 3, bonusPerPipeline: 1780);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1780));
            Assert.That(so.FlushCount,   Is.EqualTo(1));
            Assert.That(so.Stages,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stagesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesStages()
        {
            var so = CreateSO(stagesNeeded: 5, flushPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stages, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stagesNeeded: 5, flushPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StageProgress_Clamped()
        {
            var so = CreateSO(stagesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StageProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPipelineFlushed_FiresEvent()
        {
            var so    = CreateSO(stagesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePipelineSO)
                .GetField("_onPipelineFlushed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stagesNeeded: 2, bonusPerPipeline: 1780);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Stages,            Is.EqualTo(0));
            Assert.That(so.FlushCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFlushes_Accumulate()
        {
            var so = CreateSO(stagesNeeded: 2, bonusPerPipeline: 1780);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FlushCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3560));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PipelineSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PipelineSO, Is.Null);
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
            typeof(ZoneControlCapturePipelineController)
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

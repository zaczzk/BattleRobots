using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTidalTests
    {
        private static ZoneControlCaptureTidalSO CreateSO(int bonusPerCycle = 250)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTidalSO>();
            typeof(ZoneControlCaptureTidalSO)
                .GetField("_bonusPerCycle", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCycle);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTidalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTidalController>();
        }

        [Test]
        public void SO_FreshInstance_CycleCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CycleCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstLeadState_SetsPhase_PlayerLead()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            Assert.That(so.Phase, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstLeadState_SetsPhase_BotLead()
        {
            var so = CreateSO();
            so.RecordLeadState(false);
            Assert.That(so.Phase, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerThenBot_PhaseTwo()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            Assert.That(so.Phase, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FullCycle_IncrementsCycleCount()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            Assert.That(so.CycleCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FullCycle_AccumulatesBonus()
        {
            var so = CreateSO(bonusPerCycle: 300);
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FullCycle_FiresEvent()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTidalSO)
                .GetField("_onTidalCycle", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_SameState_NoOp()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            int cyclesBefore = so.CycleCount;
            so.RecordLeadState(true);
            Assert.That(so.CycleCount, Is.EqualTo(cyclesBefore));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            so.Reset();
            Assert.That(so.CycleCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.Phase,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TidalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TidalSO, Is.Null);
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
            typeof(ZoneControlCaptureTidalController)
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

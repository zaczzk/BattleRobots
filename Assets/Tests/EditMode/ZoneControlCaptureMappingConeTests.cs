using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMappingConeTests
    {
        private static ZoneControlCaptureMappingConeSO CreateSO(
            int chainMapsNeeded = 5,
            int breakPerBot     = 1,
            int bonusPerCone    = 3775)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMappingConeSO>();
            typeof(ZoneControlCaptureMappingConeSO)
                .GetField("_chainMapsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chainMapsNeeded);
            typeof(ZoneControlCaptureMappingConeSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureMappingConeSO)
                .GetField("_bonusPerCone", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCone);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMappingConeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMappingConeController>();
        }

        [Test]
        public void SO_FreshInstance_ChainMaps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChainMaps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesChainMaps()
        {
            var so = CreateSO(chainMapsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ChainMaps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(chainMapsNeeded: 3, bonusPerCone: 3775);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3775));
            Assert.That(so.ConeCount,   Is.EqualTo(1));
            Assert.That(so.ChainMaps,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(chainMapsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BreaksExactness()
        {
            var so = CreateSO(chainMapsNeeded: 5, breakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ChainMaps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chainMapsNeeded: 5, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ChainMaps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChainMapProgress_Clamped()
        {
            var so = CreateSO(chainMapsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ChainMapProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMappingConeConed_FiresEvent()
        {
            var so    = CreateSO(chainMapsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMappingConeSO)
                .GetField("_onMappingConeConed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chainMapsNeeded: 2, bonusPerCone: 3775);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ChainMaps,         Is.EqualTo(0));
            Assert.That(so.ConeCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCones_Accumulate()
        {
            var so = CreateSO(chainMapsNeeded: 2, bonusPerCone: 3775);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConeCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7550));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MappingConeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MappingConeSO, Is.Null);
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
            typeof(ZoneControlCaptureMappingConeController)
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

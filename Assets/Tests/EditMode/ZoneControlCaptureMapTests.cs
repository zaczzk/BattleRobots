using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMapTests
    {
        private static ZoneControlCaptureMapSO CreateSO(
            int mappingsNeeded = 5,
            int unmapPerBot    = 1,
            int bonusPerMap    = 2125)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMapSO>();
            typeof(ZoneControlCaptureMapSO)
                .GetField("_mappingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, mappingsNeeded);
            typeof(ZoneControlCaptureMapSO)
                .GetField("_unmapPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unmapPerBot);
            typeof(ZoneControlCaptureMapSO)
                .GetField("_bonusPerMap", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerMap);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMapController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMapController>();
        }

        [Test]
        public void SO_FreshInstance_Mappings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Mappings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MapCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MapCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMappings()
        {
            var so = CreateSO(mappingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Mappings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(mappingsNeeded: 3, bonusPerMap: 2125);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(2125));
            Assert.That(so.MapCount,  Is.EqualTo(1));
            Assert.That(so.Mappings,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(mappingsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_UnmapsMappings()
        {
            var so = CreateSO(mappingsNeeded: 5, unmapPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Mappings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(mappingsNeeded: 5, unmapPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Mappings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MappingProgress_Clamped()
        {
            var so = CreateSO(mappingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.MappingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMapBuilt_FiresEvent()
        {
            var so    = CreateSO(mappingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMapSO)
                .GetField("_onMapBuilt", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(mappingsNeeded: 2, bonusPerMap: 2125);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Mappings,          Is.EqualTo(0));
            Assert.That(so.MapCount,          Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMaps_Accumulate()
        {
            var so = CreateSO(mappingsNeeded: 2, bonusPerMap: 2125);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.MapCount,          Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MapSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MapSO, Is.Null);
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
            typeof(ZoneControlCaptureMapController)
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

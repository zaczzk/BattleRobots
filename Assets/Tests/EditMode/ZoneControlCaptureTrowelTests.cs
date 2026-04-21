using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTrowelTests
    {
        private static ZoneControlCaptureTrowelSO CreateSO(
            int layersNeeded  = 6,
            int seepagePerBot = 2,
            int bonusPerSet   = 1060)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTrowelSO>();
            typeof(ZoneControlCaptureTrowelSO)
                .GetField("_layersNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, layersNeeded);
            typeof(ZoneControlCaptureTrowelSO)
                .GetField("_seepagePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, seepagePerBot);
            typeof(ZoneControlCaptureTrowelSO)
                .GetField("_bonusPerSet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSet);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTrowelController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTrowelController>();
        }

        [Test]
        public void SO_FreshInstance_Layers_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Layers, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SetCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SetCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLayers()
        {
            var so = CreateSO(layersNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Layers, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_LayersAtThreshold()
        {
            var so    = CreateSO(layersNeeded: 3, bonusPerSet: 1060);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(1060));
            Assert.That(so.SetCount,  Is.EqualTo(1));
            Assert.That(so.Layers,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(layersNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesLayers()
        {
            var so = CreateSO(layersNeeded: 6, seepagePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Layers, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(layersNeeded: 6, seepagePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Layers, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LayerProgress_Clamped()
        {
            var so = CreateSO(layersNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.LayerProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTrowelSet_FiresEvent()
        {
            var so    = CreateSO(layersNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTrowelSO)
                .GetField("_onTrowelSet", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(layersNeeded: 2, bonusPerSet: 1060);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Layers,            Is.EqualTo(0));
            Assert.That(so.SetCount,          Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSets_Accumulate()
        {
            var so = CreateSO(layersNeeded: 2, bonusPerSet: 1060);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SetCount,          Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2120));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TrowelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TrowelSO, Is.Null);
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
            typeof(ZoneControlCaptureTrowelController)
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

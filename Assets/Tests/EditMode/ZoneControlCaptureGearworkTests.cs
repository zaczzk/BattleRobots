using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGearworkTests
    {
        private static ZoneControlCaptureGearworkSO CreateSO(
            int gearsNeeded  = 6,
            int slipPerBot   = 2,
            int bonusPerMesh = 1180)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGearworkSO>();
            typeof(ZoneControlCaptureGearworkSO)
                .GetField("_gearsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, gearsNeeded);
            typeof(ZoneControlCaptureGearworkSO)
                .GetField("_slipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slipPerBot);
            typeof(ZoneControlCaptureGearworkSO)
                .GetField("_bonusPerMesh", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerMesh);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGearworkController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGearworkController>();
        }

        [Test]
        public void SO_FreshInstance_Gears_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Gears, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MeshCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MeshCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGears()
        {
            var so = CreateSO(gearsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Gears, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_GearsAtThreshold()
        {
            var so    = CreateSO(gearsNeeded: 3, bonusPerMesh: 1180);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(1180));
            Assert.That(so.MeshCount, Is.EqualTo(1));
            Assert.That(so.Gears,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(gearsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesGears()
        {
            var so = CreateSO(gearsNeeded: 6, slipPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gears, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(gearsNeeded: 6, slipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gears, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GearProgress_Clamped()
        {
            var so = CreateSO(gearsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.GearProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGearworkMeshed_FiresEvent()
        {
            var so    = CreateSO(gearsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGearworkSO)
                .GetField("_onGearworkMeshed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(gearsNeeded: 2, bonusPerMesh: 1180);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Gears,             Is.EqualTo(0));
            Assert.That(so.MeshCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMeshes_Accumulate()
        {
            var so = CreateSO(gearsNeeded: 2, bonusPerMesh: 1180);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.MeshCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2360));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GearworkSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GearworkSO, Is.Null);
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
            typeof(ZoneControlCaptureGearworkController)
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

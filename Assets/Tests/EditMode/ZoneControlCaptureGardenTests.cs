using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGardenTests
    {
        private static ZoneControlCaptureGardenSO CreateSO(
            int bloomsNeeded  = 5,
            int wiltPerBot    = 1,
            int bonusPerBloom = 535)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGardenSO>();
            typeof(ZoneControlCaptureGardenSO)
                .GetField("_bloomsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bloomsNeeded);
            typeof(ZoneControlCaptureGardenSO)
                .GetField("_wiltPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wiltPerBot);
            typeof(ZoneControlCaptureGardenSO)
                .GetField("_bonusPerBloom", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBloom);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGardenController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGardenController>();
        }

        [Test]
        public void SO_FreshInstance_Blooms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Blooms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GardenCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GardenCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBlooms()
        {
            var so = CreateSO(bloomsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Blooms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BloomsAtThreshold()
        {
            var so    = CreateSO(bloomsNeeded: 3, bonusPerBloom: 535);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(535));
            Assert.That(so.GardenCount, Is.EqualTo(1));
            Assert.That(so.Blooms,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bloomsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WiltsBlooms()
        {
            var so = CreateSO(bloomsNeeded: 5, wiltPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Blooms, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bloomsNeeded: 5, wiltPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Blooms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BloomProgress_Clamped()
        {
            var so = CreateSO(bloomsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BloomProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGardenBloomed_FiresEvent()
        {
            var so    = CreateSO(bloomsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGardenSO)
                .GetField("_onGardenBloomed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bloomsNeeded: 2, bonusPerBloom: 535);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Blooms,            Is.EqualTo(0));
            Assert.That(so.GardenCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGardens_Accumulate()
        {
            var so = CreateSO(bloomsNeeded: 2, bonusPerBloom: 535);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GardenCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1070));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GardenSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GardenSO, Is.Null);
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
            typeof(ZoneControlCaptureGardenController)
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

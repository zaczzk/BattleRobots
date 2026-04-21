using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureChiselTests
    {
        private static ZoneControlCaptureChiselSO CreateSO(
            int carvingsNeeded = 5,
            int erosionPerBot  = 1,
            int bonusPerSculpt = 1015)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureChiselSO>();
            typeof(ZoneControlCaptureChiselSO)
                .GetField("_carvingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, carvingsNeeded);
            typeof(ZoneControlCaptureChiselSO)
                .GetField("_erosionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, erosionPerBot);
            typeof(ZoneControlCaptureChiselSO)
                .GetField("_bonusPerSculpt", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSculpt);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureChiselController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureChiselController>();
        }

        [Test]
        public void SO_FreshInstance_Carvings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Carvings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SculptCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SculptCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCarvings()
        {
            var so = CreateSO(carvingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Carvings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CarvingsAtThreshold()
        {
            var so    = CreateSO(carvingsNeeded: 3, bonusPerSculpt: 1015);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1015));
            Assert.That(so.SculptCount,  Is.EqualTo(1));
            Assert.That(so.Carvings,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(carvingsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCarvings()
        {
            var so = CreateSO(carvingsNeeded: 5, erosionPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Carvings, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(carvingsNeeded: 5, erosionPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Carvings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CarvingProgress_Clamped()
        {
            var so = CreateSO(carvingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CarvingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnChiselSculpted_FiresEvent()
        {
            var so    = CreateSO(carvingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureChiselSO)
                .GetField("_onChiselSculpted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(carvingsNeeded: 2, bonusPerSculpt: 1015);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Carvings,          Is.EqualTo(0));
            Assert.That(so.SculptCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSculpts_Accumulate()
        {
            var so = CreateSO(carvingsNeeded: 2, bonusPerSculpt: 1015);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SculptCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2030));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ChiselSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChiselSO, Is.Null);
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
            typeof(ZoneControlCaptureChiselController)
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

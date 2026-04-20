using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePhoenixTests
    {
        private static ZoneControlCapturePhoenixSO CreateSO(
            int ashesNeeded     = 5,
            int scatterPerBot   = 1,
            int bonusPerRebirth = 595)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePhoenixSO>();
            typeof(ZoneControlCapturePhoenixSO)
                .GetField("_ashesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ashesNeeded);
            typeof(ZoneControlCapturePhoenixSO)
                .GetField("_scatterPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, scatterPerBot);
            typeof(ZoneControlCapturePhoenixSO)
                .GetField("_bonusPerRebirth", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRebirth);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePhoenixController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePhoenixController>();
        }

        [Test]
        public void SO_FreshInstance_Ashes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ashes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RebirthCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RebirthCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesAshes()
        {
            var so = CreateSO(ashesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Ashes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RebornAtThreshold()
        {
            var so    = CreateSO(ashesNeeded: 3, bonusPerRebirth: 595);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(595));
            Assert.That(so.RebirthCount,  Is.EqualTo(1));
            Assert.That(so.Ashes,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ashesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ScattersAshes()
        {
            var so = CreateSO(ashesNeeded: 5, scatterPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ashes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ashesNeeded: 5, scatterPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ashes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AshProgress_Clamped()
        {
            var so = CreateSO(ashesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.AshProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPhoenixReborn_FiresEvent()
        {
            var so    = CreateSO(ashesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePhoenixSO)
                .GetField("_onPhoenixReborn", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ashesNeeded: 2, bonusPerRebirth: 595);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ashes,             Is.EqualTo(0));
            Assert.That(so.RebirthCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRebirths_Accumulate()
        {
            var so = CreateSO(ashesNeeded: 2, bonusPerRebirth: 595);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RebirthCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1190));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PhoenixSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PhoenixSO, Is.Null);
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
            typeof(ZoneControlCapturePhoenixController)
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

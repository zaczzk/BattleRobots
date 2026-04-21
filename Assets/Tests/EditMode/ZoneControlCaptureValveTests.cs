using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureValveTests
    {
        private static ZoneControlCaptureValveSO CreateSO(
            int stemsNeeded  = 5,
            int leakPerBot   = 1,
            int bonusPerOpen = 1285)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureValveSO>();
            typeof(ZoneControlCaptureValveSO)
                .GetField("_stemsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stemsNeeded);
            typeof(ZoneControlCaptureValveSO)
                .GetField("_leakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, leakPerBot);
            typeof(ZoneControlCaptureValveSO)
                .GetField("_bonusPerOpen", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerOpen);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureValveController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureValveController>();
        }

        [Test]
        public void SO_FreshInstance_Stems_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Stems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_OpenCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OpenCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStems()
        {
            var so = CreateSO(stemsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Stems, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_StemsAtThreshold()
        {
            var so    = CreateSO(stemsNeeded: 3, bonusPerOpen: 1285);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1285));
            Assert.That(so.OpenCount,   Is.EqualTo(1));
            Assert.That(so.Stems,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(stemsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesStems()
        {
            var so = CreateSO(stemsNeeded: 5, leakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stems, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(stemsNeeded: 5, leakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Stems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StemProgress_Clamped()
        {
            var so = CreateSO(stemsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StemProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnValveOpened_FiresEvent()
        {
            var so    = CreateSO(stemsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureValveSO)
                .GetField("_onValveOpened", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(stemsNeeded: 2, bonusPerOpen: 1285);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Stems,             Is.EqualTo(0));
            Assert.That(so.OpenCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleOpens_Accumulate()
        {
            var so = CreateSO(stemsNeeded: 2, bonusPerOpen: 1285);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.OpenCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2570));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ValveSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ValveSO, Is.Null);
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
            typeof(ZoneControlCaptureValveController)
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

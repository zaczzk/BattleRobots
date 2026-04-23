using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGerbeTests
    {
        private static ZoneControlCaptureGerbeSO CreateSO(
            int torsorsNeeded  = 5,
            int deflectPerBot  = 1,
            int bonusPerBinding = 2590)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGerbeSO>();
            typeof(ZoneControlCaptureGerbeSO)
                .GetField("_torsorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, torsorsNeeded);
            typeof(ZoneControlCaptureGerbeSO)
                .GetField("_deflectPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, deflectPerBot);
            typeof(ZoneControlCaptureGerbeSO)
                .GetField("_bonusPerBinding", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBinding);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGerbeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGerbeController>();
        }

        [Test]
        public void SO_FreshInstance_Torsors_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Torsors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BindingCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BindingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTorsors()
        {
            var so = CreateSO(torsorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Torsors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(torsorsNeeded: 3, bonusPerBinding: 2590);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(2590));
            Assert.That(so.BindingCount,  Is.EqualTo(1));
            Assert.That(so.Torsors,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(torsorsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesTorsors()
        {
            var so = CreateSO(torsorsNeeded: 5, deflectPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Torsors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(torsorsNeeded: 5, deflectPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Torsors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TorsorProgress_Clamped()
        {
            var so = CreateSO(torsorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.TorsorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGerbeBound_FiresEvent()
        {
            var so    = CreateSO(torsorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGerbeSO)
                .GetField("_onGerbeBound", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(torsorsNeeded: 2, bonusPerBinding: 2590);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Torsors,           Is.EqualTo(0));
            Assert.That(so.BindingCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBindings_Accumulate()
        {
            var so = CreateSO(torsorsNeeded: 2, bonusPerBinding: 2590);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BindingCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5180));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GerbeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GerbeSO, Is.Null);
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
            typeof(ZoneControlCaptureGerbeController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureUltrafilterTests
    {
        private static ZoneControlCaptureUltrafilterSO CreateSO(
            int ultrasetsNeeded   = 5,
            int dilutePerBot      = 1,
            int bonusPerUltrafine = 3415)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureUltrafilterSO>();
            typeof(ZoneControlCaptureUltrafilterSO)
                .GetField("_ultrasetsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ultrasetsNeeded);
            typeof(ZoneControlCaptureUltrafilterSO)
                .GetField("_dilutePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dilutePerBot);
            typeof(ZoneControlCaptureUltrafilterSO)
                .GetField("_bonusPerUltrafine", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerUltrafine);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureUltrafilterController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureUltrafilterController>();
        }

        [Test]
        public void SO_FreshInstance_Ultrasets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ultrasets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_UltrafineCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.UltrafineCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesUltrasets()
        {
            var so = CreateSO(ultrasetsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Ultrasets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(ultrasetsNeeded: 3, bonusPerUltrafine: 3415);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3415));
            Assert.That(so.UltrafineCount,  Is.EqualTo(1));
            Assert.That(so.Ultrasets,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(ultrasetsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DilutesUltrasets()
        {
            var so = CreateSO(ultrasetsNeeded: 5, dilutePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ultrasets, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(ultrasetsNeeded: 5, dilutePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ultrasets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_UltrafilterProgress_Clamped()
        {
            var so = CreateSO(ultrasetsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.UltrafilterProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnUltrafilterRefined_FiresEvent()
        {
            var so    = CreateSO(ultrasetsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureUltrafilterSO)
                .GetField("_onUltrafilterRefined", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(ultrasetsNeeded: 2, bonusPerUltrafine: 3415);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ultrasets,         Is.EqualTo(0));
            Assert.That(so.UltrafineCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleUltrafines_Accumulate()
        {
            var so = CreateSO(ultrasetsNeeded: 2, bonusPerUltrafine: 3415);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.UltrafineCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6830));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_UltrafilterSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.UltrafilterSO, Is.Null);
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
            typeof(ZoneControlCaptureUltrafilterController)
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

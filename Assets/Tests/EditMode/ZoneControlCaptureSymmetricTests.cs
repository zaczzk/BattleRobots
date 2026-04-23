using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSymmetricTests
    {
        private static ZoneControlCaptureSymmetricSO CreateSO(
            int swapsNeeded      = 6,
            int transposePerBot  = 2,
            int bonusPerSymmetry = 3100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSymmetricSO>();
            typeof(ZoneControlCaptureSymmetricSO)
                .GetField("_swapsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, swapsNeeded);
            typeof(ZoneControlCaptureSymmetricSO)
                .GetField("_transposePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, transposePerBot);
            typeof(ZoneControlCaptureSymmetricSO)
                .GetField("_bonusPerSymmetry", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSymmetry);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSymmetricController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSymmetricController>();
        }

        [Test]
        public void SO_FreshInstance_Swaps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Swaps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SymmetryCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SymmetryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSwaps()
        {
            var so = CreateSO(swapsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Swaps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(swapsNeeded: 3, bonusPerSymmetry: 3100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(3100));
            Assert.That(so.SymmetryCount,  Is.EqualTo(1));
            Assert.That(so.Swaps,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(swapsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSwaps()
        {
            var so = CreateSO(swapsNeeded: 6, transposePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Swaps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(swapsNeeded: 6, transposePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Swaps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SwapProgress_Clamped()
        {
            var so = CreateSO(swapsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SwapProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSymmetrized_FiresEvent()
        {
            var so    = CreateSO(swapsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSymmetricSO)
                .GetField("_onSymmetrized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(swapsNeeded: 2, bonusPerSymmetry: 3100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Swaps,             Is.EqualTo(0));
            Assert.That(so.SymmetryCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSymmetries_Accumulate()
        {
            var so = CreateSO(swapsNeeded: 2, bonusPerSymmetry: 3100);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SymmetryCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SymmetricSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SymmetricSO, Is.Null);
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
            typeof(ZoneControlCaptureSymmetricController)
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

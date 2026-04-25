using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCantorDiagonalTests
    {
        private static ZoneControlCaptureCantorDiagonalSO CreateSO(
            int diagonalConstructionsNeeded = 6,
            int enumerationAttemptsPerBot   = 1,
            int bonusPerDiagonal            = 4780)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCantorDiagonalSO>();
            typeof(ZoneControlCaptureCantorDiagonalSO)
                .GetField("_diagonalConstructionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, diagonalConstructionsNeeded);
            typeof(ZoneControlCaptureCantorDiagonalSO)
                .GetField("_enumerationAttemptsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, enumerationAttemptsPerBot);
            typeof(ZoneControlCaptureCantorDiagonalSO)
                .GetField("_bonusPerDiagonal", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDiagonal);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCantorDiagonalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCantorDiagonalController>();
        }

        [Test]
        public void SO_FreshInstance_DiagonalConstructions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DiagonalConstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DiagonalCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DiagonalCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesConstructions()
        {
            var so = CreateSO(diagonalConstructionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DiagonalConstructions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(diagonalConstructionsNeeded: 3, bonusPerDiagonal: 4780);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(4780));
            Assert.That(so.DiagonalCount,         Is.EqualTo(1));
            Assert.That(so.DiagonalConstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(diagonalConstructionsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesEnumerationAttempts()
        {
            var so = CreateSO(diagonalConstructionsNeeded: 6, enumerationAttemptsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DiagonalConstructions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(diagonalConstructionsNeeded: 6, enumerationAttemptsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DiagonalConstructions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DiagonalConstructionProgress_Clamped()
        {
            var so = CreateSO(diagonalConstructionsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DiagonalConstructionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCantorDiagonalConstructed_FiresEvent()
        {
            var so    = CreateSO(diagonalConstructionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCantorDiagonalSO)
                .GetField("_onCantorDiagonalConstructed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(diagonalConstructionsNeeded: 2, bonusPerDiagonal: 4780);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.DiagonalConstructions, Is.EqualTo(0));
            Assert.That(so.DiagonalCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDiagonals_Accumulate()
        {
            var so = CreateSO(diagonalConstructionsNeeded: 2, bonusPerDiagonal: 4780);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DiagonalCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9560));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CantorDiagonalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CantorDiagonalSO, Is.Null);
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
            typeof(ZoneControlCaptureCantorDiagonalController)
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

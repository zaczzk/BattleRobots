using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBanachTarskiTests
    {
        private static ZoneControlCaptureBanachTarskiSO CreateSO(
            int decompositionPiecesNeeded      = 5,
            int unmeasurableObstructionsPerBot = 1,
            int bonusPerParadox                = 4765)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBanachTarskiSO>();
            typeof(ZoneControlCaptureBanachTarskiSO)
                .GetField("_decompositionPiecesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decompositionPiecesNeeded);
            typeof(ZoneControlCaptureBanachTarskiSO)
                .GetField("_unmeasurableObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unmeasurableObstructionsPerBot);
            typeof(ZoneControlCaptureBanachTarskiSO)
                .GetField("_bonusPerParadox", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerParadox);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBanachTarskiController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBanachTarskiController>();
        }

        [Test]
        public void SO_FreshInstance_DecompositionPieces_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DecompositionPieces, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ParadoxCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ParadoxCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPieces()
        {
            var so = CreateSO(decompositionPiecesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.DecompositionPieces, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(decompositionPiecesNeeded: 3, bonusPerParadox: 4765);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(4765));
            Assert.That(so.ParadoxCount,   Is.EqualTo(1));
            Assert.That(so.DecompositionPieces, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(decompositionPiecesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesObstructions()
        {
            var so = CreateSO(decompositionPiecesNeeded: 5, unmeasurableObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DecompositionPieces, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(decompositionPiecesNeeded: 5, unmeasurableObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DecompositionPieces, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DecompositionPieceProgress_Clamped()
        {
            var so = CreateSO(decompositionPiecesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.DecompositionPieceProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBanachTarskiParadoxCompleted_FiresEvent()
        {
            var so    = CreateSO(decompositionPiecesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBanachTarskiSO)
                .GetField("_onBanachTarskiParadoxCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(decompositionPiecesNeeded: 2, bonusPerParadox: 4765);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.DecompositionPieces, Is.EqualTo(0));
            Assert.That(so.ParadoxCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleParadoxes_Accumulate()
        {
            var so = CreateSO(decompositionPiecesNeeded: 2, bonusPerParadox: 4765);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ParadoxCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9530));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BanachTarskiSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BanachTarskiSO, Is.Null);
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
            typeof(ZoneControlCaptureBanachTarskiController)
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

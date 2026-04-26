using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHomotopyTypeTheoryTests
    {
        private static ZoneControlCaptureHomotopyTypeTheorySO CreateSO(
            int pathEquivalencesNeeded         = 6,
            int nonUnivalentObstructionsPerBot = 1,
            int bonusPerUnivalence             = 5035)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHomotopyTypeTheorySO>();
            typeof(ZoneControlCaptureHomotopyTypeTheorySO)
                .GetField("_pathEquivalencesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pathEquivalencesNeeded);
            typeof(ZoneControlCaptureHomotopyTypeTheorySO)
                .GetField("_nonUnivalentObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nonUnivalentObstructionsPerBot);
            typeof(ZoneControlCaptureHomotopyTypeTheorySO)
                .GetField("_bonusPerUnivalence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerUnivalence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHomotopyTypeTheoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHomotopyTypeTheoryController>();
        }

        [Test]
        public void SO_FreshInstance_PathEquivalences_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PathEquivalences, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_UnivalenceCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.UnivalenceCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPathEquivalences()
        {
            var so = CreateSO(pathEquivalencesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PathEquivalences, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(pathEquivalencesNeeded: 3, bonusPerUnivalence: 5035);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(5035));
            Assert.That(so.UnivalenceCount,   Is.EqualTo(1));
            Assert.That(so.PathEquivalences,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(pathEquivalencesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesObstructions()
        {
            var so = CreateSO(pathEquivalencesNeeded: 6, nonUnivalentObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PathEquivalences, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(pathEquivalencesNeeded: 6, nonUnivalentObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PathEquivalences, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PathEquivalenceProgress_Clamped()
        {
            var so = CreateSO(pathEquivalencesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PathEquivalenceProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHomotopyTypeTheoryCompleted_FiresEvent()
        {
            var so    = CreateSO(pathEquivalencesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHomotopyTypeTheorySO)
                .GetField("_onHomotopyTypeTheoryCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(pathEquivalencesNeeded: 2, bonusPerUnivalence: 5035);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PathEquivalences,  Is.EqualTo(0));
            Assert.That(so.UnivalenceCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleUnivalences_Accumulate()
        {
            var so = CreateSO(pathEquivalencesNeeded: 2, bonusPerUnivalence: 5035);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.UnivalenceCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10070));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HomotopyTypeTheorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HomotopyTypeTheorySO, Is.Null);
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
            typeof(ZoneControlCaptureHomotopyTypeTheoryController)
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

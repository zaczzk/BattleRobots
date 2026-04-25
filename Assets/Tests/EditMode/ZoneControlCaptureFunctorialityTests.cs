using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFunctorialityTests
    {
        private static ZoneControlCaptureFunctorialitySO CreateSO(
            int transfersNeeded            = 7,
            int ramifiedObstructionsPerBot = 2,
            int bonusPerRealization        = 4255)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFunctorialitySO>();
            typeof(ZoneControlCaptureFunctorialitySO)
                .GetField("_transfersNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, transfersNeeded);
            typeof(ZoneControlCaptureFunctorialitySO)
                .GetField("_ramifiedObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ramifiedObstructionsPerBot);
            typeof(ZoneControlCaptureFunctorialitySO)
                .GetField("_bonusPerRealization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRealization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFunctorialityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFunctorialityController>();
        }

        [Test]
        public void SO_FreshInstance_Transfers_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Transfers, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RealizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RealizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTransfers()
        {
            var so = CreateSO(transfersNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Transfers, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(transfersNeeded: 3, bonusPerRealization: 4255);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4255));
            Assert.That(so.RealizationCount, Is.EqualTo(1));
            Assert.That(so.Transfers,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(transfersNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesRamifiedObstructions()
        {
            var so = CreateSO(transfersNeeded: 7, ramifiedObstructionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Transfers, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(transfersNeeded: 7, ramifiedObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Transfers, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TransferProgress_Clamped()
        {
            var so = CreateSO(transfersNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.TransferProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFunctorialityRealized_FiresEvent()
        {
            var so    = CreateSO(transfersNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFunctorialitySO)
                .GetField("_onFunctorialityRealized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(transfersNeeded: 2, bonusPerRealization: 4255);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Transfers,         Is.EqualTo(0));
            Assert.That(so.RealizationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRealizations_Accumulate()
        {
            var so = CreateSO(transfersNeeded: 2, bonusPerRealization: 4255);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RealizationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8510));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FunctorialitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FunctorialitySO, Is.Null);
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
            typeof(ZoneControlCaptureFunctorialityController)
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

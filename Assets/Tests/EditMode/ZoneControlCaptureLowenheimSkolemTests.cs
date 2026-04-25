using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLowenheimSkolemTests
    {
        private static ZoneControlCaptureLowenheimSkolemSO CreateSO(
            int downwardWitnessesNeeded      = 6,
            int uncountableObstructionsPerBot = 1,
            int bonusPerWitnessing            = 4870)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLowenheimSkolemSO>();
            typeof(ZoneControlCaptureLowenheimSkolemSO)
                .GetField("_downwardWitnessesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, downwardWitnessesNeeded);
            typeof(ZoneControlCaptureLowenheimSkolemSO)
                .GetField("_uncountableObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, uncountableObstructionsPerBot);
            typeof(ZoneControlCaptureLowenheimSkolemSO)
                .GetField("_bonusPerWitnessing", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerWitnessing);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLowenheimSkolemController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLowenheimSkolemController>();
        }

        [Test]
        public void SO_FreshInstance_DownwardWitnesses_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DownwardWitnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WitnessingCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WitnessingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWitnesses()
        {
            var so = CreateSO(downwardWitnessesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DownwardWitnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(downwardWitnessesNeeded: 3, bonusPerWitnessing: 4870);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(4870));
            Assert.That(so.WitnessingCount, Is.EqualTo(1));
            Assert.That(so.DownwardWitnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(downwardWitnessesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesUncountableObstructions()
        {
            var so = CreateSO(downwardWitnessesNeeded: 6, uncountableObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DownwardWitnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(downwardWitnessesNeeded: 6, uncountableObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DownwardWitnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DownwardWitnessProgress_Clamped()
        {
            var so = CreateSO(downwardWitnessesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DownwardWitnessProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLowenheimSkolemWitnessed_FiresEvent()
        {
            var so    = CreateSO(downwardWitnessesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLowenheimSkolemSO)
                .GetField("_onLowenheimSkolemWitnessed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(downwardWitnessesNeeded: 2, bonusPerWitnessing: 4870);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.DownwardWitnesses, Is.EqualTo(0));
            Assert.That(so.WitnessingCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleWitnessings_Accumulate()
        {
            var so = CreateSO(downwardWitnessesNeeded: 2, bonusPerWitnessing: 4870);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.WitnessingCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9740));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LowenheimSkolemSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LowenheimSkolemSO, Is.Null);
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
            typeof(ZoneControlCaptureLowenheimSkolemController)
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

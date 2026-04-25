using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFermatLastTheoremTests
    {
        private static ZoneControlCaptureFermatLastTheoremSO CreateSO(
            int modularFormsNeeded         = 5,
            int faltingsObstructionsPerBot = 2,
            int bonusPerProof              = 4675)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFermatLastTheoremSO>();
            typeof(ZoneControlCaptureFermatLastTheoremSO)
                .GetField("_modularFormsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, modularFormsNeeded);
            typeof(ZoneControlCaptureFermatLastTheoremSO)
                .GetField("_faltingsObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, faltingsObstructionsPerBot);
            typeof(ZoneControlCaptureFermatLastTheoremSO)
                .GetField("_bonusPerProof", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerProof);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFermatLastTheoremController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFermatLastTheoremController>();
        }

        [Test]
        public void SO_FreshInstance_ModularForms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ModularForms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ProofCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ProofCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesModularForms()
        {
            var so = CreateSO(modularFormsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ModularForms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(modularFormsNeeded: 3, bonusPerProof: 4675);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(4675));
            Assert.That(so.ProofCount,   Is.EqualTo(1));
            Assert.That(so.ModularForms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(modularFormsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesFaltingsObstructions()
        {
            var so = CreateSO(modularFormsNeeded: 5, faltingsObstructionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ModularForms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(modularFormsNeeded: 5, faltingsObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ModularForms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ModularFormProgress_Clamped()
        {
            var so = CreateSO(modularFormsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ModularFormProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFermatLastTheoremProved_FiresEvent()
        {
            var so    = CreateSO(modularFormsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFermatLastTheoremSO)
                .GetField("_onFermatLastTheoremProved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(modularFormsNeeded: 2, bonusPerProof: 4675);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ModularForms,      Is.EqualTo(0));
            Assert.That(so.ProofCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleProofs_Accumulate()
        {
            var so = CreateSO(modularFormsNeeded: 2, bonusPerProof: 4675);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ProofCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9350));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FermatSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FermatSO, Is.Null);
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
            typeof(ZoneControlCaptureFermatLastTheoremController)
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

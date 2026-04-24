using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNucleusTests
    {
        private static ZoneControlCaptureNucleusSO CreateSO(
            int morphismsNeeded = 6,
            int contractPerBot  = 2,
            int bonusPerClosure = 3280)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNucleusSO>();
            typeof(ZoneControlCaptureNucleusSO)
                .GetField("_morphismsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, morphismsNeeded);
            typeof(ZoneControlCaptureNucleusSO)
                .GetField("_contractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, contractPerBot);
            typeof(ZoneControlCaptureNucleusSO)
                .GetField("_bonusPerClosure", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClosure);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNucleusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNucleusController>();
        }

        [Test]
        public void SO_FreshInstance_Morphisms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ClosureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ClosureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(morphismsNeeded: 3, bonusPerClosure: 3280);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3280));
            Assert.That(so.ClosureCount, Is.EqualTo(1));
            Assert.That(so.Morphisms,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(morphismsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 6, contractPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(morphismsNeeded: 6, contractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MorphismProgress_Clamped()
        {
            var so = CreateSO(morphismsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.MorphismProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnNucleusClosed_FiresEvent()
        {
            var so    = CreateSO(morphismsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNucleusSO)
                .GetField("_onNucleusClosed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(morphismsNeeded: 2, bonusPerClosure: 3280);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Morphisms,          Is.EqualTo(0));
            Assert.That(so.ClosureCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClosures_Accumulate()
        {
            var so = CreateSO(morphismsNeeded: 2, bonusPerClosure: 3280);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ClosureCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6560));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NucleusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NucleusSO, Is.Null);
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
            typeof(ZoneControlCaptureNucleusController)
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

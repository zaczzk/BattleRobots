using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAlgebraTests
    {
        private static ZoneControlCaptureAlgebraSO CreateSO(
            int termsNeeded  = 6,
            int breakPerBot  = 2,
            int bonusPerFold = 2500)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAlgebraSO>();
            typeof(ZoneControlCaptureAlgebraSO)
                .GetField("_termsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, termsNeeded);
            typeof(ZoneControlCaptureAlgebraSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureAlgebraSO)
                .GetField("_bonusPerFold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFold);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAlgebraController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAlgebraController>();
        }

        [Test]
        public void SO_FreshInstance_Terms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Terms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FoldCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FoldCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTerms()
        {
            var so = CreateSO(termsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Terms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(termsNeeded: 3, bonusPerFold: 2500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2500));
            Assert.That(so.FoldCount,   Is.EqualTo(1));
            Assert.That(so.Terms,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(termsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesTerms()
        {
            var so = CreateSO(termsNeeded: 6, breakPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Terms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(termsNeeded: 6, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Terms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TermProgress_Clamped()
        {
            var so = CreateSO(termsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TermProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAlgebraFolded_FiresEvent()
        {
            var so    = CreateSO(termsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAlgebraSO)
                .GetField("_onAlgebraFolded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(termsNeeded: 2, bonusPerFold: 2500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Terms,             Is.EqualTo(0));
            Assert.That(so.FoldCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFolds_Accumulate()
        {
            var so = CreateSO(termsNeeded: 2, bonusPerFold: 2500);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FoldCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5000));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AlgebraSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AlgebraSO, Is.Null);
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
            typeof(ZoneControlCaptureAlgebraController)
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

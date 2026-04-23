using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureClosedTests
    {
        private static ZoneControlCaptureClosedSO CreateSO(
            int homTermsNeeded = 6,
            int openPerBot     = 2,
            int bonusPerClose  = 3160)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureClosedSO>();
            typeof(ZoneControlCaptureClosedSO)
                .GetField("_homTermsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, homTermsNeeded);
            typeof(ZoneControlCaptureClosedSO)
                .GetField("_openPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, openPerBot);
            typeof(ZoneControlCaptureClosedSO)
                .GetField("_bonusPerClose", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClose);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureClosedController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureClosedController>();
        }

        [Test]
        public void SO_FreshInstance_HomTerms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HomTerms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CloseCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CloseCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesHomTerms()
        {
            var so = CreateSO(homTermsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.HomTerms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(homTermsNeeded: 3, bonusPerClose: 3160);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3160));
            Assert.That(so.CloseCount,  Is.EqualTo(1));
            Assert.That(so.HomTerms,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(homTermsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesHomTerms()
        {
            var so = CreateSO(homTermsNeeded: 6, openPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HomTerms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(homTermsNeeded: 6, openPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HomTerms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HomTermProgress_Clamped()
        {
            var so = CreateSO(homTermsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.HomTermProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnClosed_FiresEvent()
        {
            var so    = CreateSO(homTermsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureClosedSO)
                .GetField("_onClosed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(homTermsNeeded: 2, bonusPerClose: 3160);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.HomTerms,          Is.EqualTo(0));
            Assert.That(so.CloseCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClosures_Accumulate()
        {
            var so = CreateSO(homTermsNeeded: 2, bonusPerClose: 3160);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CloseCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6320));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ClosedSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ClosedSO, Is.Null);
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
            typeof(ZoneControlCaptureClosedController)
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

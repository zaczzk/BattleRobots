using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHeytingTests
    {
        private static ZoneControlCaptureHeytingSO CreateSO(
            int implicationsNeeded  = 7,
            int retractPerBot       = 2,
            int bonusPerImplication = 3235)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHeytingSO>();
            typeof(ZoneControlCaptureHeytingSO)
                .GetField("_implicationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, implicationsNeeded);
            typeof(ZoneControlCaptureHeytingSO)
                .GetField("_retractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, retractPerBot);
            typeof(ZoneControlCaptureHeytingSO)
                .GetField("_bonusPerImplication", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerImplication);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHeytingController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHeytingController>();
        }

        [Test]
        public void SO_FreshInstance_Implications_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Implications, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ImplicationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ImplicationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesImplications()
        {
            var so = CreateSO(implicationsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Implications, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(implicationsNeeded: 3, bonusPerImplication: 3235);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(3235));
            Assert.That(so.ImplicationCount,  Is.EqualTo(1));
            Assert.That(so.Implications,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(implicationsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesImplications()
        {
            var so = CreateSO(implicationsNeeded: 7, retractPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Implications, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(implicationsNeeded: 7, retractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Implications, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ImplicationProgress_Clamped()
        {
            var so = CreateSO(implicationsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ImplicationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnImplicationFormed_FiresEvent()
        {
            var so    = CreateSO(implicationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHeytingSO)
                .GetField("_onImplicationFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(implicationsNeeded: 2, bonusPerImplication: 3235);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Implications,      Is.EqualTo(0));
            Assert.That(so.ImplicationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleImplications_Accumulate()
        {
            var so = CreateSO(implicationsNeeded: 2, bonusPerImplication: 3235);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ImplicationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6470));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HeytingSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HeytingSO, Is.Null);
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
            typeof(ZoneControlCaptureHeytingController)
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

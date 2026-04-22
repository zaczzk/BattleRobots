using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureApplicativeTests
    {
        private static ZoneControlCaptureApplicativeSO CreateSO(
            int applicationsNeeded  = 5,
            int removePerBot        = 1,
            int bonusPerApplication = 2245)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureApplicativeSO>();
            typeof(ZoneControlCaptureApplicativeSO)
                .GetField("_applicationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, applicationsNeeded);
            typeof(ZoneControlCaptureApplicativeSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureApplicativeSO)
                .GetField("_bonusPerApplication", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerApplication);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureApplicativeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureApplicativeController>();
        }

        [Test]
        public void SO_FreshInstance_Applications_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Applications, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ApplyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ApplyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesApplications()
        {
            var so = CreateSO(applicationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Applications, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(applicationsNeeded: 3, bonusPerApplication: 2245);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2245));
            Assert.That(so.ApplyCount,   Is.EqualTo(1));
            Assert.That(so.Applications, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(applicationsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesApplications()
        {
            var so = CreateSO(applicationsNeeded: 5, removePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Applications, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(applicationsNeeded: 5, removePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Applications, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplicationProgress_Clamped()
        {
            var so = CreateSO(applicationsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ApplicationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnApplicativeApplied_FiresEvent()
        {
            var so    = CreateSO(applicationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureApplicativeSO)
                .GetField("_onApplicativeApplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(applicationsNeeded: 2, bonusPerApplication: 2245);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Applications,      Is.EqualTo(0));
            Assert.That(so.ApplyCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleApplications_Accumulate()
        {
            var so = CreateSO(applicationsNeeded: 2, bonusPerApplication: 2245);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ApplyCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4490));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ApplicativeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ApplicativeSO, Is.Null);
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
            typeof(ZoneControlCaptureApplicativeController)
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

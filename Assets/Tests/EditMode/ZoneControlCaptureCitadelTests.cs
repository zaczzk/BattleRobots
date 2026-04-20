using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCitadelTests
    {
        private static ZoneControlCaptureCitadelSO CreateSO(
            int capturesForCitadel    = 4,
            int bonusPerCaptureWithin = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCitadelSO>();
            typeof(ZoneControlCaptureCitadelSO)
                .GetField("_capturesForCitadel",    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForCitadel);
            typeof(ZoneControlCaptureCitadelSO)
                .GetField("_bonusPerCaptureWithin", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCaptureWithin);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCitadelController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCitadelController>();
        }

        [Test]
        public void SO_FreshInstance_IsCitadelBuilt_False()
        {
            var so = CreateSO();
            Assert.That(so.IsCitadelBuilt, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CitadelCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CitadelCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(capturesForCitadel: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_IncreasesProgress()
        {
            var so = CreateSO(capturesForCitadel: 4);
            so.RecordPlayerCapture();
            Assert.That(so.BuildCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesThreshold_BuildsCitadel()
        {
            var so = CreateSO(capturesForCitadel: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsCitadelBuilt, Is.True);
            Assert.That(so.CitadelCount,   Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenBuilt_ReturnsBonus()
        {
            var so = CreateSO(capturesForCitadel: 1, bonusPerCaptureWithin: 100);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Build_FiresEvent()
        {
            var so    = CreateSO(capturesForCitadel: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCitadelSO)
                .GetField("_onCitadelBuilt", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_WhenBuilt_DemolishesCitadel()
        {
            var so = CreateSO(capturesForCitadel: 1);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsCitadelBuilt, Is.False);
            Assert.That(so.BuildCount,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhenBuilt_FiresDemolishedEvent()
        {
            var so    = CreateSO(capturesForCitadel: 1);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCitadelSO)
                .GetField("_onCitadelDemolished", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_WhenNotBuilt_ReducesBuildCount()
        {
            var so = CreateSO(capturesForCitadel: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BuildProgress_ReflectsBuildRatio()
        {
            var so = CreateSO(capturesForCitadel: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BuildProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesForCitadel: 1, bonusPerCaptureWithin: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsCitadelBuilt,    Is.False);
            Assert.That(so.BuildCount,         Is.EqualTo(0));
            Assert.That(so.CitadelCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CitadelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CitadelSO, Is.Null);
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
            typeof(ZoneControlCaptureCitadelController)
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

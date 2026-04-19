using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureChronoTests
    {
        private static ZoneControlCaptureChronoSO CreateSO(float minGap = 2f, float maxGap = 30f, int bonusPerSec = 20)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureChronoSO>();
            typeof(ZoneControlCaptureChronoSO)
                .GetField("_minGapForBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, minGap);
            typeof(ZoneControlCaptureChronoSO)
                .GetField("_maxGapForBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxGap);
            typeof(ZoneControlCaptureChronoSO)
                .GetField("_bonusPerSecondOfGap", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSec);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureChronoController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureChronoController>();
        }

        [Test]
        public void SO_FreshInstance_BestGap_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BestGap, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HasFirstCapture_False()
        {
            var so = CreateSO();
            Assert.That(so.HasFirstCapture, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FirstCapture_SetsAnchor()
        {
            var so = CreateSO();
            int bonus = so.RecordCapture(10f);
            Assert.That(bonus,              Is.EqualTo(0));
            Assert.That(so.HasFirstCapture, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowMinGap_ReturnsZero()
        {
            var so = CreateSO(minGap: 5f);
            so.RecordCapture(0f);
            int bonus = so.RecordCapture(3f);
            Assert.That(bonus,                   Is.EqualTo(0));
            Assert.That(so.QualifyingCaptures,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AboveMinGap_ReturnsBonus()
        {
            var so = CreateSO(minGap: 2f, maxGap: 30f, bonusPerSec: 10);
            so.RecordCapture(0f);
            int bonus = so.RecordCapture(5f);
            Assert.That(bonus,                 Is.EqualTo(50));
            Assert.That(so.QualifyingCaptures, Is.EqualTo(1));
            Assert.That(so.TotalChronoBonus,   Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ClampedAtMaxGap()
        {
            var so = CreateSO(minGap: 1f, maxGap: 10f, bonusPerSec: 10);
            so.RecordCapture(0f);
            int bonus = so.RecordCapture(100f);
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_UpdatesBestGap()
        {
            var so = CreateSO(minGap: 1f);
            so.RecordCapture(0f);
            so.RecordCapture(5f);
            Assert.That(so.BestGap, Is.EqualTo(5f).Within(0.001f));
            so.RecordCapture(12f);
            Assert.That(so.BestGap, Is.EqualTo(7f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresEvent_OnNewBestGap()
        {
            var so    = CreateSO(minGap: 1f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureChronoSO)
                .GetField("_onChronoRecord", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture(0f);
            so.RecordCapture(10f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordCapture_NoEvent_WhenNotNewBest()
        {
            var so    = CreateSO(minGap: 1f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureChronoSO)
                .GetField("_onChronoRecord", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture(0f);
            so.RecordCapture(10f);
            so.RecordCapture(15f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(minGap: 1f);
            so.RecordCapture(0f);
            so.RecordCapture(10f);
            so.Reset();
            Assert.That(so.HasFirstCapture,    Is.False);
            Assert.That(so.BestGap,            Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.QualifyingCaptures, Is.EqualTo(0));
            Assert.That(so.TotalChronoBonus,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ChronoSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChronoSO, Is.Null);
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
            typeof(ZoneControlCaptureChronoController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureConstellationTests
    {
        private static ZoneControlCaptureConstellationSO CreateSO(
            int starsNeeded           = 6,
            int bonusPerConstellation = 450,
            int botScatterCount       = 2)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureConstellationSO>();
            typeof(ZoneControlCaptureConstellationSO)
                .GetField("_starsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, starsNeeded);
            typeof(ZoneControlCaptureConstellationSO)
                .GetField("_bonusPerConstellation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConstellation);
            typeof(ZoneControlCaptureConstellationSO)
                .GetField("_botScatterCount", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, botScatterCount);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureConstellationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureConstellationController>();
        }

        [Test]
        public void SO_FreshInstance_ConstellationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConstellationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(starsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_FormsConstellation()
        {
            var so = CreateSO(starsNeeded: 4);
            for (int i = 0; i < 4; i++) so.RecordPlayerCapture();
            Assert.That(so.ConstellationCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FormConstellation_ReturnsBonus()
        {
            var so = CreateSO(starsNeeded: 3, bonusPerConstellation: 450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(450));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FormConstellation_ResetsActiveStars()
        {
            var so = CreateSO(starsNeeded: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ActiveStars, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ScattersStars()
        {
            var so = CreateSO(starsNeeded: 6, botScatterCount: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ActiveStars, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtZero_ClampsToZero()
        {
            var so = CreateSO(botScatterCount: 2);
            so.RecordBotCapture();
            Assert.That(so.ActiveStars, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StarProgress_Clamped()
        {
            var so = CreateSO(starsNeeded: 6);
            Assert.That(so.StarProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.StarProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnConstellationFormed_FiresEvent()
        {
            var so    = CreateSO(starsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureConstellationSO)
                .GetField("_onConstellationFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(starsNeeded: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ActiveStars,        Is.EqualTo(0));
            Assert.That(so.ConstellationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConstellations_Accumulate()
        {
            var so = CreateSO(starsNeeded: 3);
            for (int i = 0; i < 9; i++) so.RecordPlayerCapture();
            Assert.That(so.ConstellationCount, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesAcrossConstellations()
        {
            var so = CreateSO(starsNeeded: 2, bonusPerConstellation: 100);
            for (int i = 0; i < 6; i++) so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ConstellationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ConstellationSO, Is.Null);
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
            typeof(ZoneControlCaptureConstellationController)
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

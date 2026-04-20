using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHarvestTests
    {
        private static ZoneControlCaptureHarvestSO CreateSO(
            int capturesPerSeason = 5,
            int seasonsForHarvest = 3,
            int bonusPerHarvest   = 400)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHarvestSO>();
            typeof(ZoneControlCaptureHarvestSO)
                .GetField("_capturesPerSeason", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesPerSeason);
            typeof(ZoneControlCaptureHarvestSO)
                .GetField("_seasonsForHarvest",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, seasonsForHarvest);
            typeof(ZoneControlCaptureHarvestSO)
                .GetField("_bonusPerHarvest",    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerHarvest);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHarvestController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHarvestController>();
        }

        [Test]
        public void SO_FreshInstance_SeasonCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SeasonCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SeasonCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SeasonCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncreasesSeasonCaptures()
        {
            var so = CreateSO(capturesPerSeason: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SeasonCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesSeasonAtThreshold()
        {
            var so = CreateSO(capturesPerSeason: 3, seasonsForHarvest: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.SeasonCount,    Is.EqualTo(1));
            Assert.That(so.SeasonCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_HarvestsAtMaxSeasons()
        {
            var so    = CreateSO(capturesPerSeason: 2, seasonsForHarvest: 3, bonusPerHarvest: 400);
            for (int i = 0; i < 5; i++) so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(400));
            Assert.That(so.HarvestCount, Is.EqualTo(1));
            Assert.That(so.SeasonCount,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhenBuilding()
        {
            var so    = CreateSO(capturesPerSeason: 5, seasonsForHarvest: 3);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesSeasonCaptures()
        {
            var so = CreateSO(capturesPerSeason: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SeasonCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsAtZero()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.SeasonCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SeasonProgress_Clamped()
        {
            var so = CreateSO(capturesPerSeason: 5);
            Assert.That(so.SeasonProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.SeasonProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHarvest_FiresEvent()
        {
            var so    = CreateSO(capturesPerSeason: 2, seasonsForHarvest: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHarvestSO)
                .GetField("_onHarvest", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            for (int i = 0; i < 4; i++) so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesPerSeason: 2, seasonsForHarvest: 2, bonusPerHarvest: 400);
            for (int i = 0; i < 5; i++) so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SeasonCaptures,    Is.EqualTo(0));
            Assert.That(so.SeasonCount,       Is.EqualTo(0));
            Assert.That(so.HarvestCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleHarvests_Accumulate()
        {
            var so = CreateSO(capturesPerSeason: 2, seasonsForHarvest: 2, bonusPerHarvest: 400);
            for (int i = 0; i < 8; i++) so.RecordPlayerCapture();
            Assert.That(so.HarvestCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(800));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HarvestSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HarvestSO, Is.Null);
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
            typeof(ZoneControlCaptureHarvestController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T277: ZoneControlSaveData integration.
    ///
    /// Covers the three file patches:
    ///   • <see cref="SaveData"/> — two new career zone score fields.
    ///   • <see cref="ZoneScoreTrackerSO"/> — LoadSnapshot / AccumulateToCareer /
    ///     CareerPlayerScore / CareerEnemyScore.
    ///   • <see cref="GameBootstrapper"/> — new optional _zoneScoreTracker field.
    ///
    /// Tests (12):
    ///   SaveData_CareerPlayerZoneScore_DefaultZero                         ×1
    ///   SaveData_CareerEnemyZoneScore_DefaultZero                         ×1
    ///   ZoneScoreTracker_CareerPlayerScore_DefaultZero                    ×1
    ///   ZoneScoreTracker_CareerEnemyScore_DefaultZero                     ×1
    ///   ZoneScoreTracker_AccumulateToCareer_AddsPlayerScore               ×1
    ///   ZoneScoreTracker_AccumulateToCareer_AddsEnemyScore                ×1
    ///   ZoneScoreTracker_AccumulateToCareer_Additive_MultipleMatches      ×1
    ///   ZoneScoreTracker_LoadSnapshot_SetsCareerPlayerScore               ×1
    ///   ZoneScoreTracker_LoadSnapshot_SetsCareerEnemyScore                ×1
    ///   ZoneScoreTracker_LoadSnapshot_ClampsNegativeToZero                ×1
    ///   ZoneScoreTracker_Reset_DoesNotAffectCareerTotals                  ×1
    ///   GameBootstrapper_ZoneScoreTracker_FieldExists                     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlSaveDataTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static ZoneScoreTrackerSO CreateTrackerSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneScoreTrackerSO>();
            return so;
        }

        // ── SaveData tests ────────────────────────────────────────────────────

        [Test]
        public void SaveData_CareerPlayerZoneScore_DefaultZero()
        {
            var data = new SaveData();
            Assert.AreEqual(0f, data.careerPlayerZoneScore, 0.001f,
                "SaveData.careerPlayerZoneScore must default to 0.");
        }

        [Test]
        public void SaveData_CareerEnemyZoneScore_DefaultZero()
        {
            var data = new SaveData();
            Assert.AreEqual(0f, data.careerEnemyZoneScore, 0.001f,
                "SaveData.careerEnemyZoneScore must default to 0.");
        }

        // ── ZoneScoreTrackerSO career property tests ──────────────────────────

        [Test]
        public void ZoneScoreTracker_CareerPlayerScore_DefaultZero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0f, so.CareerPlayerScore, 0.001f,
                "CareerPlayerScore must be 0 on a fresh ZoneScoreTrackerSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_CareerEnemyScore_DefaultZero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0f, so.CareerEnemyScore, 0.001f,
                "CareerEnemyScore must be 0 on a fresh ZoneScoreTrackerSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_AccumulateToCareer_AddsPlayerScore()
        {
            var so = CreateTrackerSO();
            so.AddPlayerScore(30f);
            so.AccumulateToCareer();

            Assert.AreEqual(30f, so.CareerPlayerScore, 0.001f,
                "AccumulateToCareer must add PlayerScore to CareerPlayerScore.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_AccumulateToCareer_AddsEnemyScore()
        {
            var so = CreateTrackerSO();
            so.AddEnemyScore(20f);
            so.AccumulateToCareer();

            Assert.AreEqual(20f, so.CareerEnemyScore, 0.001f,
                "AccumulateToCareer must add EnemyScore to CareerEnemyScore.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_AccumulateToCareer_Additive_MultipleMatches()
        {
            var so = CreateTrackerSO();

            // Simulate match 1.
            so.AddPlayerScore(10f);
            so.AddEnemyScore(5f);
            so.AccumulateToCareer();
            so.Reset();

            // Simulate match 2.
            so.AddPlayerScore(15f);
            so.AddEnemyScore(8f);
            so.AccumulateToCareer();

            Assert.AreEqual(25f, so.CareerPlayerScore, 0.001f,
                "CareerPlayerScore must accumulate across multiple AccumulateToCareer calls.");
            Assert.AreEqual(13f, so.CareerEnemyScore, 0.001f,
                "CareerEnemyScore must accumulate across multiple AccumulateToCareer calls.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_LoadSnapshot_SetsCareerPlayerScore()
        {
            var so = CreateTrackerSO();
            so.LoadSnapshot(100f, 0f);

            Assert.AreEqual(100f, so.CareerPlayerScore, 0.001f,
                "LoadSnapshot must set CareerPlayerScore to the provided player value.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_LoadSnapshot_SetsCareerEnemyScore()
        {
            var so = CreateTrackerSO();
            so.LoadSnapshot(0f, 75f);

            Assert.AreEqual(75f, so.CareerEnemyScore, 0.001f,
                "LoadSnapshot must set CareerEnemyScore to the provided enemy value.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_LoadSnapshot_ClampsNegativeToZero()
        {
            var so = CreateTrackerSO();
            so.LoadSnapshot(-50f, -30f);

            Assert.AreEqual(0f, so.CareerPlayerScore, 0.001f,
                "LoadSnapshot must clamp negative playerScore to 0.");
            Assert.AreEqual(0f, so.CareerEnemyScore, 0.001f,
                "LoadSnapshot must clamp negative enemyScore to 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ZoneScoreTracker_Reset_DoesNotAffectCareerTotals()
        {
            var so = CreateTrackerSO();
            so.LoadSnapshot(200f, 150f);

            // Add per-match scores and then reset — career totals must survive.
            so.AddPlayerScore(50f);
            so.AddEnemyScore(25f);
            so.Reset(); // clears per-match scores only

            Assert.AreEqual(200f, so.CareerPlayerScore, 0.001f,
                "Reset must not modify CareerPlayerScore.");
            Assert.AreEqual(150f, so.CareerEnemyScore, 0.001f,
                "Reset must not modify CareerEnemyScore.");
            Object.DestroyImmediate(so);
        }

        // ── GameBootstrapper field existence test ─────────────────────────────

        [Test]
        public void GameBootstrapper_ZoneScoreTracker_FieldExists()
        {
            FieldInfo fi = typeof(GameBootstrapper)
                .GetField("_zoneScoreTracker",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            Assert.IsNotNull(fi,
                "GameBootstrapper must declare a '_zoneScoreTracker' serialised field (T277).");
            Assert.AreEqual(typeof(ZoneScoreTrackerSO), fi.FieldType,
                "_zoneScoreTracker field must be of type ZoneScoreTrackerSO.");
        }
    }
}

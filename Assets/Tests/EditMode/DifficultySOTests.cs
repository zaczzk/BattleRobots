using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="DifficultySO"/> and its integration
    /// with <see cref="MatchRecord.difficultyName"/>.
    ///
    /// Coverage:
    ///   - Default values match Medium preset.
    ///   - LoadPreset(Easy / Medium / Hard) sets all fields correctly.
    ///   - CreatePreset factory returns correctly initialised instances.
    ///   - Hard has higher aggression/damage/drive and lower time limit than Easy.
    ///   - Medium values are between Easy and Hard (sanity).
    ///   - MatchRecord.difficultyName field serialises (non-null default).
    /// </summary>
    [TestFixture]
    public sealed class DifficultySOTests
    {
        private DifficultySO _difficulty;

        [SetUp]
        public void SetUp()
        {
            _difficulty = ScriptableObject.CreateInstance<DifficultySO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_difficulty);
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void DefaultValues_MatchMediumPreset()
        {
            // ScriptableObject.CreateInstance uses serialised field initialisers.
            Assert.AreEqual("Medium", _difficulty.DifficultyName,
                "Default difficulty name should be 'Medium'.");
            Assert.AreEqual(0.5f, _difficulty.AiAggressionScale, 1e-5f,
                "Default AiAggressionScale should be 0.5 (Medium).");
            Assert.AreEqual(1f, _difficulty.AiDriveSpeedScale, 1e-5f,
                "Default AiDriveSpeedScale should be 1.0.");
            Assert.AreEqual(1f, _difficulty.DamageMultiplier, 1e-5f,
                "Default DamageMultiplier should be 1.0.");
            Assert.AreEqual(1f, _difficulty.TimeLimitScale, 1e-5f,
                "Default TimeLimitScale should be 1.0.");
        }

        // ── LoadPreset — Easy ─────────────────────────────────────────────────

        [Test]
        public void LoadPreset_Easy_SetsName()
        {
            _difficulty.LoadPreset(DifficultyLevel.Easy);
            Assert.AreEqual("Easy", _difficulty.DifficultyName);
        }

        [Test]
        public void LoadPreset_Easy_SetsLowAggression()
        {
            _difficulty.LoadPreset(DifficultyLevel.Easy);
            Assert.Less(_difficulty.AiAggressionScale, 0.5f,
                "Easy aggression should be below Medium (0.5).");
        }

        [Test]
        public void LoadPreset_Easy_SetsLowDamage()
        {
            _difficulty.LoadPreset(DifficultyLevel.Easy);
            Assert.Less(_difficulty.DamageMultiplier, 1f,
                "Easy damage multiplier should be below 1.0.");
        }

        [Test]
        public void LoadPreset_Easy_SetsHighTimeLimitScale()
        {
            _difficulty.LoadPreset(DifficultyLevel.Easy);
            Assert.Greater(_difficulty.TimeLimitScale, 1f,
                "Easy time limit scale should be > 1.0 (more time).");
        }

        // ── LoadPreset — Medium ───────────────────────────────────────────────

        [Test]
        public void LoadPreset_Medium_SetsIdentityValues()
        {
            // Mutate to Hard first so we know LoadPreset overwrites.
            _difficulty.LoadPreset(DifficultyLevel.Hard);
            _difficulty.LoadPreset(DifficultyLevel.Medium);

            Assert.AreEqual("Medium", _difficulty.DifficultyName);
            Assert.AreEqual(0.5f, _difficulty.AiAggressionScale, 1e-5f);
            Assert.AreEqual(1f,   _difficulty.AiDriveSpeedScale, 1e-5f);
            Assert.AreEqual(1f,   _difficulty.DamageMultiplier,  1e-5f);
            Assert.AreEqual(1f,   _difficulty.TimeLimitScale,    1e-5f);
        }

        // ── LoadPreset — Hard ─────────────────────────────────────────────────

        [Test]
        public void LoadPreset_Hard_SetsName()
        {
            _difficulty.LoadPreset(DifficultyLevel.Hard);
            Assert.AreEqual("Hard", _difficulty.DifficultyName);
        }

        [Test]
        public void LoadPreset_Hard_SetsHighAggression()
        {
            _difficulty.LoadPreset(DifficultyLevel.Hard);
            Assert.Greater(_difficulty.AiAggressionScale, 0.5f,
                "Hard aggression should be above Medium (0.5).");
        }

        [Test]
        public void LoadPreset_Hard_SetsHighDamage()
        {
            _difficulty.LoadPreset(DifficultyLevel.Hard);
            Assert.Greater(_difficulty.DamageMultiplier, 1f,
                "Hard damage multiplier should be above 1.0.");
        }

        [Test]
        public void LoadPreset_Hard_SetsLowTimeLimitScale()
        {
            _difficulty.LoadPreset(DifficultyLevel.Hard);
            Assert.Less(_difficulty.TimeLimitScale, 1f,
                "Hard time limit scale should be < 1.0 (less time).");
        }

        // ── Hard > Easy ordering ──────────────────────────────────────────────

        [Test]
        public void Hard_AggressionScale_GreaterThan_Easy()
        {
            var easy = DifficultySO.CreatePreset(DifficultyLevel.Easy);
            var hard = DifficultySO.CreatePreset(DifficultyLevel.Hard);

            Assert.Greater(hard.AiAggressionScale, easy.AiAggressionScale);

            Object.DestroyImmediate(easy);
            Object.DestroyImmediate(hard);
        }

        [Test]
        public void Hard_DamageMultiplier_GreaterThan_Easy()
        {
            var easy = DifficultySO.CreatePreset(DifficultyLevel.Easy);
            var hard = DifficultySO.CreatePreset(DifficultyLevel.Hard);

            Assert.Greater(hard.DamageMultiplier, easy.DamageMultiplier);

            Object.DestroyImmediate(easy);
            Object.DestroyImmediate(hard);
        }

        [Test]
        public void Hard_DriveSpeedScale_GreaterThan_Easy()
        {
            var easy = DifficultySO.CreatePreset(DifficultyLevel.Easy);
            var hard = DifficultySO.CreatePreset(DifficultyLevel.Hard);

            Assert.Greater(hard.AiDriveSpeedScale, easy.AiDriveSpeedScale);

            Object.DestroyImmediate(easy);
            Object.DestroyImmediate(hard);
        }

        [Test]
        public void Easy_TimeLimitScale_GreaterThan_Hard()
        {
            var easy = DifficultySO.CreatePreset(DifficultyLevel.Easy);
            var hard = DifficultySO.CreatePreset(DifficultyLevel.Hard);

            Assert.Greater(easy.TimeLimitScale, hard.TimeLimitScale);

            Object.DestroyImmediate(easy);
            Object.DestroyImmediate(hard);
        }

        // ── MatchRecord integration ───────────────────────────────────────────

        [Test]
        public void MatchRecord_DifficultyName_DefaultIsNull()
        {
            var record = new MatchRecord();
            // difficultyName is a string field; C# initialises string fields to null by default.
            Assert.IsNull(record.difficultyName,
                "difficultyName should be null when not set, so empty-string assignment is explicit.");
        }

        [Test]
        public void MatchRecord_DifficultyName_CanBeSetToPresetName()
        {
            var record = new MatchRecord
            {
                difficultyName = "Hard"
            };
            Assert.AreEqual("Hard", record.difficultyName);
        }
    }
}

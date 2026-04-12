using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AdaptiveDifficultyAdvisorSO"/>.
    ///
    /// Covers GetSuggestion:
    ///   • Null config → null.
    ///   • Empty presets list → null.
    ///   • Null _scoreHistory and _winStreak (no data) → null.
    ///   • Positive trend + streak ≥ 3 → hardest preset (last index).
    ///   • Positive trend + streak == 2 → next-up preset (second-to-last).
    ///   • Positive trend + streak == 1 → next-up preset.
    ///   • Positive trend + streak == 0 → null (streak required for positive path).
    ///   • Negative trend → easiest preset (index 0).
    ///   • Zero trend + zero streak → null (no data).
    ///   • Single-preset list, negative trend → the only preset.
    ///   • Two-preset list, positive trend + streak ≥ 1 → index 0 (next-up clamps to 0).
    ///
    /// Covers GetSuggestionReason:
    ///   • Null config → "No data".
    ///   • No data (zero trend, zero streak) → "No data".
    ///   • Positive trend + streak ≥ 3 → reason contains streak count.
    ///   • Positive trend + streak == 1 → reason contains streak count.
    ///   • Negative trend → reason mentions "easier".
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class AdaptiveDifficultyAdvisorSOTests
    {
        // ── Instances under test ──────────────────────────────────────────────

        private AdaptiveDifficultyAdvisorSO _advisor;
        private ScoreHistorySO              _history;
        private WinStreakSO                 _streak;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a DifficultyPresetsConfig with <paramref name="count"/> presets,
        /// each backed by a fresh BotDifficultyConfig SO.
        /// Returns the created config and the BotDifficultyConfig array (index 0 = easiest).
        /// </summary>
        private static (DifficultyPresetsConfig config, BotDifficultyConfig[] configs)
            MakePresetsConfig(int count)
        {
            var presetsCfg = ScriptableObject.CreateInstance<DifficultyPresetsConfig>();
            var botConfigs = new BotDifficultyConfig[count];

            var presetsList = new List<DifficultyPresetsConfig.DifficultyPreset>();
            for (int i = 0; i < count; i++)
            {
                botConfigs[i] = ScriptableObject.CreateInstance<BotDifficultyConfig>();
                botConfigs[i].name = $"Difficulty_{i}";
                presetsList.Add(new DifficultyPresetsConfig.DifficultyPreset
                {
                    displayName = $"Preset{i}",
                    config      = botConfigs[i],
                });
            }

            SetField(presetsCfg, "_presets", presetsList);
            return (presetsCfg, botConfigs);
        }

        /// <summary>Destroys every SO in <paramref name="configs"/>.</summary>
        private static void DestroyConfigs(BotDifficultyConfig[] configs)
        {
            foreach (var c in configs)
                if (c != null) Object.DestroyImmediate(c);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _advisor = ScriptableObject.CreateInstance<AdaptiveDifficultyAdvisorSO>();
            _history = ScriptableObject.CreateInstance<ScoreHistorySO>();
            _streak  = ScriptableObject.CreateInstance<WinStreakSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_advisor);
            Object.DestroyImmediate(_history);
            Object.DestroyImmediate(_streak);
        }

        // ── GetSuggestion — null / empty config ───────────────────────────────

        [Test]
        public void GetSuggestion_NullConfig_ReturnsNull()
        {
            Assert.IsNull(_advisor.GetSuggestion(null),
                "Null config must produce a null suggestion.");
        }

        [Test]
        public void GetSuggestion_EmptyPresets_ReturnsNull()
        {
            var (config, configs) = MakePresetsConfig(0);

            Assert.IsNull(_advisor.GetSuggestion(config),
                "Empty presets list must produce a null suggestion.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        // ── GetSuggestion — no data ───────────────────────────────────────────

        [Test]
        public void GetSuggestion_NullHistoryAndStreak_ReturnsNull()
        {
            // Both data sources absent — advisor fields left null (default from SetUp).
            var (config, configs) = MakePresetsConfig(3);

            Assert.IsNull(_advisor.GetSuggestion(config),
                "No data sources wired must produce a null suggestion.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestion_ZeroTrendZeroStreak_ReturnsNull()
        {
            SetField(_advisor, "_scoreHistory", _history);  // empty history → TrendDelta = 0
            SetField(_advisor, "_winStreak",    _streak);   // reset streak  → CurrentStreak = 0

            var (config, configs) = MakePresetsConfig(3);

            Assert.IsNull(_advisor.GetSuggestion(config),
                "Zero trend and zero streak must produce a null suggestion.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        // ── GetSuggestion — positive trend + streak ───────────────────────────

        [Test]
        public void GetSuggestion_PositiveTrendAndStreakThree_ReturnsHardestPreset()
        {
            // Build a positive TrendDelta: record 100 then 200 → last-first = +100.
            _history.Record(100);
            _history.Record(200);
            _streak.LoadSnapshot(3, 3);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            BotDifficultyConfig hardest = configs[2]; // last index

            BotDifficultyConfig result = _advisor.GetSuggestion(config);

            Assert.AreSame(hardest, result,
                "Positive trend + streak ≥ 3 must suggest the hardest (last) preset.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestion_PositiveTrendAndStreakFive_ReturnsHardestPreset()
        {
            _history.Record(100);
            _history.Record(500);
            _streak.LoadSnapshot(5, 5);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            BotDifficultyConfig hardest = configs[2];

            Assert.AreSame(hardest, _advisor.GetSuggestion(config),
                "Positive trend + streak == 5 (≥ 3) must suggest hardest preset.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestion_PositiveTrendAndStreakOne_ReturnsNextUpPreset()
        {
            _history.Record(100);
            _history.Record(300);
            _streak.LoadSnapshot(1, 1);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            BotDifficultyConfig nextUp = configs[1]; // second-to-last in a 3-preset list

            Assert.AreSame(nextUp, _advisor.GetSuggestion(config),
                "Positive trend + streak == 1 must suggest the next-up (second-to-last) preset.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestion_PositiveTrendAndStreakTwo_ReturnsNextUpPreset()
        {
            _history.Record(200);
            _history.Record(400);
            _streak.LoadSnapshot(2, 2);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            BotDifficultyConfig nextUp = configs[1];

            Assert.AreSame(nextUp, _advisor.GetSuggestion(config),
                "Positive trend + streak == 2 (< 3) must suggest next-up, not hardest.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestion_PositiveTrendStreakZero_ReturnsNull()
        {
            _history.Record(100);
            _history.Record(400);
            // streak remains 0 (fresh WinStreakSO)

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);

            Assert.IsNull(_advisor.GetSuggestion(config),
                "Positive trend with streak == 0 must return null (streak required for positive suggestion).");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        // ── GetSuggestion — two-preset list (next-up clamps to index 0) ───────

        [Test]
        public void GetSuggestion_TwoPresets_PositiveTrendStreakOne_ReturnsIndexZero()
        {
            _history.Record(100);
            _history.Record(300);
            _streak.LoadSnapshot(1, 1);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            // With 2 presets: Count - 2 = 0, so next-up clamps to index 0.
            var (config, configs) = MakePresetsConfig(2);
            BotDifficultyConfig expected = configs[0];

            Assert.AreSame(expected, _advisor.GetSuggestion(config),
                "Two-preset list: next-up index (Count-2=0) must clamp to index 0.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        // ── GetSuggestion — negative trend ────────────────────────────────────

        [Test]
        public void GetSuggestion_NegativeTrend_ReturnsEasiestPreset()
        {
            _history.Record(500);
            _history.Record(200);
            // streak irrelevant for negative-trend path

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            BotDifficultyConfig easiest = configs[0];

            Assert.AreSame(easiest, _advisor.GetSuggestion(config),
                "Negative trend must suggest the easiest (index 0) preset.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestion_SinglePreset_NegativeTrend_ReturnsOnlyPreset()
        {
            _history.Record(300);
            _history.Record(100);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(1);
            BotDifficultyConfig only = configs[0];

            Assert.AreSame(only, _advisor.GetSuggestion(config),
                "Single-preset list with negative trend must return the only preset.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        // ── GetSuggestionReason ───────────────────────────────────────────────

        [Test]
        public void GetSuggestionReason_NullConfig_ReturnsNoData()
        {
            Assert.AreEqual("No data", _advisor.GetSuggestionReason(null));
        }

        [Test]
        public void GetSuggestionReason_NoData_ReturnsNoData()
        {
            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);

            Assert.AreEqual("No data", _advisor.GetSuggestionReason(config),
                "Zero trend + zero streak must return 'No data' reason.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestionReason_PositiveTrendStreakThreePlus_ContainsStreakCount()
        {
            _history.Record(100);
            _history.Record(400);
            _streak.LoadSnapshot(4, 4);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            string reason = _advisor.GetSuggestionReason(config);

            StringAssert.Contains("4", reason,
                "Reason for hardest suggestion must include the streak count.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestionReason_PositiveTrendStreakOne_ContainsStreakCount()
        {
            _history.Record(100);
            _history.Record(300);
            _streak.LoadSnapshot(1, 1);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            string reason = _advisor.GetSuggestionReason(config);

            StringAssert.Contains("1", reason,
                "Reason for next-up suggestion must include the streak count.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }

        [Test]
        public void GetSuggestionReason_NegativeTrend_ContainsEasierKeyword()
        {
            _history.Record(500);
            _history.Record(100);

            SetField(_advisor, "_scoreHistory", _history);
            SetField(_advisor, "_winStreak",    _streak);

            var (config, configs) = MakePresetsConfig(3);
            string reason = _advisor.GetSuggestionReason(config);

            StringAssert.Contains("easier", reason,
                "Reason for easiest suggestion must mention 'easier'.");

            Object.DestroyImmediate(config);
            DestroyConfigs(configs);
        }
    }
}

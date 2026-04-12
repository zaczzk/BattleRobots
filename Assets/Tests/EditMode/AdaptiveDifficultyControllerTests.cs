using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AdaptiveDifficultyController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all fields null — no throw.
    ///   • OnEnable / OnDisable with null event channels — no throw.
    ///   • OnDisable unregisters from both event channels (external-counter pattern).
    ///   • Refresh() with null advisor — suggestion text = "No suggestion", reason = "No data",
    ///     button not interactable.
    ///   • Refresh() with advisor that returns a valid suggestion — button interactable,
    ///     suggestion text contains "Try:".
    ///   • Refresh() with advisor that returns null suggestion — button not interactable.
    ///   • Refresh() with null UI refs — no throw.
    ///   • UseSuggestion() applies suggestion to SelectedDifficultySO.
    ///   • UseSuggestion() with null advisor — silent no-op.
    ///   • UseSuggestion() with null _selectedDifficulty — silent no-op.
    ///
    /// All tests run headless (no full scene required).
    /// </summary>
    public class AdaptiveDifficultyControllerTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static (GameObject go, AdaptiveDifficultyController ctrl) MakeCtrl()
        {
            var go   = new GameObject("AdaptiveDifficultyController");
            go.SetActive(false);
            var ctrl = go.AddComponent<AdaptiveDifficultyController>();
            return (go, ctrl);
        }

        /// <summary>
        /// Creates a DifficultyPresetsConfig with <paramref name="count"/> presets.
        /// Returns (config, botConfigs[]).  Index 0 = easiest, last = hardest.
        /// </summary>
        private static (DifficultyPresetsConfig config, BotDifficultyConfig[] botConfigs)
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

        private static void DestroyBotConfigs(BotDifficultyConfig[] configs)
        {
            foreach (var c in configs)
                if (c != null) Object.DestroyImmediate(c);
        }

        /// <summary>
        /// Builds an AdaptiveDifficultyAdvisorSO wired with a ScoreHistorySO that has a
        /// positive TrendDelta and a WinStreakSO with the given streak, so that
        /// GetSuggestion() returns a non-null result.
        /// </summary>
        private static AdaptiveDifficultyAdvisorSO MakeAdvisorWithPositiveSignal(int streak = 3)
        {
            var advisor = ScriptableObject.CreateInstance<AdaptiveDifficultyAdvisorSO>();
            var history = ScriptableObject.CreateInstance<ScoreHistorySO>();
            history.Record(100);
            history.Record(500); // TrendDelta = +400

            var winStreak = ScriptableObject.CreateInstance<WinStreakSO>();
            winStreak.LoadSnapshot(streak, streak);

            SetField(advisor, "_scoreHistory", history);
            SetField(advisor, "_winStreak",    winStreak);

            // Track sub-assets so callers can destroy them alongside the advisor.
            // (History and WinStreak SOs will be GC'd by Unity after DestroyImmediate(advisor).)
            return advisor;
        }

        // ── OnEnable / OnDisable — all null refs ──────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable / OnDisable — null channels ──────────────────────────────

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            var advisor = ScriptableObject.CreateInstance<AdaptiveDifficultyAdvisorSO>();
            SetField(ctrl, "_advisor", advisor);
            // _onMatchEnded and _onHistoryUpdated remain null.
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null event channels must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(advisor);
        }

        [Test]
        public void OnDisable_NullChannels_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            var advisor = ScriptableObject.CreateInstance<AdaptiveDifficultyAdvisorSO>();
            SetField(ctrl, "_advisor", advisor);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null event channels must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(advisor);
        }

        // ── OnDisable — unregisters from both channels ────────────────────────

        [Test]
        public void OnDisable_UnregistersFromBothChannels()
        {
            var matchEndedChannel   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var historyChannel      = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalMatchCount  = 0;
            int externalHistoryCnt  = 0;
            matchEndedChannel.RegisterCallback(() => externalMatchCount++);
            historyChannel.RegisterCallback(() => externalHistoryCnt++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onMatchEnded",     matchEndedChannel);
            SetField(ctrl, "_onHistoryUpdated", historyChannel);

            go.SetActive(true);   // OnEnable subscribes
            go.SetActive(false);  // OnDisable unsubscribes

            matchEndedChannel.Raise();  // only external counter fires
            historyChannel.Raise();

            Assert.AreEqual(1, externalMatchCount,
                "After OnDisable, only the external counter fires on _onMatchEnded.");
            Assert.AreEqual(1, externalHistoryCnt,
                "After OnDisable, only the external counter fires on _onHistoryUpdated.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEndedChannel);
            Object.DestroyImmediate(historyChannel);
        }

        // ── Refresh — null advisor → fallback text, button non-interactable ───

        [Test]
        public void Refresh_NullAdvisor_SuggestionTextShowsNoSuggestion()
        {
            var (go, ctrl) = MakeCtrl();
            var textGO = new GameObject("SuggestionText");
            var text   = textGO.AddComponent<Text>();
            SetField(ctrl, "_suggestionText", text);
            // _advisor left null

            go.SetActive(true);

            Assert.AreEqual("No suggestion", text.text,
                "Null advisor must show 'No suggestion' in _suggestionText.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGO);
        }

        [Test]
        public void Refresh_NullAdvisor_ReasonTextShowsNoData()
        {
            var (go, ctrl) = MakeCtrl();
            var textGO = new GameObject("ReasonText");
            var text   = textGO.AddComponent<Text>();
            SetField(ctrl, "_suggestionReasonText", text);

            go.SetActive(true);

            Assert.AreEqual("No data", text.text,
                "Null advisor must show 'No data' in _suggestionReasonText.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGO);
        }

        [Test]
        public void Refresh_NullAdvisor_ButtonNotInteractable()
        {
            var (go, ctrl) = MakeCtrl();
            var btnGO  = new GameObject("Button");
            var btn    = btnGO.AddComponent<Button>();
            SetField(ctrl, "_useSuggestionButton", btn);

            go.SetActive(true);

            Assert.IsFalse(btn.interactable,
                "Null advisor must make the suggestion button non-interactable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGO);
        }

        // ── Refresh — valid suggestion → button interactable ──────────────────

        [Test]
        public void Refresh_WithSuggestion_ButtonIsInteractable()
        {
            var (config, botConfigs) = MakePresetsConfig(3);
            var advisor = MakeAdvisorWithPositiveSignal(streak: 3);

            var (go, ctrl) = MakeCtrl();
            var btnGO = new GameObject("Button");
            var btn   = btnGO.AddComponent<Button>();

            SetField(ctrl, "_advisor",          advisor);
            SetField(ctrl, "_difficultyPresets", config);
            SetField(ctrl, "_useSuggestionButton", btn);

            go.SetActive(true);

            Assert.IsTrue(btn.interactable,
                "Valid suggestion must make the button interactable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGO);
            Object.DestroyImmediate(advisor);
            Object.DestroyImmediate(config);
            DestroyBotConfigs(botConfigs);
        }

        [Test]
        public void Refresh_WithSuggestion_SuggestionTextContainsTryPrefix()
        {
            var (config, botConfigs) = MakePresetsConfig(3);
            var advisor = MakeAdvisorWithPositiveSignal(streak: 3);

            var (go, ctrl) = MakeCtrl();
            var textGO = new GameObject("SuggestionText");
            var text   = textGO.AddComponent<Text>();

            SetField(ctrl, "_advisor",          advisor);
            SetField(ctrl, "_difficultyPresets", config);
            SetField(ctrl, "_suggestionText",   text);

            go.SetActive(true);

            StringAssert.StartsWith("Try:", text.text,
                "Suggestion text must start with 'Try:' when a suggestion is available.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGO);
            Object.DestroyImmediate(advisor);
            Object.DestroyImmediate(config);
            DestroyBotConfigs(botConfigs);
        }

        // ── Refresh — no suggestion → button non-interactable ─────────────────

        [Test]
        public void Refresh_NoSuggestion_ButtonNotInteractable()
        {
            // Advisor wired but no data signal → GetSuggestion returns null.
            var (config, botConfigs) = MakePresetsConfig(3);
            var advisor = ScriptableObject.CreateInstance<AdaptiveDifficultyAdvisorSO>();
            // No _scoreHistory/_winStreak wired → zero trend + zero streak → no suggestion.

            var (go, ctrl) = MakeCtrl();
            var btnGO = new GameObject("Button");
            var btn   = btnGO.AddComponent<Button>();

            SetField(ctrl, "_advisor",           advisor);
            SetField(ctrl, "_difficultyPresets",  config);
            SetField(ctrl, "_useSuggestionButton", btn);

            go.SetActive(true);

            Assert.IsFalse(btn.interactable,
                "No suggestion (null return from advisor) must make button non-interactable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(btnGO);
            Object.DestroyImmediate(advisor);
            Object.DestroyImmediate(config);
            DestroyBotConfigs(botConfigs);
        }

        // ── Refresh — null UI refs → no throw ─────────────────────────────────

        [Test]
        public void Refresh_NullUIRefs_DoesNotThrow()
        {
            var (config, botConfigs) = MakePresetsConfig(3);
            var advisor = MakeAdvisorWithPositiveSignal(streak: 3);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_advisor",          advisor);
            SetField(ctrl, "_difficultyPresets", config);
            // All Text/Button refs left null.

            Assert.DoesNotThrow(() => go.SetActive(true),
                "Refresh() with null UI refs must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(advisor);
            Object.DestroyImmediate(config);
            DestroyBotConfigs(botConfigs);
        }

        // ── UseSuggestion — applies to SelectedDifficultySO ───────────────────

        [Test]
        public void UseSuggestion_AppliesSuggestionToSelectedDifficulty()
        {
            var (config, botConfigs) = MakePresetsConfig(3);
            var advisor  = MakeAdvisorWithPositiveSignal(streak: 3);
            var selected = ScriptableObject.CreateInstance<SelectedDifficultySO>();

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_advisor",           advisor);
            SetField(ctrl, "_difficultyPresets",  config);
            SetField(ctrl, "_selectedDifficulty", selected);

            go.SetActive(true);

            ctrl.UseSuggestion();

            // Positive trend + streak 3 → hardest preset = botConfigs[2].
            Assert.AreSame(botConfigs[2], selected.Current,
                "UseSuggestion() must apply the hardest preset when streak ≥ 3 and trend is positive.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(advisor);
            Object.DestroyImmediate(selected);
            Object.DestroyImmediate(config);
            DestroyBotConfigs(botConfigs);
        }

        // ── UseSuggestion — null guard no-ops ─────────────────────────────────

        [Test]
        public void UseSuggestion_NullAdvisor_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.UseSuggestion(),
                "UseSuggestion() with null advisor must be a silent no-op.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void UseSuggestion_NullSelectedDifficulty_DoesNotThrow()
        {
            var (config, botConfigs) = MakePresetsConfig(3);
            var advisor = MakeAdvisorWithPositiveSignal(streak: 3);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_advisor",          advisor);
            SetField(ctrl, "_difficultyPresets", config);
            // _selectedDifficulty left null.

            go.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.UseSuggestion(),
                "UseSuggestion() with null _selectedDifficulty must be a silent no-op.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(advisor);
            Object.DestroyImmediate(config);
            DestroyBotConfigs(botConfigs);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the T114 M7 progression display feature.
    ///
    /// Three test areas, all in one file because they share the same feature boundary:
    ///
    /// ── PART 1: MatchResultSO M7 fields ────────────────────────────────────────
    ///   New properties: XPEarned, LeveledUp, NewLevel.
    ///   • Fresh-instance defaults: XPEarned=0, LeveledUp=false, NewLevel=1.
    ///   • Write() with M7 params stores correct values.
    ///   • Write() omitting M7 params is backwards-compatible (0/false/1).
    ///   • Write() called twice overwrites M7 fields.
    ///
    /// ── PART 2: MatchManager M7 integration ────────────────────────────────────
    ///   EndMatch() (private, invoked via reflection after priming _matchRunning).
    ///   • Win: XPEarned > 0 is written to MatchResultSO.
    ///   • Loss: XPEarned > 0 (consolation) is written to MatchResultSO.
    ///   • Level-up: LeveledUp=true written when threshold crossed (Level 1 → 2).
    ///   • No level-up: LeveledUp=false when XP is insufficient.
    ///   • Null _playerProgression: XPEarned=0, LeveledUp=false, NewLevel=1.
    ///
    /// ── PART 3: PostMatchController M7 display ─────────────────────────────────
    ///   ShowResults() triggered via VoidGameEvent.Raise() after wiring.
    ///   • Null-safety: all M7 fields null → DoesNotThrow.
    ///   • Null _matchResult early-return: M7 code not reached → DoesNotThrow.
    ///   • XP text populated: "XP: +N" format.
    ///   • Level text populated: "Level N" format.
    ///   • Level-up banner: SetActive(true) when LeveledUp=true.
    ///   • Level-up banner: SetActive(false) when LeveledUp=false.
    ///   • Tier text populated from BuildRatingSO + RobotTierConfig.
    ///   • Null BuildRatingSO → tier text block skipped → DoesNotThrow.
    ///   • Achievement banner shown/hidden based on LastUnlockedId.
    ///   • Achievement text uses catalog display name when catalog assigned.
    ///   • Achievement text falls back to raw ID when catalog null.
    ///   • Null PlayerAchievementsSO → DoesNotThrow.
    /// </summary>
    public class PostMatchM7Tests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ═════════════════════════════════════════════════════════════════════
        // PART 1 — MatchResultSO M7 fields
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void FreshInstance_XPEarned_IsZero()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            Assert.AreEqual(0, result.XPEarned,
                "XPEarned must default to 0 on a fresh MatchResultSO.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void FreshInstance_LeveledUp_IsFalse()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            Assert.IsFalse(result.LeveledUp,
                "LeveledUp must default to false on a fresh MatchResultSO.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void FreshInstance_NewLevel_IsOne()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            Assert.AreEqual(1, result.NewLevel,
                "NewLevel must default to 1 on a fresh MatchResultSO.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_WithM7Params_SetsXPEarned()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(true, 60f, 200, 700,
                         xpEarned: 142, leveledUp: true, newLevel: 2);
            Assert.AreEqual(142, result.XPEarned,
                "Write() must store the supplied xpEarned value.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_WithM7Params_SetsLeveledUp()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(true, 60f, 200, 700,
                         xpEarned: 142, leveledUp: true, newLevel: 2);
            Assert.IsTrue(result.LeveledUp,
                "Write() must store leveledUp = true.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_WithM7Params_SetsNewLevel()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(true, 60f, 200, 700,
                         xpEarned: 142, leveledUp: true, newLevel: 3);
            Assert.AreEqual(3, result.NewLevel,
                "Write() must store the supplied newLevel value.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_OmittingM7Params_XPEarned_DefaultsToZero()
        {
            // Backwards-compat: existing callers that don't pass M7 params get 0/false/1.
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon: true, durationSeconds: 60f,
                         currencyEarned: 200, newWalletBalance: 700);
            Assert.AreEqual(0, result.XPEarned,
                "Omitting xpEarned must default to 0 (backwards-compatible).");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_OmittingM7Params_LeveledUp_DefaultsFalse()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon: false, durationSeconds: 90f,
                         currencyEarned: 50, newWalletBalance: 350);
            Assert.IsFalse(result.LeveledUp,
                "Omitting leveledUp must default to false (backwards-compatible).");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_OmittingM7Params_NewLevel_DefaultsOne()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon: false, durationSeconds: 90f,
                         currencyEarned: 50, newWalletBalance: 350);
            Assert.AreEqual(1, result.NewLevel,
                "Omitting newLevel must default to 1 (backwards-compatible).");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_CalledTwice_OverwritesM7Fields()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(true,  60f, 200, 700, xpEarned: 142, leveledUp: true,  newLevel: 2);
            result.Write(false, 90f,  50, 350, xpEarned:  37, leveledUp: false, newLevel: 2);
            Assert.AreEqual(37,    result.XPEarned,  "Second Write must overwrite XPEarned.");
            Assert.IsFalse(        result.LeveledUp, "Second Write must overwrite LeveledUp.");
            Object.DestroyImmediate(result);
        }

        // ═════════════════════════════════════════════════════════════════════
        // PART 2 — MatchManager M7 integration
        // ═════════════════════════════════════════════════════════════════════

        // ── Shared fixture fields (used by Part 2 MatchManager tests) ────────

        private GameObject     _mmGO;
        private MatchManager   _mm;
        private HealthSO       _ph;
        private HealthSO       _eh;

        [SetUp]
        public void SetUpMatchManager()
        {
            SaveSystem.Delete();

            _mmGO = new GameObject("TestMatchManager");
            _mmGO.SetActive(false);
            _mm   = _mmGO.AddComponent<MatchManager>();

            _ph = ScriptableObject.CreateInstance<HealthSO>();
            _eh = ScriptableObject.CreateInstance<HealthSO>();
            _ph.Reset();
            _eh.Reset();

            SetField(_mm, "_playerHealth", _ph);
            SetField(_mm, "_enemyHealth",  _eh);

            _mmGO.SetActive(true);
        }

        [TearDown]
        public void TearDownMatchManager()
        {
            SaveSystem.Delete();
            if (_mmGO) Object.DestroyImmediate(_mmGO);
            if (_ph)   Object.DestroyImmediate(_ph);
            if (_eh)   Object.DestroyImmediate(_eh);
        }

        /// <summary>
        /// Primes _matchRunning and _activeRoundDuration, then calls EndMatch(playerWon)
        /// via reflection so the private method runs without a physics tick.
        /// </summary>
        private void InvokeEndMatch(bool playerWon, float roundDuration = 60f)
        {
            SetField(_mm, "_matchRunning",       true);
            SetField(_mm, "_activeRoundDuration", roundDuration);
            SetField(_mm, "_timeRemaining",       0f);

            var method = typeof(MatchManager)
                .GetMethod("EndMatch", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "EndMatch method not found on MatchManager via reflection.");
            method.Invoke(_mm, new object[] { playerWon });
        }

        [Test]
        public void EndMatch_Win_WritesPositiveXPEarned_ToMatchResult()
        {
            var matchResult = ScriptableObject.CreateInstance<MatchResultSO>();
            var progression = ScriptableObject.CreateInstance<PlayerProgressionSO>();

            SetField(_mm, "_matchResult",      matchResult);
            SetField(_mm, "_playerProgression", progression);

            InvokeEndMatch(playerWon: true, roundDuration: 60f);

            // Win + 60s → XPRewardCalculator: 100 + 12 + 0 = 112 (no streak).
            Assert.Greater(matchResult.XPEarned, 0,
                "A winning match must write XPEarned > 0 to MatchResultSO.");

            Object.DestroyImmediate(matchResult);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void EndMatch_Loss_WritesPositiveXPEarned_ToMatchResult()
        {
            var matchResult = ScriptableObject.CreateInstance<MatchResultSO>();
            var progression = ScriptableObject.CreateInstance<PlayerProgressionSO>();

            SetField(_mm, "_matchResult",      matchResult);
            SetField(_mm, "_playerProgression", progression);

            InvokeEndMatch(playerWon: false, roundDuration: 60f);

            // Loss + 60s → XPRewardCalculator: 25 + 12 + 0 = 37 (no streak).
            Assert.Greater(matchResult.XPEarned, 0,
                "A losing match must still write consolation XPEarned > 0 to MatchResultSO.");

            Object.DestroyImmediate(matchResult);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void EndMatch_LevelUp_WritesLeveledUpTrue_ToMatchResult()
        {
            var matchResult = ScriptableObject.CreateInstance<MatchResultSO>();
            // PlayerProgressionSO at Level 1, 0 XP.
            // Win + 60s gives 112 XP. Level 2 requires 100 XP → level-up occurs.
            var progression = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            progression.LoadSnapshot(totalXP: 0, level: 1);

            SetField(_mm, "_matchResult",      matchResult);
            SetField(_mm, "_playerProgression", progression);

            InvokeEndMatch(playerWon: true, roundDuration: 60f);

            Assert.IsTrue(matchResult.LeveledUp,
                "LeveledUp must be true when a win causes a level threshold to be crossed.");

            Object.DestroyImmediate(matchResult);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void EndMatch_NoLevelUp_WritesLeveledUpFalse_ToMatchResult()
        {
            var matchResult = ScriptableObject.CreateInstance<MatchResultSO>();
            // PlayerProgressionSO at Level 9 (max 10). Level 10 needs 4500 XP.
            // A single loss match (25 XP consolation + duration bonus) is nowhere near.
            var progression = ScriptableObject.CreateInstance<PlayerProgressionSO>();
            progression.LoadSnapshot(totalXP: 3600, level: 9);

            SetField(_mm, "_matchResult",      matchResult);
            SetField(_mm, "_playerProgression", progression);

            InvokeEndMatch(playerWon: false, roundDuration: 10f);
            // Loss + 10s → 25 + 2 = 27 XP. Level 10 needs 4500 total; current=3600 → needs 900 more.

            Assert.IsFalse(matchResult.LeveledUp,
                "LeveledUp must be false when the XP earned is insufficient to reach the next level.");

            Object.DestroyImmediate(matchResult);
            Object.DestroyImmediate(progression);
        }

        [Test]
        public void EndMatch_NullProgression_WritesXPEarned_Zero_ToMatchResult()
        {
            var matchResult = ScriptableObject.CreateInstance<MatchResultSO>();
            SetField(_mm, "_matchResult", matchResult);
            // _playerProgression left null

            InvokeEndMatch(playerWon: true);

            Assert.AreEqual(0, matchResult.XPEarned,
                "When _playerProgression is null, XPEarned must be 0 in MatchResultSO.");

            Object.DestroyImmediate(matchResult);
        }

        [Test]
        public void EndMatch_NullProgression_WritesLeveledUp_False_ToMatchResult()
        {
            var matchResult = ScriptableObject.CreateInstance<MatchResultSO>();
            SetField(_mm, "_matchResult", matchResult);

            InvokeEndMatch(playerWon: true);

            Assert.IsFalse(matchResult.LeveledUp,
                "When _playerProgression is null, LeveledUp must be false in MatchResultSO.");

            Object.DestroyImmediate(matchResult);
        }

        [Test]
        public void EndMatch_NullProgression_WritesNewLevel_One_ToMatchResult()
        {
            var matchResult = ScriptableObject.CreateInstance<MatchResultSO>();
            SetField(_mm, "_matchResult", matchResult);

            InvokeEndMatch(playerWon: false);

            Assert.AreEqual(1, matchResult.NewLevel,
                "When _playerProgression is null, NewLevel must be 1 in MatchResultSO.");

            Object.DestroyImmediate(matchResult);
        }

        // ═════════════════════════════════════════════════════════════════════
        // PART 3 — PostMatchController M7 display
        // ═════════════════════════════════════════════════════════════════════

        // ── Factory helpers ───────────────────────────────────────────────────

        private static (GameObject go, PostMatchController ctrl) MakeCtrl()
        {
            var go   = new GameObject("PostMatchController");
            go.SetActive(false);
            var ctrl = go.AddComponent<PostMatchController>();
            return (go, ctrl);
        }

        /// <summary>Creates a MatchResultSO with M7 fields populated.</summary>
        private static MatchResultSO MakeM7Result(
            int  xpEarned  = 112,
            bool leveledUp = false,
            int  newLevel  = 1)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon: true, durationSeconds: 60f,
                    currencyEarned: 200, newWalletBalance: 700,
                    xpEarned: xpEarned, leveledUp: leveledUp, newLevel: newLevel);
            return r;
        }

        /// <summary>Creates a Text component on a child GO of parent.</summary>
        private static Text MakeText(GameObject parent)
        {
            var child = new GameObject("Text");
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void ShowResults_AllM7FieldsNull_DoesNotThrow()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeM7Result();
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            // All M7 optional fields left null

            go.SetActive(true);
            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with all M7 fields null must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ShowResults_NullMatchResult_M7FieldsAssigned_DoesNotThrow()
        {
            // M7 code only runs after the null-matchResult early-return guard.
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var achievements = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            var (go, ctrl)   = MakeCtrl();

            SetField(ctrl, "_onMatchEnded",       matchEnded);
            SetField(ctrl, "_playerAchievements", achievements);
            // _matchResult left null — early return fires before M7 block

            go.SetActive(true);
            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "Null _matchResult must trigger early-return before M7 display code.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(achievements);
        }

        [Test]
        public void ShowResults_NullBuildRating_TierText_DoesNotThrow()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeM7Result();
            var tierConfig = ScriptableObject.CreateInstance<RobotTierConfig>();
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_tierConfig",   tierConfig);
            // _buildRating left null — tier display block must be skipped

            go.SetActive(true);
            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "Null _buildRating with non-null _tierConfig must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(tierConfig);
        }

        [Test]
        public void ShowResults_NullPlayerAchievements_DoesNotThrow()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeM7Result();
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            // _playerAchievements left null

            go.SetActive(true);
            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "Null _playerAchievements must not throw in ShowResults.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        // ── XP text ───────────────────────────────────────────────────────────

        [Test]
        public void ShowResults_WithXPEarned_PopulatesXPText()
        {
            var matchEnded    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result        = MakeM7Result(xpEarned: 142);
            var (go, ctrl)    = MakeCtrl();
            var xpText        = MakeText(go);

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_xpEarnedText", xpText);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.AreEqual("XP: +142", xpText.text,
                "XP text must be formatted as 'XP: +N'.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        // ── Level text ────────────────────────────────────────────────────────

        [Test]
        public void ShowResults_WithNewLevel_PopulatesLevelText()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeM7Result(newLevel: 3);
            var (go, ctrl) = MakeCtrl();
            var levelText  = MakeText(go);

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_levelText",    levelText);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.AreEqual("Level 3", levelText.text,
                "Level text must be formatted as 'Level N'.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        // ── Level-up banner ───────────────────────────────────────────────────

        [Test]
        public void ShowResults_LeveledUp_True_ActivatesLevelUpBanner()
        {
            var matchEnded    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result        = MakeM7Result(leveledUp: true, newLevel: 2);
            var (go, ctrl)    = MakeCtrl();
            var bannerGO      = new GameObject("LevelUpBanner");
            bannerGO.transform.SetParent(go.transform);
            bannerGO.SetActive(false); // start hidden

            SetField(ctrl, "_onMatchEnded",  matchEnded);
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_levelUpBanner", bannerGO);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.IsTrue(bannerGO.activeSelf,
                "Level-up banner must be shown when LeveledUp=true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ShowResults_LeveledUp_False_HidesLevelUpBanner()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeM7Result(leveledUp: false);
            var (go, ctrl) = MakeCtrl();
            var bannerGO   = new GameObject("LevelUpBanner");
            bannerGO.transform.SetParent(go.transform);
            bannerGO.SetActive(true); // start visible — must be hidden by ShowResults

            SetField(ctrl, "_onMatchEnded",  matchEnded);
            SetField(ctrl, "_matchResult",   result);
            SetField(ctrl, "_levelUpBanner", bannerGO);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.IsFalse(bannerGO.activeSelf,
                "Level-up banner must be hidden when LeveledUp=false.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        // ── Tier text ─────────────────────────────────────────────────────────

        [Test]
        public void ShowResults_WithTierConfig_PopulatesTierText()
        {
            var matchEnded  = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result      = MakeM7Result();
            var buildRating = ScriptableObject.CreateInstance<BuildRatingSO>();
            var tierConfig  = ScriptableObject.CreateInstance<RobotTierConfig>();
            var (go, ctrl)  = MakeCtrl();
            var tierText    = MakeText(go);

            // Empty tier config → EvaluateTier returns Unranked;
            // GetDisplayName returns "Unranked" (or the fallback enum name).
            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_buildRating",  buildRating);
            SetField(ctrl, "_tierConfig",   tierConfig);
            SetField(ctrl, "_tierText",     tierText);

            go.SetActive(true);
            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with valid BuildRatingSO and RobotTierConfig must not throw.");
            // Text must be non-empty (at minimum the enum fallback "Unranked").
            Assert.IsFalse(string.IsNullOrEmpty(tierText.text),
                "Tier text must be populated with at least the tier name.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(buildRating);
            Object.DestroyImmediate(tierConfig);
        }

        // ── Achievement display ───────────────────────────────────────────────

        [Test]
        public void ShowResults_WithAchievement_ActivatesAchievementBanner()
        {
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result       = MakeM7Result();
            var achievements = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            achievements.Unlock("ach_001"); // sets LastUnlockedId
            var (go, ctrl)   = MakeCtrl();
            var bannerGO     = new GameObject("AchievementBanner");
            bannerGO.transform.SetParent(go.transform);
            bannerGO.SetActive(false);

            SetField(ctrl, "_onMatchEnded",       matchEnded);
            SetField(ctrl, "_matchResult",        result);
            SetField(ctrl, "_playerAchievements", achievements);
            SetField(ctrl, "_achievementBanner",  bannerGO);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.IsTrue(bannerGO.activeSelf,
                "Achievement banner must be shown when LastUnlockedId is non-empty.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(achievements);
        }

        [Test]
        public void ShowResults_NoAchievement_HidesAchievementBanner()
        {
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result       = MakeM7Result();
            var achievements = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            // No Unlock() call → LastUnlockedId is null
            var (go, ctrl)   = MakeCtrl();
            var bannerGO     = new GameObject("AchievementBanner");
            bannerGO.transform.SetParent(go.transform);
            bannerGO.SetActive(true); // start visible

            SetField(ctrl, "_onMatchEnded",       matchEnded);
            SetField(ctrl, "_matchResult",        result);
            SetField(ctrl, "_playerAchievements", achievements);
            SetField(ctrl, "_achievementBanner",  bannerGO);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.IsFalse(bannerGO.activeSelf,
                "Achievement banner must be hidden when no achievement was unlocked.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(achievements);
        }

        [Test]
        public void ShowResults_AchievementWithCatalog_UsesDisplayName()
        {
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result       = MakeM7Result();
            var achievements = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            achievements.Unlock("ach_first_win");

            // Build a catalog with a definition for "ach_first_win".
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            SetField(def, "_id",          "ach_first_win");
            SetField(def, "_displayName", "First Victory");

            var catalog = ScriptableObject.CreateInstance<AchievementCatalogSO>();
            SetField(catalog, "_achievements",
                     new System.Collections.Generic.List<AchievementDefinitionSO> { def });

            var (go, ctrl)     = MakeCtrl();
            var achievementText = MakeText(go);

            SetField(ctrl, "_onMatchEnded",        matchEnded);
            SetField(ctrl, "_matchResult",         result);
            SetField(ctrl, "_playerAchievements",  achievements);
            SetField(ctrl, "_achievementCatalog",  catalog);
            SetField(ctrl, "_achievementText",     achievementText);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.AreEqual("Achievement: First Victory", achievementText.text,
                "Achievement text must use the display name from the catalog.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(achievements);
            Object.DestroyImmediate(def);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ShowResults_AchievementWithoutCatalog_UsesRawId()
        {
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result       = MakeM7Result();
            var achievements = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            achievements.Unlock("raw_id_42");

            var (go, ctrl)      = MakeCtrl();
            var achievementText = MakeText(go);

            SetField(ctrl, "_onMatchEnded",       matchEnded);
            SetField(ctrl, "_matchResult",        result);
            SetField(ctrl, "_playerAchievements", achievements);
            // _achievementCatalog left null → must fall back to raw ID
            SetField(ctrl, "_achievementText",    achievementText);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.AreEqual("Achievement: raw_id_42", achievementText.text,
                "Without a catalog, achievement text must fall back to the raw ID.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(achievements);
        }
    }
}

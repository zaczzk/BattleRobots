using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T067 — Achievement system
    /// (AchievementDefinition, AchievementCatalogSO, AchievementProgressSO).
    ///
    /// Coverage (20 cases):
    ///
    /// AchievementProgressSO — default state
    ///   [01] DefaultState_UnlockedCount_IsZero
    ///   [02] DefaultState_IsUnlocked_ReturnsFalse
    ///
    /// AchievementProgressSO — LoadFromData
    ///   [03] LoadFromData_Null_ClearsAndLeavesEmpty
    ///   [04] LoadFromData_WithIds_RestoresUnlocked
    ///   [05] LoadFromData_DuplicateIds_Deduplicated
    ///   [06] LoadFromData_EmptyId_IsSkipped
    ///
    /// AchievementProgressSO — BuildData
    ///   [07] BuildData_EmptyWhenNothingUnlocked
    ///   [08] BuildData_ContainsAllUnlockedIds
    ///
    /// AchievementProgressSO — CheckAndUnlock
    ///   [09] CheckAndUnlock_NullCatalog_NoOp
    ///   [10] CheckAndUnlock_NullProfile_NoOp
    ///   [11] CheckAndUnlock_WinCount_MeetsThreshold_Unlocks
    ///   [12] CheckAndUnlock_WinCount_BelowThreshold_DoesNotUnlock
    ///   [13] CheckAndUnlock_AlreadyUnlocked_DoesNotFireEventAgain
    ///   [14] CheckAndUnlock_MatchCount_Unlocks
    ///   [15] CheckAndUnlock_CareerDamage_Unlocks
    ///   [16] CheckAndUnlock_CareerEarnings_Unlocks
    ///   [17] CheckAndUnlock_WinCurrentMatch_PlayerWon_Unlocks
    ///   [18] CheckAndUnlock_WinCurrentMatch_PlayerLost_DoesNotUnlock
    ///   [19] CheckAndUnlock_MultipleDefinitions_EachUnlockedSeparately
    ///
    /// AchievementDefinition — Evaluate edge cases
    ///   [20] Evaluate_NullRecord_WinCurrentMatch_ReturnsFalse
    /// </summary>
    [TestFixture]
    public sealed class AchievementTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private AchievementProgressSO _progress;
        private AchievementCatalogSO  _catalog;
        private PlayerProfileSO       _profile;

        // Track how many times _onAchievementUnlocked fired via a listener.
        private int _unlockedEventCount;

        [SetUp]
        public void SetUp()
        {
            _progress = ScriptableObject.CreateInstance<AchievementProgressSO>();
            _catalog  = ScriptableObject.CreateInstance<AchievementCatalogSO>();
            _profile  = ScriptableObject.CreateInstance<PlayerProfileSO>();
            _unlockedEventCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_progress);
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_profile);
        }

        // ── [01] Default state — UnlockedCount ───────────────────────────────

        [Test]
        public void DefaultState_UnlockedCount_IsZero()
        {
            Assert.AreEqual(0, _progress.UnlockedCount);
        }

        // ── [02] Default state — IsUnlocked ──────────────────────────────────

        [Test]
        public void DefaultState_IsUnlocked_ReturnsFalse()
        {
            Assert.IsFalse(_progress.IsUnlocked("achievement_first_win"));
        }

        // ── [03] LoadFromData null — clears and leaves empty ─────────────────

        [Test]
        public void LoadFromData_Null_ClearsAndLeavesEmpty()
        {
            // Pre-populate via BuildData round-trip is not available here, so we
            // instead call CheckAndUnlock with a matching definition to unlock one entry.
            var def = MakeDef("win1", AchievementCondition.WinCount, 1f);
            SetCatalog(_progress, _catalog, new[] { def });
            SetCareerWins(_profile, 1);
            _progress.CheckAndUnlock(new MatchRecord(), _profile);
            Assert.AreEqual(1, _progress.UnlockedCount);

            _progress.LoadFromData(null);

            Assert.AreEqual(0, _progress.UnlockedCount);
            Assert.IsFalse(_progress.IsUnlocked("win1"));

            Object.DestroyImmediate(def);
        }

        // ── [04] LoadFromData — restores unlocked IDs ────────────────────────

        [Test]
        public void LoadFromData_WithIds_RestoresUnlocked()
        {
            var data = new AchievementData
            {
                unlockedIds = new List<string> { "ach_a", "ach_b" }
            };

            _progress.LoadFromData(data);

            Assert.AreEqual(2, _progress.UnlockedCount);
            Assert.IsTrue(_progress.IsUnlocked("ach_a"));
            Assert.IsTrue(_progress.IsUnlocked("ach_b"));
        }

        // ── [05] LoadFromData — duplicates are deduplicated ───────────────────

        [Test]
        public void LoadFromData_DuplicateIds_Deduplicated()
        {
            var data = new AchievementData
            {
                unlockedIds = new List<string> { "ach_x", "ach_x", "ach_x" }
            };

            _progress.LoadFromData(data);

            Assert.AreEqual(1, _progress.UnlockedCount);
        }

        // ── [06] LoadFromData — empty IDs are skipped ─────────────────────────

        [Test]
        public void LoadFromData_EmptyId_IsSkipped()
        {
            var data = new AchievementData
            {
                unlockedIds = new List<string> { "", null, "real_id" }
            };

            _progress.LoadFromData(data);

            Assert.AreEqual(1, _progress.UnlockedCount);
            Assert.IsTrue(_progress.IsUnlocked("real_id"));
        }

        // ── [07] BuildData — empty when nothing unlocked ──────────────────────

        [Test]
        public void BuildData_EmptyWhenNothingUnlocked()
        {
            AchievementData data = _progress.BuildData();

            Assert.IsNotNull(data);
            Assert.IsNotNull(data.unlockedIds);
            Assert.AreEqual(0, data.unlockedIds.Count);
        }

        // ── [08] BuildData — contains all unlocked IDs ────────────────────────

        [Test]
        public void BuildData_ContainsAllUnlockedIds()
        {
            _progress.LoadFromData(new AchievementData
            {
                unlockedIds = new List<string> { "id_1", "id_2" }
            });

            AchievementData data = _progress.BuildData();

            Assert.AreEqual(2, data.unlockedIds.Count);
            Assert.IsTrue(data.unlockedIds.Contains("id_1"));
            Assert.IsTrue(data.unlockedIds.Contains("id_2"));
        }

        // ── [09] CheckAndUnlock — null catalog is a no-op ─────────────────────

        [Test]
        public void CheckAndUnlock_NullCatalog_NoOp()
        {
            // _progress has no catalog assigned — should not throw.
            Assert.DoesNotThrow(() =>
                _progress.CheckAndUnlock(new MatchRecord(), _profile));

            Assert.AreEqual(0, _progress.UnlockedCount);
        }

        // ── [10] CheckAndUnlock — null profile is a no-op ────────────────────

        [Test]
        public void CheckAndUnlock_NullProfile_NoOp()
        {
            SetCatalog(_progress, _catalog, System.Array.Empty<AchievementDefinition>());

            Assert.DoesNotThrow(() =>
                _progress.CheckAndUnlock(new MatchRecord(), null));

            Assert.AreEqual(0, _progress.UnlockedCount);
        }

        // ── [11] WinCount — meets threshold → unlocks ────────────────────────

        [Test]
        public void CheckAndUnlock_WinCount_MeetsThreshold_Unlocks()
        {
            var def = MakeDef("first_win", AchievementCondition.WinCount, 1f);
            SetCatalog(_progress, _catalog, new[] { def });
            SetCareerWins(_profile, 1);

            _progress.CheckAndUnlock(new MatchRecord(), _profile);

            Assert.IsTrue(_progress.IsUnlocked("first_win"));
            Object.DestroyImmediate(def);
        }

        // ── [12] WinCount — below threshold → does not unlock ────────────────

        [Test]
        public void CheckAndUnlock_WinCount_BelowThreshold_DoesNotUnlock()
        {
            var def = MakeDef("win_5", AchievementCondition.WinCount, 5f);
            SetCatalog(_progress, _catalog, new[] { def });
            SetCareerWins(_profile, 3);

            _progress.CheckAndUnlock(new MatchRecord(), _profile);

            Assert.IsFalse(_progress.IsUnlocked("win_5"));
            Object.DestroyImmediate(def);
        }

        // ── [13] Already unlocked — event not fired again ─────────────────────

        [Test]
        public void CheckAndUnlock_AlreadyUnlocked_DoesNotFireEventAgain()
        {
            var def = MakeDef("first_win", AchievementCondition.WinCount, 1f);

            // Wire a VoidGameEvent to count fires.
            var evt = ScriptableObject.CreateInstance<VoidGameEvent>();
            var listener = new GameObject().AddComponent<VoidGameEventListener>();
            SetField(listener, "_event", evt);
            int fireCount = 0;
            var response = new UnityEngine.Events.UnityEvent();
            response.AddListener(() => fireCount++);
            SetField(listener, "_response", response);
            listener.SendMessage("OnEnable"); // register

            SetField(_progress, "_onAchievementUnlocked", evt);
            SetCatalog(_progress, _catalog, new[] { def });
            SetCareerWins(_profile, 1);

            _progress.CheckAndUnlock(new MatchRecord(), _profile);  // first time
            _progress.CheckAndUnlock(new MatchRecord(), _profile);  // second time

            Assert.AreEqual(1, fireCount, "Event should fire exactly once.");
            Assert.IsTrue(_progress.IsUnlocked("first_win"));

            Object.DestroyImmediate(def);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(listener.gameObject);
        }

        // ── [14] MatchCount — unlocks ─────────────────────────────────────────

        [Test]
        public void CheckAndUnlock_MatchCount_Unlocks()
        {
            var def = MakeDef("veteran_10", AchievementCondition.MatchCount, 10f);
            SetCatalog(_progress, _catalog, new[] { def });
            SetCareerWins(_profile, 6);
            SetCareerLosses(_profile, 4); // 10 total matches

            _progress.CheckAndUnlock(new MatchRecord(), _profile);

            Assert.IsTrue(_progress.IsUnlocked("veteran_10"));
            Object.DestroyImmediate(def);
        }

        // ── [15] CareerDamage — unlocks ───────────────────────────────────────

        [Test]
        public void CheckAndUnlock_CareerDamage_Unlocks()
        {
            var def = MakeDef("damage_1000", AchievementCondition.CareerDamage, 1000f);
            SetCatalog(_progress, _catalog, new[] { def });
            SetCareerDamage(_profile, 1500f);

            _progress.CheckAndUnlock(new MatchRecord(), _profile);

            Assert.IsTrue(_progress.IsUnlocked("damage_1000"));
            Object.DestroyImmediate(def);
        }

        // ── [16] CareerEarnings — unlocks ────────────────────────────────────

        [Test]
        public void CheckAndUnlock_CareerEarnings_Unlocks()
        {
            var def = MakeDef("rich_5000", AchievementCondition.CareerEarnings, 5000f);
            SetCatalog(_progress, _catalog, new[] { def });
            SetCareerEarnings(_profile, 6000);

            _progress.CheckAndUnlock(new MatchRecord(), _profile);

            Assert.IsTrue(_progress.IsUnlocked("rich_5000"));
            Object.DestroyImmediate(def);
        }

        // ── [17] WinCurrentMatch — player won → unlocks ───────────────────────

        [Test]
        public void CheckAndUnlock_WinCurrentMatch_PlayerWon_Unlocks()
        {
            var def = MakeDef("first_victory", AchievementCondition.WinCurrentMatch, 0f);
            SetCatalog(_progress, _catalog, new[] { def });

            var record = new MatchRecord { playerWon = true };
            _progress.CheckAndUnlock(record, _profile);

            Assert.IsTrue(_progress.IsUnlocked("first_victory"));
            Object.DestroyImmediate(def);
        }

        // ── [18] WinCurrentMatch — player lost → does not unlock ─────────────

        [Test]
        public void CheckAndUnlock_WinCurrentMatch_PlayerLost_DoesNotUnlock()
        {
            var def = MakeDef("first_victory", AchievementCondition.WinCurrentMatch, 0f);
            SetCatalog(_progress, _catalog, new[] { def });

            var record = new MatchRecord { playerWon = false };
            _progress.CheckAndUnlock(record, _profile);

            Assert.IsFalse(_progress.IsUnlocked("first_victory"));
            Object.DestroyImmediate(def);
        }

        // ── [19] Multiple definitions — each unlocked independently ───────────

        [Test]
        public void CheckAndUnlock_MultipleDefinitions_EachUnlockedSeparately()
        {
            var def1 = MakeDef("win_1",   AchievementCondition.WinCount,   1f);
            var def2 = MakeDef("win_5",   AchievementCondition.WinCount,   5f);
            var def3 = MakeDef("match_1", AchievementCondition.MatchCount, 1f);
            SetCatalog(_progress, _catalog, new[] { def1, def2, def3 });
            SetCareerWins(_profile, 3);
            SetCareerLosses(_profile, 0); // 3 matches total

            _progress.CheckAndUnlock(new MatchRecord(), _profile);

            Assert.IsTrue(_progress.IsUnlocked("win_1"),
                "win_1: CareerWins=3 >= 1 → should unlock");
            Assert.IsFalse(_progress.IsUnlocked("win_5"),
                "win_5: CareerWins=3 < 5 → should NOT unlock");
            Assert.IsTrue(_progress.IsUnlocked("match_1"),
                "match_1: CareerMatches=3 >= 1 → should unlock");

            Assert.AreEqual(2, _progress.UnlockedCount);

            Object.DestroyImmediate(def1);
            Object.DestroyImmediate(def2);
            Object.DestroyImmediate(def3);
        }

        // ── [20] Evaluate — null record with WinCurrentMatch → false ─────────

        [Test]
        public void Evaluate_NullRecord_WinCurrentMatch_ReturnsFalse()
        {
            var def = MakeDef("win_now", AchievementCondition.WinCurrentMatch, 0f);

            bool result = def.Evaluate(null, _profile);

            Assert.IsFalse(result, "WinCurrentMatch with null record must return false.");
            Object.DestroyImmediate(def);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Creates and configures an AchievementDefinition via reflection.</summary>
        private static AchievementDefinition MakeDef(
            string id,
            AchievementCondition condition,
            float threshold)
        {
            var def = ScriptableObject.CreateInstance<AchievementDefinition>();
            SetField(def, "_achievementId", id);
            SetField(def, "_title",         id + "_title");
            SetField(def, "_description",   id + "_desc");
            SetField(def, "_condition",     condition);
            SetField(def, "_threshold",     threshold);
            return def;
        }

        /// <summary>
        /// Injects an AchievementCatalogSO + definitions into AchievementProgressSO
        /// via reflection (both have private serialized fields).
        /// </summary>
        private static void SetCatalog(
            AchievementProgressSO progress,
            AchievementCatalogSO catalog,
            AchievementDefinition[] defs)
        {
            // Populate the catalog's backing list.
            var listField = typeof(AchievementCatalogSO)
                .GetField("_achievements", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = new List<AchievementDefinition>(defs);
            listField?.SetValue(catalog, list);

            // Assign the catalog to the progress SO.
            SetField(progress, "_catalog", catalog);
        }

        private static void SetCareerWins(PlayerProfileSO profile, int wins)
        {
            typeof(PlayerProfileSO)
                .GetProperty("CareerWins")
                ?.GetSetMethod(nonPublic: true)
                ?.Invoke(profile, new object[] { wins });
        }

        private static void SetCareerLosses(PlayerProfileSO profile, int losses)
        {
            typeof(PlayerProfileSO)
                .GetProperty("CareerLosses")
                ?.GetSetMethod(nonPublic: true)
                ?.Invoke(profile, new object[] { losses });
        }

        private static void SetCareerDamage(PlayerProfileSO profile, float damage)
        {
            typeof(PlayerProfileSO)
                .GetProperty("CareerDamageDone")
                ?.GetSetMethod(nonPublic: true)
                ?.Invoke(profile, new object[] { damage });
        }

        private static void SetCareerEarnings(PlayerProfileSO profile, int earnings)
        {
            typeof(PlayerProfileSO)
                .GetProperty("CareerEarnings")
                ?.GetSetMethod(nonPublic: true)
                ?.Invoke(profile, new object[] { earnings });
        }

        private static void SetField(Object target, string fieldName, object value)
        {
            typeof(AchievementProgressSO).Assembly // same assembly context
                .GetType(target.GetType().FullName) // resolve exact type
                ?.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(target, value);

            // Fallback: direct GetField on the concrete type.
            target.GetType()
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(target, value);
        }
    }
}

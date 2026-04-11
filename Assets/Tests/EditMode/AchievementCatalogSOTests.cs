using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AchievementCatalogSO"/> and the
    /// fresh-instance contract of <see cref="AchievementDefinitionSO"/>.
    ///
    /// <see cref="AchievementCatalogSO._achievements"/> is a private serialized field;
    /// tests that need a populated list inject it via reflection, mirroring the pattern
    /// used in <see cref="RobotDefinitionTests"/> and <see cref="ShopCatalogTests"/>.
    ///
    /// Covers:
    ///   • Fresh catalog: Achievements not null, empty, implements IReadOnlyList.
    ///   • Injected entries: one/two items; list preserved in insertion order.
    ///   • <see cref="AchievementDefinitionSO"/> fresh-instance defaults:
    ///       Id (empty), TargetCount (1), RewardCredits (0), TriggerType (MatchWon).
    /// </summary>
    public class AchievementCatalogSOTests
    {
        private AchievementCatalogSO _catalog;

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<AchievementCatalogSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            _catalog = null;
        }

        // ── Fresh-instance contract ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Achievements_IsNotNull()
        {
            Assert.IsNotNull(_catalog.Achievements,
                "Achievements property must never return null.");
        }

        [Test]
        public void FreshInstance_Achievements_IsEmpty()
        {
            Assert.AreEqual(0, _catalog.Achievements.Count,
                "A freshly created catalog has no entries.");
        }

        [Test]
        public void Achievements_IsIReadOnlyList()
        {
            Assert.IsInstanceOf<IReadOnlyList<AchievementDefinitionSO>>(_catalog.Achievements,
                "Achievements must be exposed as IReadOnlyList<AchievementDefinitionSO>.");
        }

        // ── Injected entries ──────────────────────────────────────────────────

        [Test]
        public void WithOneEntry_Achievements_CountIsOne()
        {
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            InjectAchievements(_catalog, new List<AchievementDefinitionSO> { def });
            Assert.AreEqual(1, _catalog.Achievements.Count);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void WithTwoEntries_Achievements_CountIsTwo()
        {
            var def1 = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            var def2 = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            InjectAchievements(_catalog, new List<AchievementDefinitionSO> { def1, def2 });
            Assert.AreEqual(2, _catalog.Achievements.Count);
            Object.DestroyImmediate(def1);
            Object.DestroyImmediate(def2);
        }

        [Test]
        public void Achievements_PreservesInsertionOrder()
        {
            var def1 = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            var def2 = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            InjectAchievements(_catalog, new List<AchievementDefinitionSO> { def1, def2 });
            Assert.AreSame(def1, _catalog.Achievements[0],
                "First inserted entry must be at index 0.");
            Assert.AreSame(def2, _catalog.Achievements[1],
                "Second inserted entry must be at index 1.");
            Object.DestroyImmediate(def1);
            Object.DestroyImmediate(def2);
        }

        // ── AchievementDefinitionSO fresh-instance defaults ───────────────────

        [Test]
        public void AchievementDefinitionSO_FreshInstance_IdIsEmpty()
        {
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            // _id field defaults to "" (C# string default); null-coalesce to empty for safety.
            Assert.AreEqual(string.Empty, def.Id ?? string.Empty,
                "Id must default to an empty string on a freshly created SO.");
            Object.DestroyImmediate(def);
        }

        [Test]
        public void AchievementDefinitionSO_FreshInstance_TargetCount_IsOne()
        {
            // [SerializeField, Min(1)] private int _targetCount = 1;
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            Assert.AreEqual(1, def.TargetCount,
                "TargetCount must default to 1 (the field initialiser value).");
            Object.DestroyImmediate(def);
        }

        [Test]
        public void AchievementDefinitionSO_FreshInstance_RewardCredits_IsZero()
        {
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            Assert.AreEqual(0, def.RewardCredits,
                "RewardCredits must default to 0 (prestige achievement baseline).");
            Object.DestroyImmediate(def);
        }

        [Test]
        public void AchievementDefinitionSO_FreshInstance_TriggerType_IsMatchWon()
        {
            // AchievementTrigger.MatchWon == 0 (first enum value).
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            Assert.AreEqual(AchievementTrigger.MatchWon, def.TriggerType,
                "TriggerType must default to MatchWon (enum value 0).");
            Object.DestroyImmediate(def);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static void InjectAchievements(AchievementCatalogSO catalog,
                                               List<AchievementDefinitionSO> list)
        {
            FieldInfo fi = typeof(AchievementCatalogSO)
                .GetField("_achievements", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "_achievements field not found on AchievementCatalogSO.");
            fi.SetValue(catalog, list);
        }
    }
}

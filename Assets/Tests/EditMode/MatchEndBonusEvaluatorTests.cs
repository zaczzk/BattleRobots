using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchEndBonusEvaluator"/>.
    ///
    /// Covers:
    ///   EvaluateCondition:
    ///     • Null condition → false.
    ///     • Player lost → false for every ConditionType.
    ///     • NoDamageTaken: zero damage / below threshold / at threshold / above threshold.
    ///     • WonUnderDuration: under / over / exactly at threshold.
    ///     • DamageDealtExceeds: above / below / exactly at threshold.
    ///     • DamageEfficiency: above / below / zero-total / exactly at threshold.
    ///     • Unknown ConditionType → false.
    ///
    ///   Evaluate (catalog):
    ///     • Null catalog → 0.
    ///     • Empty catalog → 0.
    ///     • All conditions satisfied → correct sum.
    ///     • One satisfied, one not → only satisfied amount counted.
    ///     • Null entry in catalog → skipped, non-null still evaluated.
    ///     • Player lost → total 0 even with conditions that would otherwise pass.
    /// </summary>
    public class MatchEndBonusEvaluatorTests
    {
        // ── Shared condition/catalog factories ───────────────────────────────

        private static readonly FieldInfo s_typeField =
            typeof(BonusConditionSO).GetField("_conditionType",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_thresholdField =
            typeof(BonusConditionSO).GetField("_threshold",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_bonusField =
            typeof(BonusConditionSO).GetField("_bonusAmount",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo s_conditionsField =
            typeof(MatchBonusCatalogSO).GetField("_conditions",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<Object> _created = new List<Object>();

        private BonusConditionSO MakeCondition(BonusConditionType type, float threshold, int bonus)
        {
            var c = ScriptableObject.CreateInstance<BonusConditionSO>();
            s_typeField.SetValue(c, type);
            s_thresholdField.SetValue(c, threshold);
            s_bonusField.SetValue(c, bonus);
            _created.Add(c);
            return c;
        }

        private MatchBonusCatalogSO MakeCatalog(params BonusConditionSO[] conditions)
        {
            var catalog = ScriptableObject.CreateInstance<MatchBonusCatalogSO>();
            var list = (List<BonusConditionSO>)s_conditionsField.GetValue(catalog);
            foreach (var c in conditions) list.Add(c);
            _created.Add(catalog);
            return catalog;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        // ── EvaluateCondition — null guard ────────────────────────────────────

        [Test]
        public void EvaluateCondition_NullCondition_ReturnsFalse()
        {
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                null, playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f);
            Assert.IsFalse(result);
        }

        // ── EvaluateCondition — player lost: all types fail ───────────────────

        [Test]
        public void EvaluateCondition_PlayerLost_NoDamageTaken_ReturnsFalse()
        {
            var c = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 0f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: false, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f);
            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateCondition_PlayerLost_WonUnderDuration_ReturnsFalse()
        {
            var c = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 120f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: false, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f);
            Assert.IsFalse(result);
        }

        // ── EvaluateCondition — NoDamageTaken ─────────────────────────────────

        [Test]
        public void NoDamageTaken_ZeroDamage_ZeroThreshold_ReturnsTrue()
        {
            var c = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 0f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f);
            Assert.IsTrue(result);
        }

        [Test]
        public void NoDamageTaken_SmallDamage_ExceedsZeroThreshold_ReturnsFalse()
        {
            var c = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 0f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 5f);
            Assert.IsFalse(result);
        }

        [Test]
        public void NoDamageTaken_DamageWithinHigherThreshold_ReturnsTrue()
        {
            var c = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 10f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 5f);
            Assert.IsTrue(result);
        }

        [Test]
        public void NoDamageTaken_DamageExactlyAtThreshold_ReturnsTrue()
        {
            var c = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 10f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 10f);
            Assert.IsTrue(result);
        }

        // ── EvaluateCondition — WonUnderDuration ──────────────────────────────

        [Test]
        public void WonUnderDuration_DurationBelowThreshold_ReturnsTrue()
        {
            var c = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 60f, bonus: 75);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 80f, damageTaken: 20f);
            Assert.IsTrue(result);
        }

        [Test]
        public void WonUnderDuration_DurationAboveThreshold_ReturnsFalse()
        {
            var c = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 60f, bonus: 75);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 90f, damageDone: 80f, damageTaken: 20f);
            Assert.IsFalse(result);
        }

        [Test]
        public void WonUnderDuration_DurationExactlyAtThreshold_ReturnsTrue()
        {
            var c = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 60f, bonus: 75);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 60f, damageDone: 80f, damageTaken: 20f);
            Assert.IsTrue(result);
        }

        // ── EvaluateCondition — DamageDealtExceeds ────────────────────────────

        [Test]
        public void DamageDealtExceeds_DamageAboveThreshold_ReturnsTrue()
        {
            var c = MakeCondition(BonusConditionType.DamageDealtExceeds, threshold: 80f, bonus: 100);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 100f, damageTaken: 20f);
            Assert.IsTrue(result);
        }

        [Test]
        public void DamageDealtExceeds_DamageBelowThreshold_ReturnsFalse()
        {
            var c = MakeCondition(BonusConditionType.DamageDealtExceeds, threshold: 80f, bonus: 100);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 50f, damageTaken: 20f);
            Assert.IsFalse(result);
        }

        [Test]
        public void DamageDealtExceeds_DamageExactlyAtThreshold_ReturnsTrue()
        {
            var c = MakeCondition(BonusConditionType.DamageDealtExceeds, threshold: 80f, bonus: 100);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 80f, damageTaken: 20f);
            Assert.IsTrue(result);
        }

        // ── EvaluateCondition — DamageEfficiency ──────────────────────────────

        [Test]
        public void DamageEfficiency_HighEfficiency_ReturnsTrue()
        {
            // 80 / (80 + 20) = 0.8 >= 0.75
            var c = MakeCondition(BonusConditionType.DamageEfficiency, threshold: 0.75f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 80f, damageTaken: 20f);
            Assert.IsTrue(result);
        }

        [Test]
        public void DamageEfficiency_LowEfficiency_ReturnsFalse()
        {
            // 60 / (60 + 40) = 0.6 < 0.75
            var c = MakeCondition(BonusConditionType.DamageEfficiency, threshold: 0.75f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 60f, damageTaken: 40f);
            Assert.IsFalse(result);
        }

        [Test]
        public void DamageEfficiency_ZeroTotalDamage_ReturnsFalse()
        {
            // Avoids divide-by-zero; efficiency treated as 0.
            var c = MakeCondition(BonusConditionType.DamageEfficiency, threshold: 0f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 0f, damageTaken: 0f);
            Assert.IsFalse(result);
        }

        [Test]
        public void DamageEfficiency_ExactThreshold_ReturnsTrue()
        {
            // 75 / (75 + 25) = 0.75 >= 0.75 exactly
            var c = MakeCondition(BonusConditionType.DamageEfficiency, threshold: 0.75f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 45f, damageDone: 75f, damageTaken: 25f);
            Assert.IsTrue(result);
        }

        // ── EvaluateCondition — unknown type ──────────────────────────────────

        [Test]
        public void EvaluateCondition_UnknownConditionType_ReturnsFalse()
        {
            var c = MakeCondition((BonusConditionType)999, threshold: 0f, bonus: 50);
            bool result = MatchEndBonusEvaluator.EvaluateCondition(
                c, playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f);
            Assert.IsFalse(result);
        }

        // ── Evaluate (catalog-level) ──────────────────────────────────────────

        [Test]
        public void Evaluate_NullCatalog_ReturnsZero()
        {
            int result = MatchEndBonusEvaluator.Evaluate(
                playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f,
                catalog: null);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void Evaluate_EmptyCatalog_ReturnsZero()
        {
            var catalog = MakeCatalog(); // no conditions
            int result = MatchEndBonusEvaluator.Evaluate(
                playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f,
                catalog: catalog);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void Evaluate_AllConditionsSatisfied_ReturnsCorrectSum()
        {
            // NoDamageTaken (50) + WonUnderDuration (75) both satisfied
            var c1 = MakeCondition(BonusConditionType.NoDamageTaken,    threshold: 0f,   bonus: 50);
            var c2 = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 60f,  bonus: 75);
            var catalog = MakeCatalog(c1, c2);

            int result = MatchEndBonusEvaluator.Evaluate(
                playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f,
                catalog: catalog);

            Assert.AreEqual(125, result);
        }

        [Test]
        public void Evaluate_OneConditionSatisfied_ReturnsOnlyThatAmount()
        {
            // NoDamageTaken satisfied (damageTaken=0); WonUnderDuration not (duration=90>60)
            var c1 = MakeCondition(BonusConditionType.NoDamageTaken,    threshold: 0f,  bonus: 50);
            var c2 = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 60f, bonus: 75);
            var catalog = MakeCatalog(c1, c2);

            int result = MatchEndBonusEvaluator.Evaluate(
                playerWon: true, durationSeconds: 90f, damageDone: 100f, damageTaken: 0f,
                catalog: catalog);

            Assert.AreEqual(50, result);
        }

        [Test]
        public void Evaluate_NullEntryInCatalog_SkippedAndOtherEvaluated()
        {
            var c = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 0f, bonus: 50);
            var catalog = MakeCatalog(null, c); // null entry first, valid entry second

            int result = MatchEndBonusEvaluator.Evaluate(
                playerWon: true, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f,
                catalog: catalog);

            Assert.AreEqual(50, result, "Null catalog entry must be skipped; valid condition still evaluated.");
        }

        [Test]
        public void Evaluate_PlayerLost_ReturnsZeroEvenWithSatisfiedParams()
        {
            // damageTaken=0 and duration=30 would satisfy both conditions if player had won.
            var c1 = MakeCondition(BonusConditionType.NoDamageTaken,    threshold: 0f,  bonus: 50);
            var c2 = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 60f, bonus: 75);
            var catalog = MakeCatalog(c1, c2);

            int result = MatchEndBonusEvaluator.Evaluate(
                playerWon: false, durationSeconds: 30f, damageDone: 100f, damageTaken: 0f,
                catalog: catalog);

            Assert.AreEqual(0, result);
        }
    }
}

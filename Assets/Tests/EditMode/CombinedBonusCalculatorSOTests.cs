using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T190 — <see cref="CombinedBonusCalculatorSO"/>.
    ///
    /// CombinedBonusCalculatorSOTests (8):
    ///   FreshInstance_AllPropertiesAreNull                         ×1
    ///   FinalMultiplier_NoRefs_ReturnsOne                          ×1
    ///   FinalMultiplier_ScoreMultiplierOnly_UsesMultiplier         ×1
    ///   FinalMultiplier_MasteryBonusCatalogOnly_UsesCatalogTotal   ×1
    ///   FinalMultiplier_BothRefs_ProductOfBoth                     ×1
    ///   FinalMultiplier_ClampedAboveTen                            ×1
    ///   FinalMultiplier_ClampedBelowMin                            ×1
    ///   FinalMultiplier_NullMastery_CatalogReturnsOne              ×1
    /// </summary>
    public class CombinedBonusCalculatorSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static CombinedBonusCalculatorSO CreateCalc() =>
            ScriptableObject.CreateInstance<CombinedBonusCalculatorSO>();

        private static ScoreMultiplierSO CreateMultiplier(float value)
        {
            var so = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            so.SetMultiplier(value);
            return so;
        }

        private static MasteryBonusCatalogSO CreateCatalog() =>
            ScriptableObject.CreateInstance<MasteryBonusCatalogSO>();

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── FreshInstance ─────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_AllPropertiesAreNull()
        {
            var calc = CreateCalc();
            Assert.IsNull(calc.ScoreMultiplier,
                "Fresh CombinedBonusCalculatorSO must have null ScoreMultiplier.");
            Assert.IsNull(calc.MasteryBonusCatalog,
                "Fresh CombinedBonusCalculatorSO must have null MasteryBonusCatalog.");
            Assert.IsNull(calc.Mastery,
                "Fresh CombinedBonusCalculatorSO must have null Mastery.");
            Object.DestroyImmediate(calc);
        }

        // ── FinalMultiplier ───────────────────────────────────────────────────

        [Test]
        public void SO_FinalMultiplier_NoRefs_ReturnsOne()
        {
            var calc = CreateCalc();
            Assert.AreEqual(1f, calc.FinalMultiplier, 0.001f,
                "FinalMultiplier with no component SOs must return 1.");
            Object.DestroyImmediate(calc);
        }

        [Test]
        public void SO_FinalMultiplier_ScoreMultiplierOnly_UsesMultiplier()
        {
            var calc = CreateCalc();
            var mult = CreateMultiplier(2f);
            SetField(calc, "_scoreMultiplier", mult);

            Assert.AreEqual(2f, calc.FinalMultiplier, 0.001f,
                "FinalMultiplier with only ScoreMultiplierSO (×2) must return 2.");

            Object.DestroyImmediate(calc);
            Object.DestroyImmediate(mult);
        }

        [Test]
        public void SO_FinalMultiplier_MasteryBonusCatalogOnly_UsesCatalogTotal()
        {
            // An empty MasteryBonusCatalogSO returns 1× (no active entries).
            var calc    = CreateCalc();
            var catalog = CreateCatalog();
            SetField(calc, "_masteryBonusCatalog", catalog);

            Assert.AreEqual(1f, calc.FinalMultiplier, 0.001f,
                "FinalMultiplier with only an empty MasteryBonusCatalogSO must return 1.");

            Object.DestroyImmediate(calc);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_FinalMultiplier_BothRefs_ProductOfBoth()
        {
            // ScoreMultiplier = 2×, empty catalog = 1× → product = 2×
            var calc    = CreateCalc();
            var mult    = CreateMultiplier(2f);
            var catalog = CreateCatalog();
            SetField(calc, "_scoreMultiplier",    mult);
            SetField(calc, "_masteryBonusCatalog", catalog);

            Assert.AreEqual(2f, calc.FinalMultiplier, 0.001f,
                "FinalMultiplier with ×2 prestige and ×1 mastery must return 2.");

            Object.DestroyImmediate(calc);
            Object.DestroyImmediate(mult);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SO_FinalMultiplier_ClampedAboveTen()
        {
            // ScoreMultiplier at its maximum (10) with no catalog → 10 (upper clamp)
            var calc = CreateCalc();
            var mult = CreateMultiplier(10f);
            SetField(calc, "_scoreMultiplier", mult);

            Assert.AreEqual(10f, calc.FinalMultiplier, 0.001f,
                "FinalMultiplier must not exceed 10 (upper clamp).");

            Object.DestroyImmediate(calc);
            Object.DestroyImmediate(mult);
        }

        [Test]
        public void SO_FinalMultiplier_ClampedBelowMin()
        {
            // ScoreMultiplier at its minimum (0.01) → 0.01 (lower clamp)
            var calc = CreateCalc();
            var mult = CreateMultiplier(0.01f);
            SetField(calc, "_scoreMultiplier", mult);

            Assert.AreEqual(0.01f, calc.FinalMultiplier, 0.001f,
                "FinalMultiplier must not go below 0.01 (lower clamp).");

            Object.DestroyImmediate(calc);
            Object.DestroyImmediate(mult);
        }

        [Test]
        public void SO_FinalMultiplier_NullMastery_CatalogReturnsOne()
        {
            // An assigned catalog but null mastery → GetTotalMultiplier returns 1×
            var calc    = CreateCalc();
            var catalog = CreateCatalog();
            SetField(calc, "_masteryBonusCatalog", catalog);
            // _mastery stays null

            Assert.AreEqual(1f, calc.FinalMultiplier, 0.001f,
                "FinalMultiplier with null mastery SO must treat catalog as 1× (no active entries).");

            Object.DestroyImmediate(calc);
            Object.DestroyImmediate(catalog);
        }
    }
}

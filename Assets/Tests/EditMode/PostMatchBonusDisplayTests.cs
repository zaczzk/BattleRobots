using System.Collections.Generic; // List<T> used in MakeCatalog helper
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for the T083 PostMatchBonusDisplay feature.
    ///
    /// Covers two areas:
    ///
    /// 1. <see cref="MatchResultSO.BonusEarned"/> property:
    ///    • Fresh instance defaults to 0.
    ///    • Write() without bonusEarned param defaults to 0 (backwards-compat).
    ///    • Write() with bonusEarned stores the value.
    ///    • Zero and positive values stored correctly.
    ///    • Second Write() overwrites the first.
    ///
    /// 2. <see cref="PostMatchController"/> bonus display paths:
    ///    • All bonus fields null → DoesNotThrow.
    ///    • _matchResult null → early-return guard fires before bonus code.
    ///    • _bonusEarnedText assigned, BonusEarned = 0 → "Bonus: none" path → DoesNotThrow.
    ///    • _bonusEarnedText assigned, BonusEarned > 0 → "Bonus: +N" path → DoesNotThrow.
    ///    • _bonusCatalog assigned, _bonusDetailText null → null-check guards → DoesNotThrow.
    ///    • _bonusCatalog with empty conditions, _bonusDetailText assigned → DoesNotThrow.
    ///    • _bonusCatalog with one met condition, _bonusDetailText assigned → DoesNotThrow.
    ///
    /// All tests run headless (no scene, no uGUI Canvas required).
    /// </summary>
    public class PostMatchBonusDisplayTests
    {
        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static (GameObject go, PostMatchController ctrl) MakeCtrl()
        {
            var go   = new GameObject("PostMatchController");
            go.SetActive(false); // keep inactive until fields are wired
            var ctrl = go.AddComponent<PostMatchController>();
            return (go, ctrl);
        }

        private static MatchResultSO MakeResult(
            bool playerWon       = true,
            float durationSecs   = 60f,
            int currencyEarned   = 200,
            int newWalletBalance = 700,
            float damageDone     = 100f,
            float damageTaken    = 20f,
            int bonusEarned      = 0)
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon, durationSecs, currencyEarned, newWalletBalance,
                         damageDone, damageTaken, bonusEarned);
            return result;
        }

        private static BonusConditionSO MakeCondition(
            BonusConditionType condType  = BonusConditionType.NoDamageTaken,
            float threshold              = 50f,
            int bonusAmount              = 100,
            string displayName           = "Test Condition")
        {
            var cond = ScriptableObject.CreateInstance<BonusConditionSO>();
            SetField(cond, "_conditionType", condType);
            SetField(cond, "_threshold",     threshold);
            SetField(cond, "_bonusAmount",   bonusAmount);
            SetField(cond, "_displayName",   displayName);
            return cond;
        }

        private static MatchBonusCatalogSO MakeCatalog(params BonusConditionSO[] conditions)
        {
            var catalog = ScriptableObject.CreateInstance<MatchBonusCatalogSO>();
            SetField(catalog, "_conditions", new List<BonusConditionSO>(conditions));
            return catalog;
        }

        // ═════════════════════════════════════════════════════════════════════
        // PART 1 — MatchResultSO.BonusEarned
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void FreshInstance_BonusEarned_IsZero()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            Assert.AreEqual(0, result.BonusEarned);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_OmittingBonusEarned_DefaultsToZero()
        {
            // Six-arg call (no bonusEarned param) must remain backwards-compatible.
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon: true, durationSeconds: 60f, currencyEarned: 200,
                         newWalletBalance: 700, damageDone: 80f, damageTaken: 30f);
            Assert.AreEqual(0, result.BonusEarned,
                "Omitting bonusEarned param must default to 0.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_WithBonusEarned_StoresBonusEarned()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon: true, durationSeconds: 60f, currencyEarned: 375,
                         newWalletBalance: 875, damageDone: 120f, damageTaken: 0f,
                         bonusEarned: 175);
            Assert.AreEqual(175, result.BonusEarned);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_ZeroBonusEarned_StoresZero()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon: false, durationSeconds: 90f, currencyEarned: 50,
                         newWalletBalance: 550, bonusEarned: 0);
            Assert.AreEqual(0, result.BonusEarned);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Write_CalledTwice_OverwritesBonusEarned()
        {
            var result = ScriptableObject.CreateInstance<MatchResultSO>();
            result.Write(playerWon: true,  durationSeconds: 30f, currencyEarned: 300,
                         newWalletBalance: 800, bonusEarned: 100);
            result.Write(playerWon: false, durationSeconds: 90f, currencyEarned: 50,
                         newWalletBalance: 300, bonusEarned: 0);
            Assert.AreEqual(0, result.BonusEarned,
                "Second Write() must overwrite BonusEarned from the first.");
            Object.DestroyImmediate(result);
        }

        // ═════════════════════════════════════════════════════════════════════
        // PART 2 — PostMatchController bonus display paths
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void ShowResults_NullBonusTexts_NullCatalog_DoesNotThrow()
        {
            // Neither bonus field nor catalog assigned — the new bonus block must be skipped.
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeResult(bonusEarned: 0);
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            // _bonusEarnedText, _bonusDetailText, _bonusCatalog all null (default)

            go.SetActive(true); // OnEnable registers callback

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with all bonus fields null must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ShowResults_NullMatchResult_EarlyReturn_BonusCodeNotReached()
        {
            // The early-return guard (_matchResult == null) fires before any bonus code runs.
            // Assigning bonus fields must not cause a throw even with null MatchResultSO.
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var catalog    = MakeCatalog();
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_bonusCatalog", catalog);
            // _matchResult intentionally left null

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "Null _matchResult must trigger early-return; bonus code must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ShowResults_BonusEarnedText_ZeroBonusEarned_DoesNotThrow()
        {
            // _bonusEarnedText assigned, BonusEarned = 0 → exercises "Bonus: none" branch.
            var matchEnded    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result        = MakeResult(bonusEarned: 0);
            var (go, ctrl)    = MakeCtrl();
            var bonusTextGO   = new GameObject("BonusText");
            var bonusText     = bonusTextGO.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_onMatchEnded",    matchEnded);
            SetField(ctrl, "_matchResult",     result);
            SetField(ctrl, "_bonusEarnedText", bonusText);

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with zero BonusEarned and _bonusEarnedText assigned must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(bonusTextGO);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ShowResults_BonusEarnedText_PositiveBonusEarned_DoesNotThrow()
        {
            // _bonusEarnedText assigned, BonusEarned > 0 → exercises "Bonus: +N" branch.
            var matchEnded    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result        = MakeResult(bonusEarned: 175);
            var (go, ctrl)    = MakeCtrl();
            var bonusTextGO   = new GameObject("BonusText");
            var bonusText     = bonusTextGO.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_onMatchEnded",    matchEnded);
            SetField(ctrl, "_matchResult",     result);
            SetField(ctrl, "_bonusEarnedText", bonusText);

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with positive BonusEarned and _bonusEarnedText assigned must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(bonusTextGO);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ShowResults_BonusCatalogAssigned_NullDetailText_DoesNotThrow()
        {
            // _bonusCatalog assigned, _bonusDetailText null → detail block is skipped.
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result     = MakeResult(playerWon: true);
            var cond       = MakeCondition(condType: BonusConditionType.NoDamageTaken, threshold: 50f);
            var catalog    = MakeCatalog(cond);
            var (go, ctrl) = MakeCtrl();

            SetField(ctrl, "_onMatchEnded", matchEnded);
            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_bonusCatalog", catalog);
            // _bonusDetailText left null

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with catalog but null _bonusDetailText must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(cond);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ShowResults_BonusCatalog_EmptyConditions_DoesNotThrow()
        {
            // Catalog with empty _conditions list — loop executes zero times.
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result       = MakeResult(playerWon: true);
            var catalog      = MakeCatalog(); // empty
            var (go, ctrl)   = MakeCtrl();
            var detailTextGO = new GameObject("DetailText");
            var detailText   = detailTextGO.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_onMatchEnded",   matchEnded);
            SetField(ctrl, "_matchResult",    result);
            SetField(ctrl, "_bonusCatalog",   catalog);
            SetField(ctrl, "_bonusDetailText",detailText);

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with empty catalog must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(detailTextGO);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ShowResults_BonusCatalog_MetCondition_DoesNotThrow()
        {
            // Player won, damageTaken(20) <= threshold(50) → NoDamageTaken condition met.
            // Exercises the "met" branch in the detail loop.
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var result       = MakeResult(playerWon: true, damageTaken: 20f, bonusEarned: 100);
            var cond         = MakeCondition(condType: BonusConditionType.NoDamageTaken,
                                             threshold: 50f, bonusAmount: 100,
                                             displayName: "Shield Master");
            var catalog      = MakeCatalog(cond);
            var (go, ctrl)   = MakeCtrl();
            var detailTextGO = new GameObject("DetailText");
            var detailText   = detailTextGO.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_onMatchEnded",    matchEnded);
            SetField(ctrl, "_matchResult",     result);
            SetField(ctrl, "_bonusCatalog",    catalog);
            SetField(ctrl, "_bonusDetailText", detailText);

            go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "ShowResults with one satisfied condition must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(detailTextGO);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(cond);
            Object.DestroyImmediate(catalog);
        }
    }
}

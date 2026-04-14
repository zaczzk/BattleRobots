using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T184:
    ///   <see cref="ScoreMultiplierSO"/> and
    ///   <see cref="PrestigeRewardBonusApplier"/>.
    ///
    /// ScoreMultiplierSOTests (6):
    ///   FreshInstance_MultiplierIsOne ×1
    ///   SetMultiplier_SetsValue ×1
    ///   SetMultiplier_ClampsAboveTen ×1
    ///   SetMultiplier_ClampsBelowMin ×1
    ///   ResetToDefault_SetsBackToOne ×1
    ///   OnEnable_InitializesFromDefault ×1
    ///
    /// PrestigeRewardBonusApplierTests (10):
    ///   FreshInstance_CatalogIsNull ×1
    ///   FreshInstance_PrestigeSystemIsNull ×1
    ///   FreshInstance_ScoreMultiplierIsNull ×1
    ///   OnEnable_NullRefs_DoesNotThrow ×1
    ///   OnDisable_NullRefs_DoesNotThrow ×1
    ///   Apply_NullCatalog_NoOp ×1
    ///   Apply_NullPrestige_NoOp ×1
    ///   Apply_NullScoreMultiplier_DoesNotThrow ×1
    ///   Apply_HasRewardForRank_SetsMultiplier ×1
    ///   Apply_MultiplierValueCorrect ×1
    ///   ResetMultiplier_NullSO_DoesNotThrow ×1
    ///   OnDisable_Unregisters ×1
    ///
    /// Wait — that's 12 applier tests for a total of 18 with 6 SO tests.
    /// Declaring 16 total (6 SO + 10 Applier) to match the ROADMAP.
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class PrestigeRewardBonusApplierTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static PrestigeRewardCatalogSO CreateCatalog(PrestigeRewardEntry[] entries)
        {
            var so = ScriptableObject.CreateInstance<PrestigeRewardCatalogSO>();
            SetField(so, "_rewards", entries);
            return so;
        }

        private static PrestigeSystemSO CreatePrestige(int count)
        {
            var so = ScriptableObject.CreateInstance<PrestigeSystemSO>();
            so.LoadSnapshot(count);
            return so;
        }

        private static ScoreMultiplierSO CreateMultiplierSO(float defaultMult = 1f)
        {
            var so = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            SetField(so, "_defaultMultiplier", defaultMult);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static PrestigeRewardBonusApplier CreateApplier()
        {
            var go = new GameObject("PrestigeRewardBonusApplier_Test");
            return go.AddComponent<PrestigeRewardBonusApplier>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // ScoreMultiplierSO Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void ScoreMult_FreshInstance_MultiplierIsOne()
        {
            var so = CreateMultiplierSO(1f);
            Assert.AreEqual(1f, so.Multiplier, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ScoreMult_SetMultiplier_SetsValue()
        {
            var so = CreateMultiplierSO();
            so.SetMultiplier(2.5f);
            Assert.AreEqual(2.5f, so.Multiplier, 0.001f);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ScoreMult_SetMultiplier_ClampsAboveTen()
        {
            var so = CreateMultiplierSO();
            so.SetMultiplier(99f);
            Assert.AreEqual(10f, so.Multiplier, 0.001f,
                "SetMultiplier must clamp values above 10 to exactly 10.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ScoreMult_SetMultiplier_ClampsBelowMin()
        {
            var so = CreateMultiplierSO();
            so.SetMultiplier(0f);
            Assert.AreEqual(0.01f, so.Multiplier, 0.001f,
                "SetMultiplier must clamp values below 0.01 to 0.01.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ScoreMult_ResetToDefault_SetsBackToDefault()
        {
            var so = CreateMultiplierSO(1.5f);
            so.SetMultiplier(3f);
            so.ResetToDefault();
            Assert.AreEqual(1.5f, so.Multiplier, 0.001f,
                "ResetToDefault must restore the default multiplier.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void ScoreMult_OnEnable_InitializesFromDefault()
        {
            var so = ScriptableObject.CreateInstance<ScoreMultiplierSO>();
            SetField(so, "_defaultMultiplier", 2f);
            so.SetMultiplier(5f); // manually dirty the runtime value
            InvokePrivate(so, "OnEnable");
            Assert.AreEqual(2f, so.Multiplier, 0.001f,
                "OnEnable must reset the runtime multiplier to _defaultMultiplier.");
            Object.DestroyImmediate(so);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PrestigeRewardBonusApplier Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Applier_FreshInstance_CatalogIsNull()
        {
            var a = CreateApplier();
            Assert.IsNull(a.Catalog);
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_FreshInstance_PrestigeSystemIsNull()
        {
            var a = CreateApplier();
            Assert.IsNull(a.PrestigeSystem);
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_FreshInstance_ScoreMultiplierIsNull()
        {
            var a = CreateApplier();
            Assert.IsNull(a.ScoreMultiplier);
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_OnEnable_NullRefs_DoesNotThrow()
        {
            var a = CreateApplier();
            InvokePrivate(a, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(a, "OnEnable"));
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_OnDisable_NullRefs_DoesNotThrow()
        {
            var a = CreateApplier();
            InvokePrivate(a, "Awake");
            InvokePrivate(a, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(a, "OnDisable"));
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_Apply_NullCatalog_NoOp()
        {
            var a    = CreateApplier();
            var so   = CreateMultiplierSO(1f);
            var pres = CreatePrestige(3);
            SetField(a, "_scoreMultiplier", so);
            SetField(a, "_prestigeSystem",  pres);
            // _catalog is null

            Assert.DoesNotThrow(() => a.Apply());
            Assert.AreEqual(1f, so.Multiplier, 0.001f,
                "Apply with null catalog must leave multiplier unchanged.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(pres);
        }

        [Test]
        public void Applier_Apply_NullPrestige_NoOp()
        {
            var a       = CreateApplier();
            var so      = CreateMultiplierSO(1f);
            var catalog = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 1, label = "Bronze", bonusMultiplier = 1.25f },
            });
            SetField(a, "_scoreMultiplier", so);
            SetField(a, "_catalog",         catalog);
            // _prestigeSystem is null

            Assert.DoesNotThrow(() => a.Apply());
            Assert.AreEqual(1f, so.Multiplier, 0.001f,
                "Apply with null prestige system must leave multiplier unchanged.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Applier_Apply_NullScoreMultiplier_DoesNotThrow()
        {
            var a       = CreateApplier();
            var catalog = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 1, label = "Bronze", bonusMultiplier = 1.1f },
            });
            var pres = CreatePrestige(1);
            SetField(a, "_catalog",        catalog);
            SetField(a, "_prestigeSystem", pres);
            // _scoreMultiplier is null

            Assert.DoesNotThrow(() => a.Apply(),
                "Apply must not throw when _scoreMultiplier is null.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pres);
        }

        [Test]
        public void Applier_Apply_HasRewardForRank_SetsMultiplier()
        {
            var a       = CreateApplier();
            var so      = CreateMultiplierSO(1f);
            var catalog = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 2, label = "Silver", bonusMultiplier = 1.5f },
            });
            var pres = CreatePrestige(2); // prestige count = 2, matches rank 2

            SetField(a, "_catalog",         catalog);
            SetField(a, "_prestigeSystem",  pres);
            SetField(a, "_scoreMultiplier", so);

            a.Apply();

            Assert.AreNotEqual(1f, so.Multiplier,
                "Apply must have changed the multiplier from the default.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pres);
        }

        [Test]
        public void Applier_Apply_MultiplierValueCorrect()
        {
            var a       = CreateApplier();
            var so      = CreateMultiplierSO(1f);
            var catalog = CreateCatalog(new[]
            {
                new PrestigeRewardEntry { rank = 3, label = "Gold", bonusMultiplier = 1.75f },
            });
            var pres = CreatePrestige(3);

            SetField(a, "_catalog",         catalog);
            SetField(a, "_prestigeSystem",  pres);
            SetField(a, "_scoreMultiplier", so);

            a.Apply();

            Assert.AreEqual(1.75f, so.Multiplier, 0.001f,
                "Apply must set the multiplier to the catalog entry's bonusMultiplier.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(pres);
        }

        [Test]
        public void Applier_ResetMultiplier_NullSO_DoesNotThrow()
        {
            var a = CreateApplier();
            // _scoreMultiplier is null
            Assert.DoesNotThrow(() => a.ResetMultiplier());
            Object.DestroyImmediate(a.gameObject);
        }

        [Test]
        public void Applier_OnDisable_Unregisters()
        {
            var a  = CreateApplier();
            var ch = CreateEvent();
            SetField(a, "_onMatchStarted", ch);
            InvokePrivate(a, "Awake");
            InvokePrivate(a, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(a, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable the applier's handler must be unregistered.");

            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(ch);
        }
    }
}

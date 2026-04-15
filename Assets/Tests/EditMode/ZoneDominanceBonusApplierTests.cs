using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T259: <see cref="ZoneDominanceBonusApplier"/>.
    ///
    /// ZoneDominanceBonusApplierTests (14):
    ///   FreshInstance_DominanceSO_Null                                     ×1
    ///   FreshInstance_ScoreMultiplier_Null                                 ×1
    ///   FreshInstance_BonusMultiplier_Default_OnePointFive                 ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters_BothChannels                                 ×1
    ///   Apply_NullDominanceSO_DoesNotThrow                                 ×1
    ///   Apply_NullScoreMultiplier_DoesNotThrow                             ×1
    ///   Apply_HasDominance_SetsMultiplier                                  ×1
    ///   Apply_NoDominance_DoesNotSetMultiplier                             ×1
    ///   ResetBonus_NullScoreMultiplier_DoesNotThrow                        ×1
    ///   ResetBonus_ResetsToDefault                                         ×1
    ///   OnMatchStarted_Event_CallsApply                                    ×1
    ///   OnMatchEnded_Event_CallsResetBonus                                 ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneDominanceBonusApplierTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneDominanceSO CreateDominanceSO() =>
            ScriptableObject.CreateInstance<ZoneDominanceSO>();

        private static ScoreMultiplierSO CreateMultiplierSO() =>
            ScriptableObject.CreateInstance<ScoreMultiplierSO>();

        private static ZoneDominanceBonusApplier CreateApplier() =>
            new GameObject("ZoneDomBonus_Test").AddComponent<ZoneDominanceBonusApplier>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_DominanceSO_Null()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.DominanceSO,
                "DominanceSO must be null on a fresh ZoneDominanceBonusApplier.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void FreshInstance_ScoreMultiplier_Null()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.ScoreMultiplier,
                "ScoreMultiplier must be null on a fresh ZoneDominanceBonusApplier.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void FreshInstance_BonusMultiplier_Default_OnePointFive()
        {
            var applier = CreateApplier();
            Assert.AreEqual(1.5f, applier.BonusMultiplier, 0.001f,
                "BonusMultiplier must default to 1.5.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(applier, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(applier, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_BothChannels()
        {
            var applier    = CreateApplier();
            var dominance  = CreateDominanceSO();
            var multiplier = CreateMultiplierSO();
            var start      = CreateEvent();
            var end        = CreateEvent();

            SetField(applier, "_dominanceSO",     dominance);
            SetField(applier, "_scoreMultiplier", multiplier);
            SetField(applier, "_onMatchStarted",  start);
            SetField(applier, "_onMatchEnded",    end);
            SetField(applier, "_bonusMultiplier", 2f);

            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");
            InvokePrivate(applier, "OnDisable");

            // Give dominance so Apply would normally set multiplier.
            dominance.AddPlayerZone();
            dominance.AddPlayerZone();
            Assert.IsTrue(dominance.HasDominance, "Pre-condition: player must have dominance.");

            // After disable, raising match-started must NOT apply the bonus.
            float before = multiplier.Multiplier;
            start.Raise();
            Assert.AreEqual(before, multiplier.Multiplier, 0.001f,
                "After OnDisable, _onMatchStarted must not apply dominance bonus.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(multiplier);
            Object.DestroyImmediate(start);
            Object.DestroyImmediate(end);
        }

        [Test]
        public void Apply_NullDominanceSO_DoesNotThrow()
        {
            var applier    = CreateApplier();
            var multiplier = CreateMultiplierSO();
            SetField(applier, "_scoreMultiplier", multiplier);
            InvokePrivate(applier, "Awake");

            Assert.DoesNotThrow(() => applier.Apply(),
                "Apply must not throw when DominanceSO is null.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(multiplier);
        }

        [Test]
        public void Apply_NullScoreMultiplier_DoesNotThrow()
        {
            var applier   = CreateApplier();
            var dominance = CreateDominanceSO();
            SetField(applier, "_dominanceSO", dominance);
            InvokePrivate(applier, "Awake");

            Assert.DoesNotThrow(() => applier.Apply(),
                "Apply must not throw when ScoreMultiplier is null.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(dominance);
        }

        [Test]
        public void Apply_HasDominance_SetsMultiplier()
        {
            var applier    = CreateApplier();
            var dominance  = CreateDominanceSO();
            var multiplier = CreateMultiplierSO();
            SetField(applier, "_dominanceSO",     dominance);
            SetField(applier, "_scoreMultiplier", multiplier);
            SetField(applier, "_bonusMultiplier", 2f);
            InvokePrivate(applier, "Awake");

            // Give player majority (2 of 3 zones).
            dominance.AddPlayerZone();
            dominance.AddPlayerZone();
            Assert.IsTrue(dominance.HasDominance, "Pre-condition: player must have dominance.");

            applier.Apply();

            Assert.AreEqual(2f, multiplier.Multiplier, 0.001f,
                "Apply must write BonusMultiplier to ScoreMultiplierSO when player has dominance.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(multiplier);
        }

        [Test]
        public void Apply_NoDominance_DoesNotSetMultiplier()
        {
            var applier    = CreateApplier();
            var dominance  = CreateDominanceSO();
            var multiplier = CreateMultiplierSO();
            SetField(applier, "_dominanceSO",     dominance);
            SetField(applier, "_scoreMultiplier", multiplier);
            SetField(applier, "_bonusMultiplier", 2f);
            InvokePrivate(applier, "Awake");

            // Player holds 0 zones — no dominance.
            Assert.IsFalse(dominance.HasDominance, "Pre-condition: player must not have dominance.");

            float before = multiplier.Multiplier;
            applier.Apply();

            Assert.AreEqual(before, multiplier.Multiplier, 0.001f,
                "Apply must not change multiplier when player has no dominance.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(multiplier);
        }

        [Test]
        public void ResetBonus_NullScoreMultiplier_DoesNotThrow()
        {
            var applier = CreateApplier();
            InvokePrivate(applier, "Awake");

            Assert.DoesNotThrow(() => applier.ResetBonus(),
                "ResetBonus must not throw when ScoreMultiplier is null.");

            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void ResetBonus_ResetsToDefault()
        {
            var applier    = CreateApplier();
            var dominance  = CreateDominanceSO();
            var multiplier = CreateMultiplierSO();
            SetField(applier, "_dominanceSO",     dominance);
            SetField(applier, "_scoreMultiplier", multiplier);
            SetField(applier, "_bonusMultiplier", 2f);
            InvokePrivate(applier, "Awake");

            dominance.AddPlayerZone();
            dominance.AddPlayerZone();
            applier.Apply();
            Assert.AreEqual(2f, multiplier.Multiplier, 0.001f,
                "Pre-condition: Apply must have set multiplier to 2.");

            applier.ResetBonus();

            Assert.AreEqual(multiplier.DefaultMultiplier, multiplier.Multiplier, 0.001f,
                "ResetBonus must restore ScoreMultiplierSO to its default multiplier.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(multiplier);
        }

        [Test]
        public void OnMatchStarted_Event_CallsApply()
        {
            var applier    = CreateApplier();
            var dominance  = CreateDominanceSO();
            var multiplier = CreateMultiplierSO();
            var start      = CreateEvent();
            SetField(applier, "_dominanceSO",     dominance);
            SetField(applier, "_scoreMultiplier", multiplier);
            SetField(applier, "_onMatchStarted",  start);
            SetField(applier, "_bonusMultiplier", 3f);

            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");

            dominance.AddPlayerZone();
            dominance.AddPlayerZone();
            start.Raise();

            Assert.AreEqual(3f, multiplier.Multiplier, 0.001f,
                "_onMatchStarted must trigger Apply and set the bonus multiplier.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(multiplier);
            Object.DestroyImmediate(start);
        }

        [Test]
        public void OnMatchEnded_Event_CallsResetBonus()
        {
            var applier    = CreateApplier();
            var dominance  = CreateDominanceSO();
            var multiplier = CreateMultiplierSO();
            var start      = CreateEvent();
            var end        = CreateEvent();
            SetField(applier, "_dominanceSO",     dominance);
            SetField(applier, "_scoreMultiplier", multiplier);
            SetField(applier, "_onMatchStarted",  start);
            SetField(applier, "_onMatchEnded",    end);
            SetField(applier, "_bonusMultiplier", 3f);

            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");

            // Apply bonus first.
            dominance.AddPlayerZone();
            dominance.AddPlayerZone();
            start.Raise();
            Assert.AreEqual(3f, multiplier.Multiplier, 0.001f,
                "Pre-condition: Apply must have set multiplier to 3.");

            // End match — must reset.
            end.Raise();
            Assert.AreEqual(multiplier.DefaultMultiplier, multiplier.Multiplier, 0.001f,
                "_onMatchEnded must trigger ResetBonus and restore the default multiplier.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(dominance);
            Object.DestroyImmediate(multiplier);
            Object.DestroyImmediate(start);
            Object.DestroyImmediate(end);
        }
    }
}

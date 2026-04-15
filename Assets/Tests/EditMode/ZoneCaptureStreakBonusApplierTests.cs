using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T275: <see cref="ZoneCaptureStreakBonusApplier"/>.
    ///
    /// Tests (14):
    ///   FreshInstance_StreakSO_Null                                        ×1
    ///   FreshInstance_ScoreMultiplier_Null                                 ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters_BothChannels                                 ×1
    ///   ApplyBonus_NullStreakSO_NoThrow                                    ×1
    ///   ApplyBonus_NullScoreMultiplier_NoThrow                             ×1
    ///   ApplyBonus_HasBonus_SetsMultiplier                                 ×1
    ///   ApplyBonus_NoBonus_ResetsToDefault                                 ×1
    ///   ApplyBonus_BothNull_NoThrow                                        ×1
    ///   ResetBonus_NullScoreMultiplier_NoThrow                             ×1
    ///   ResetBonus_ResetsScoreMultiplier                                   ×1
    ///   OnStreakChanged_Raise_AppliesBonus                                 ×1
    ///   OnMatchEnded_Raise_ResetsBonus                                     ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneCaptureStreakBonusApplierTests
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

        private static ZoneCaptureStreakBonusApplier CreateApplier() =>
            new GameObject("StreakBonusApplier_Test")
                .AddComponent<ZoneCaptureStreakBonusApplier>();

        private static ZoneCaptureStreakSO CreateStreakSO() =>
            ScriptableObject.CreateInstance<ZoneCaptureStreakSO>();

        private static ScoreMultiplierSO CreateMultiplierSO() =>
            ScriptableObject.CreateInstance<ScoreMultiplierSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_StreakSO_Null()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.StreakSO,
                "StreakSO must be null on a fresh ZoneCaptureStreakBonusApplier.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void FreshInstance_ScoreMultiplier_Null()
        {
            var applier = CreateApplier();
            Assert.IsNull(applier.ScoreMultiplier,
                "ScoreMultiplier must be null on a fresh ZoneCaptureStreakBonusApplier.");
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
            var applier      = CreateApplier();
            var streakSO     = CreateStreakSO();
            var multiplierSO = CreateMultiplierSO();
            var evtStreak    = CreateEvent();
            var evtMatch     = CreateEvent();

            SetField(applier, "_streakSO",        streakSO);
            SetField(applier, "_scoreMultiplier",  multiplierSO);
            SetField(applier, "_onStreakChanged",  evtStreak);
            SetField(applier, "_onMatchEnded",     evtMatch);

            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");
            InvokePrivate(applier, "OnDisable");

            // Set multiplier manually then raise events — must not be changed.
            multiplierSO.SetMultiplier(5f);
            evtStreak.Raise(); // must not call ApplyBonus
            evtMatch.Raise();  // must not call ResetBonus
            Assert.AreEqual(5f, multiplierSO.Multiplier, 0.001f,
                "After OnDisable, raising events must not modify the score multiplier.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(multiplierSO);
            Object.DestroyImmediate(evtStreak);
            Object.DestroyImmediate(evtMatch);
        }

        [Test]
        public void ApplyBonus_NullStreakSO_NoThrow()
        {
            var applier      = CreateApplier();
            var multiplierSO = CreateMultiplierSO();
            SetField(applier, "_scoreMultiplier", multiplierSO);

            Assert.DoesNotThrow(() => applier.ApplyBonus(),
                "ApplyBonus with null StreakSO must not throw.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(multiplierSO);
        }

        [Test]
        public void ApplyBonus_NullScoreMultiplier_NoThrow()
        {
            var applier  = CreateApplier();
            var streakSO = CreateStreakSO();
            SetField(applier, "_streakSO", streakSO);

            Assert.DoesNotThrow(() => applier.ApplyBonus(),
                "ApplyBonus with null ScoreMultiplier must not throw.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(streakSO);
        }

        [Test]
        public void ApplyBonus_HasBonus_SetsMultiplier()
        {
            var applier      = CreateApplier();
            var streakSO     = CreateStreakSO();
            var multiplierSO = CreateMultiplierSO();

            SetField(applier, "_streakSO",       streakSO);
            SetField(applier, "_scoreMultiplier", multiplierSO);

            // Bring streak to threshold (default 3) so HasBonus == true.
            streakSO.IncrementStreak();
            streakSO.IncrementStreak();
            streakSO.IncrementStreak();
            Assert.IsTrue(streakSO.HasBonus, "Pre-condition: HasBonus must be true.");

            applier.ApplyBonus();

            Assert.AreEqual(streakSO.BonusMultiplier, multiplierSO.Multiplier, 0.001f,
                "ApplyBonus must write BonusMultiplier to ScoreMultiplierSO when HasBonus.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(multiplierSO);
        }

        [Test]
        public void ApplyBonus_NoBonus_ResetsToDefault()
        {
            var applier      = CreateApplier();
            var streakSO     = CreateStreakSO();
            var multiplierSO = CreateMultiplierSO();

            SetField(applier, "_streakSO",       streakSO);
            SetField(applier, "_scoreMultiplier", multiplierSO);

            // Set a custom multiplier, then verify ApplyBonus resets it when streak < threshold.
            multiplierSO.SetMultiplier(3f);
            Assert.IsFalse(streakSO.HasBonus, "Pre-condition: HasBonus must be false.");

            applier.ApplyBonus();

            Assert.AreEqual(multiplierSO.DefaultMultiplier, multiplierSO.Multiplier, 0.001f,
                "ApplyBonus must reset the multiplier to default when HasBonus is false.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(multiplierSO);
        }

        [Test]
        public void ApplyBonus_BothNull_NoThrow()
        {
            var applier = CreateApplier();
            Assert.DoesNotThrow(() => applier.ApplyBonus(),
                "ApplyBonus with both refs null must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void ResetBonus_NullScoreMultiplier_NoThrow()
        {
            var applier = CreateApplier();
            Assert.DoesNotThrow(() => applier.ResetBonus(),
                "ResetBonus with null ScoreMultiplier must not throw.");
            Object.DestroyImmediate(applier.gameObject);
        }

        [Test]
        public void ResetBonus_ResetsScoreMultiplier()
        {
            var applier      = CreateApplier();
            var multiplierSO = CreateMultiplierSO();
            SetField(applier, "_scoreMultiplier", multiplierSO);

            multiplierSO.SetMultiplier(4f);
            Assert.AreEqual(4f, multiplierSO.Multiplier, 0.001f,
                "Pre-condition: multiplier should be 4.");

            applier.ResetBonus();

            Assert.AreEqual(multiplierSO.DefaultMultiplier, multiplierSO.Multiplier, 0.001f,
                "ResetBonus must restore the ScoreMultiplierSO to its default.");
            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(multiplierSO);
        }

        [Test]
        public void OnStreakChanged_Raise_AppliesBonus()
        {
            var applier      = CreateApplier();
            var streakSO     = CreateStreakSO();
            var multiplierSO = CreateMultiplierSO();
            var evtStreak    = CreateEvent();

            SetField(applier, "_streakSO",       streakSO);
            SetField(applier, "_scoreMultiplier", multiplierSO);
            SetField(applier, "_onStreakChanged", evtStreak);

            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");

            // Drive streak to bonus threshold, then raise the event.
            streakSO.IncrementStreak();
            streakSO.IncrementStreak();
            streakSO.IncrementStreak();
            evtStreak.Raise();

            Assert.AreEqual(streakSO.BonusMultiplier, multiplierSO.Multiplier, 0.001f,
                "Raising _onStreakChanged must invoke ApplyBonus and set the score multiplier.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(streakSO);
            Object.DestroyImmediate(multiplierSO);
            Object.DestroyImmediate(evtStreak);
        }

        [Test]
        public void OnMatchEnded_Raise_ResetsBonus()
        {
            var applier      = CreateApplier();
            var multiplierSO = CreateMultiplierSO();
            var evtMatch     = CreateEvent();

            SetField(applier, "_scoreMultiplier", multiplierSO);
            SetField(applier, "_onMatchEnded",    evtMatch);

            InvokePrivate(applier, "Awake");
            InvokePrivate(applier, "OnEnable");

            multiplierSO.SetMultiplier(6f);
            evtMatch.Raise();

            Assert.AreEqual(multiplierSO.DefaultMultiplier, multiplierSO.Multiplier, 0.001f,
                "Raising _onMatchEnded must invoke ResetBonus and restore the default multiplier.");

            Object.DestroyImmediate(applier.gameObject);
            Object.DestroyImmediate(multiplierSO);
            Object.DestroyImmediate(evtMatch);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="AchievementRowController"/>.
    ///
    /// Because the display formatting lives in the private static helper
    /// <c>FormatProgress</c>, it is invoked here via reflection so the
    /// formatting contracts can be tested without wiring up uGUI Text components.
    ///
    /// Covers:
    ///   • <c>FormatProgress</c> (private static) — reflection-invoked;
    ///     verifies "current / target" formatting across boundary cases.
    ///   • <see cref="AchievementRowController.Setup"/> null-safety guard —
    ///     must not throw when called with a null definition or when all optional
    ///     UI refs are absent (freshly added MB).
    ///
    /// All tests run headless (no scene, no uGUI dependencies).
    /// </summary>
    public class AchievementRowControllerTests
    {
        // ── Reflection — FormatProgress ───────────────────────────────────────

        private static readonly MethodInfo _formatProgress =
            typeof(AchievementRowController).GetMethod(
                "FormatProgress",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(int), typeof(int) },
                null);

        // ── Invocation helper ─────────────────────────────────────────────────

        private static string Progress(int current, int target)
            => (string)_formatProgress.Invoke(null, new object[] { current, target });

        // ── Helper ────────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static AchievementDefinitionSO MakeDef(
            string id          = "test_id",
            string displayName = "Test Achievement",
            int    targetCount = 5,
            int    reward      = 0)
        {
            var def = ScriptableObject.CreateInstance<AchievementDefinitionSO>();
            SetField(def, "_id",           id);
            SetField(def, "_displayName",  displayName);
            SetField(def, "_description",  "A test achievement.");
            SetField(def, "_targetCount",  targetCount);
            SetField(def, "_rewardCredits", reward);
            return def;
        }

        // ── Reflection sanity ─────────────────────────────────────────────────

        [Test]
        public void ReflectionSanity_FormatProgress_Found()
        {
            Assert.IsNotNull(_formatProgress,
                "Private static method 'FormatProgress(int, int)' not found on " +
                "AchievementRowController — has the method been renamed or removed?");
        }

        // ── FormatProgress — boundary cases ───────────────────────────────────

        [Test]
        public void FormatProgress_Zero_ShowsZeroOverTarget()
        {
            Assert.AreEqual("0 / 5", Progress(0, 5),
                "Zero progress should display as '0 / 5'.");
        }

        [Test]
        public void FormatProgress_Midway_ShowsBothValues()
        {
            Assert.AreEqual("3 / 5", Progress(3, 5),
                "Mid-way progress should display as '3 / 5'.");
        }

        [Test]
        public void FormatProgress_ExactTarget_ShowsEqual()
        {
            Assert.AreEqual("5 / 5", Progress(5, 5),
                "Exactly meeting the target should display as '5 / 5'.");
        }

        [Test]
        public void FormatProgress_AboveTarget_ShowsCurrent()
        {
            // Shouldn't normally happen (achievement would be unlocked), but the
            // method must not throw or produce unexpected output.
            Assert.AreEqual("7 / 5", Progress(7, 5));
        }

        [Test]
        public void FormatProgress_TargetOne_ShowsSingleSlot()
        {
            Assert.AreEqual("0 / 1", Progress(0, 1));
        }

        // ── Setup — null-safety ────────────────────────────────────────────────

        [Test]
        public void Setup_NullDef_DoesNotThrow()
        {
            var go   = new GameObject("TestAchievementRow");
            var ctrl = go.AddComponent<AchievementRowController>();

            Assert.DoesNotThrow(() => ctrl.Setup(null, false, 0),
                "Setup(null, …) must return early without throwing.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Setup_UnlockedDef_AllUIRefsNull_DoesNotThrow()
        {
            var go   = new GameObject("TestAchievementRow");
            var ctrl = go.AddComponent<AchievementRowController>();
            var def  = MakeDef(targetCount: 1, reward: 100);

            Assert.DoesNotThrow(() => ctrl.Setup(def, isUnlocked: true, currentProgress: 1),
                "Setup with isUnlocked=true must not throw when all Text/badge refs are null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void Setup_NotUnlockedDef_AllUIRefsNull_DoesNotThrow()
        {
            var go   = new GameObject("TestAchievementRow");
            var ctrl = go.AddComponent<AchievementRowController>();
            var def  = MakeDef(targetCount: 5, reward: 0);

            Assert.DoesNotThrow(() => ctrl.Setup(def, isUnlocked: false, currentProgress: 2),
                "Setup with isUnlocked=false must not throw when all Text/badge refs are null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void Setup_CalledTwice_DoesNotThrow()
        {
            var go   = new GameObject("TestAchievementRow");
            var ctrl = go.AddComponent<AchievementRowController>();
            var defA = MakeDef("id_a", targetCount: 3);
            var defB = MakeDef("id_b", targetCount: 10, reward: 50);

            Assert.DoesNotThrow(() =>
            {
                ctrl.Setup(defA, false, 1);
                ctrl.Setup(defB, true,  10);
            }, "Calling Setup twice in succession must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(defA);
            Object.DestroyImmediate(defB);
        }
    }
}

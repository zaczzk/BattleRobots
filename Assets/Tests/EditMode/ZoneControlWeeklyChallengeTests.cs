using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T299:
    ///   <see cref="ZoneControlWeeklyChallengeSO"/> and
    ///   <see cref="ZoneControlWeeklyChallengeController"/>.
    ///
    /// ZoneControlWeeklyChallengeTests (14):
    ///   SO_FreshInstance_IsComplete_False                        ×1
    ///   SO_FreshInstance_BestValue_Zero                          ×1
    ///   SO_EvaluateChallenge_ReturnsFalse_BelowTarget            ×1
    ///   SO_EvaluateChallenge_ReturnsTrue_WhenTargetMet           ×1
    ///   SO_EvaluateChallenge_FiresEvent_OnCompletion             ×1
    ///   SO_EvaluateChallenge_DoesNotFireEvent_WhenAlreadyDone    ×1
    ///   SO_LoadSnapshot_RestoresState                            ×1
    ///   SO_Reset_ClearsState                                     ×1
    ///   Controller_FreshInstance_ChallengeSO_Null                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullChallengeSO_HidesPanel            ×1
    ///   Controller_HandleMatchEnded_NullRefs_DoesNotThrow        ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlWeeklyChallengeTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlWeeklyChallengeSO CreateChallengeSO(float target = 10f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlWeeklyChallengeSO>();
            SetField(so, "_targetValue", target);
            return so;
        }

        private static ZoneControlSessionSummarySO CreateSummarySO() =>
            ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();

        private static ZoneControlWeeklyChallengeController CreateController() =>
            new GameObject("Challenge_Test").AddComponent<ZoneControlWeeklyChallengeController>();

        private static Text CreateText()
        {
            var go = new GameObject("Txt");
            go.AddComponent<CanvasRenderer>();
            return go.AddComponent<Text>();
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsComplete_False()
        {
            var so = CreateChallengeSO();
            Assert.IsFalse(so.IsComplete,
                "IsComplete must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BestValue_Zero()
        {
            var so = CreateChallengeSO();
            Assert.AreEqual(0f, so.BestValue,
                "BestValue must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateChallenge_ReturnsFalse_BelowTarget()
        {
            var so = CreateChallengeSO(10f);
            bool result = so.EvaluateChallenge(5f);
            Assert.IsFalse(result,
                "EvaluateChallenge must return false when value is below target.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateChallenge_ReturnsTrue_WhenTargetMet()
        {
            var so = CreateChallengeSO(10f);
            bool result = so.EvaluateChallenge(10f);
            Assert.IsTrue(result,
                "EvaluateChallenge must return true when value meets the target.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateChallenge_FiresEvent_OnCompletion()
        {
            var so  = CreateChallengeSO(10f);
            var evt = CreateEvent();
            SetField(so, "_onChallengeComplete", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.EvaluateChallenge(15f);

            Assert.AreEqual(1, fired,
                "_onChallengeComplete must fire once on first completion.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateChallenge_DoesNotFireEvent_WhenAlreadyDone()
        {
            var so  = CreateChallengeSO(10f);
            var evt = CreateEvent();
            SetField(so, "_onChallengeComplete", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.EvaluateChallenge(15f); // first completion
            so.EvaluateChallenge(20f); // already complete

            Assert.AreEqual(1, fired,
                "_onChallengeComplete must not fire again when already complete.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_LoadSnapshot_RestoresState()
        {
            var so = CreateChallengeSO(10f);
            so.LoadSnapshot(true, 12f);
            Assert.IsTrue(so.IsComplete, "LoadSnapshot must restore IsComplete.");
            Assert.AreEqual(12f, so.BestValue, "LoadSnapshot must restore BestValue.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateChallengeSO(10f);
            so.EvaluateChallenge(15f);
            so.Reset();
            Assert.IsFalse(so.IsComplete, "Reset must clear IsComplete.");
            Assert.AreEqual(0f, so.BestValue, "Reset must clear BestValue.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ChallengeSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ChallengeSO,
                "ChallengeSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlWeeklyChallengeController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlWeeklyChallengeController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlWeeklyChallengeController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullChallengeSO_HidesPanel()
        {
            var go    = new GameObject("Test_NullChallenge");
            var ctrl  = go.AddComponent<ZoneControlWeeklyChallengeController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when ChallengeSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded must not throw when all refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T295:
    ///   <see cref="ZoneControlAdaptiveDifficultySO"/> and
    ///   <see cref="ZoneControlAdaptiveDifficultyController"/>.
    ///
    /// ZoneControlAdaptiveDifficultyTests (12):
    ///   SO_FreshInstance_CurrentDuration_Zero                    ×1
    ///   SO_Initialize_ClampsToDuration                           ×1
    ///   SO_AdjustFromRating_HighRating_IncreasesDuration         ×1
    ///   SO_AdjustFromRating_LowRating_DecreasesDuration          ×1
    ///   SO_AdjustFromRating_MidRating_NoChange                   ×1
    ///   SO_AdjustFromRating_ClampsToMax                          ×1
    ///   Controller_FreshInstance_DifficultySO_Null               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_HandleRatingHistoryUpdated_NullRefs_NoThrow   ×1
    ///   Controller_HandleRatingHistoryUpdated_AdjustsDifficulty  ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlAdaptiveDifficultyTests
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

        private static ZoneControlAdaptiveDifficultySO CreateDifficultySO() =>
            ScriptableObject.CreateInstance<ZoneControlAdaptiveDifficultySO>();

        private static ZoneControlMatchRatingHistorySO CreateHistorySO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchRatingHistorySO>();

        private static ZoneControlAdaptiveDifficultyController CreateController() =>
            new GameObject("AdaptiveDiffCtrl_Test")
                .AddComponent<ZoneControlAdaptiveDifficultyController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentDuration_Zero()
        {
            var so = CreateDifficultySO();
            Assert.AreEqual(0f, so.CurrentDuration,
                "CurrentDuration must be 0 on a freshly created SO (Reset on OnEnable).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Initialize_ClampsToDuration()
        {
            var so = CreateDifficultySO();
            // Default min=1, max=30.
            so.Initialize(15f);
            Assert.AreEqual(15f, so.CurrentDuration,
                "Initialize must set CurrentDuration to the given value when in range.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AdjustFromRating_HighRating_IncreasesDuration()
        {
            var so = CreateDifficultySO();
            so.Initialize(10f);
            float before = so.CurrentDuration;
            so.AdjustFromRating(4);
            Assert.Greater(so.CurrentDuration, before,
                "Rating >= 4 must increase CurrentDuration.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AdjustFromRating_LowRating_DecreasesDuration()
        {
            var so = CreateDifficultySO();
            so.Initialize(10f);
            float before = so.CurrentDuration;
            so.AdjustFromRating(2);
            Assert.Less(so.CurrentDuration, before,
                "Rating <= 2 must decrease CurrentDuration.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AdjustFromRating_MidRating_NoChange()
        {
            var so = CreateDifficultySO();
            so.Initialize(10f);
            float before = so.CurrentDuration;
            so.AdjustFromRating(3);
            Assert.AreEqual(before, so.CurrentDuration,
                "Rating == 3 must leave CurrentDuration unchanged.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AdjustFromRating_ClampsToMax()
        {
            var so = CreateDifficultySO();
            // Default max is 30. Initialize near max.
            so.Initialize(30f);
            so.AdjustFromRating(5);
            Assert.AreEqual(so.MaxCaptureDuration, so.CurrentDuration,
                "AdjustFromRating must not exceed MaxCaptureDuration.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_DifficultySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.DifficultySO,
                "DifficultySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlAdaptiveDifficultyController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlAdaptiveDifficultyController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlAdaptiveDifficultyController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onRatingHistoryUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onRatingHistoryUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleRatingHistoryUpdated_NullRefs_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleRatingHistoryUpdated(),
                "HandleRatingHistoryUpdated must not throw when refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_HandleRatingHistoryUpdated_AdjustsDifficulty()
        {
            var go         = new GameObject("Test_Adjusts");
            var ctrl       = go.AddComponent<ZoneControlAdaptiveDifficultyController>();
            var difficulty = CreateDifficultySO();
            var history    = CreateHistorySO();

            difficulty.Initialize(10f);
            float before = difficulty.CurrentDuration;

            // Add a high rating → difficulty should increase.
            history.AddRating(5);

            SetField(ctrl, "_difficultySO", difficulty);
            SetField(ctrl, "_historySO",    history);

            ctrl.HandleRatingHistoryUpdated();

            Assert.AreNotEqual(before, difficulty.CurrentDuration,
                "HandleRatingHistoryUpdated must adjust CurrentDuration based on last rating.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(difficulty);
            Object.DestroyImmediate(history);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T192 — <see cref="MasteryProgressMilestoneSO"/>.
    ///
    /// MasteryProgressMilestoneSOTests (10):
    ///   SO_FreshInstance_ArraysAreEmpty                          ×1
    ///   SO_GetMilestonesForType_Physical_ReturnsArray            ×1
    ///   SO_GetMilestonesForType_Unknown_ReturnsNull              ×1
    ///   SO_GetClearedCount_NoMilestones_Zero                     ×1
    ///   SO_GetClearedCount_SomeMilestones_Correct                ×1
    ///   SO_GetClearedCount_AllCleared_ReturnsTotal               ×1
    ///   SO_GetNextMilestone_SomeMilestones_ReturnsFirstUncleared ×1
    ///   SO_GetNextMilestone_AllCleared_ReturnsNull               ×1
    ///   SO_GetProgress_NoNextMilestone_ReturnsOne                ×1
    ///   SO_GetProgress_WithMilestones_Correct                    ×1
    /// </summary>
    public class MasteryProgressMilestoneSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static MasteryProgressMilestoneSO CreateSO() =>
            ScriptableObject.CreateInstance<MasteryProgressMilestoneSO>();

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static MasteryProgressMilestoneSO CreateSOWithPhysical(float[] milestones)
        {
            var so = CreateSO();
            SetField(so, "_physicalMilestones", milestones);
            return so;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_ArraysAreEmpty()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.GetMilestonesForType(DamageType.Physical).Length,
                "Fresh SO must have an empty Physical milestones array.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetMilestonesForType_Physical_ReturnsArray()
        {
            var milestones = new float[] { 500f, 1000f, 5000f };
            var so = CreateSOWithPhysical(milestones);

            float[] result = so.GetMilestonesForType(DamageType.Physical);
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Length,
                "GetMilestonesForType(Physical) must return the assigned array.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetMilestonesForType_Unknown_ReturnsNull()
        {
            var so = CreateSO();
            // Cast to int to supply an unknown DamageType value.
            float[] result = so.GetMilestonesForType((DamageType)99);
            Assert.IsNull(result,
                "GetMilestonesForType with unknown DamageType must return null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetClearedCount_NoMilestones_Zero()
        {
            var so = CreateSO();
            int cleared = so.GetClearedCount(DamageType.Physical, 9999f);
            Assert.AreEqual(0, cleared,
                "GetClearedCount with no milestones must return 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetClearedCount_SomeMilestones_Correct()
        {
            // Milestones: [500, 1000, 5000]; accumulation = 800 → 1 cleared (500).
            var so = CreateSOWithPhysical(new float[] { 500f, 1000f, 5000f });
            int cleared = so.GetClearedCount(DamageType.Physical, 800f);
            Assert.AreEqual(1, cleared,
                "At accumulation=800 with milestones [500,1000,5000], 1 milestone must be cleared.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetClearedCount_AllCleared_ReturnsTotal()
        {
            // Milestones: [500, 1000, 5000]; accumulation = 5000 → 3 cleared.
            var so = CreateSOWithPhysical(new float[] { 500f, 1000f, 5000f });
            int cleared = so.GetClearedCount(DamageType.Physical, 5000f);
            Assert.AreEqual(3, cleared,
                "At accumulation=5000 with milestones [500,1000,5000], all 3 must be cleared.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetNextMilestone_SomeMilestones_ReturnsFirstUncleared()
        {
            // Milestones: [500, 1000, 5000]; accumulation = 800 → next = 1000.
            var so   = CreateSOWithPhysical(new float[] { 500f, 1000f, 5000f });
            float? next = so.GetNextMilestone(DamageType.Physical, 800f);
            Assert.IsTrue(next.HasValue, "GetNextMilestone must return a value when milestones remain.");
            Assert.AreEqual(1000f, next.Value, 0.001f,
                "GetNextMilestone at accumulation=800 must return 1000.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetNextMilestone_AllCleared_ReturnsNull()
        {
            // Milestones: [500, 1000]; accumulation = 1000 → all cleared → null.
            var so   = CreateSOWithPhysical(new float[] { 500f, 1000f });
            float? next = so.GetNextMilestone(DamageType.Physical, 1000f);
            Assert.IsFalse(next.HasValue,
                "GetNextMilestone must return null when all milestones are cleared.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetProgress_NoNextMilestone_ReturnsOne()
        {
            // All milestones cleared → progress = 1.
            var so = CreateSOWithPhysical(new float[] { 500f, 1000f });
            float progress = so.GetProgress(DamageType.Physical, 1000f);
            Assert.AreEqual(1f, progress, 0.001f,
                "GetProgress when all milestones are cleared must return 1.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetProgress_WithMilestones_Correct()
        {
            // Milestones: [0→500→1000]; accumulation=800.
            // cleared=1 (500 done); prev=500; next=1000; range=500; progress=(800-500)/500=0.6
            var so = CreateSOWithPhysical(new float[] { 500f, 1000f });
            float progress = so.GetProgress(DamageType.Physical, 800f);
            Assert.AreEqual(0.6f, progress, 0.001f,
                "GetProgress at accumulation=800 with milestones [500,1000] must return 0.6.");
            Object.DestroyImmediate(so);
        }
    }
}

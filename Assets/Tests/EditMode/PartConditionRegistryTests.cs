using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartConditionRegistry"/>.
    ///
    /// Covers:
    ///   • Fresh instance: Entries not null, empty.
    ///   • GetCondition: null/empty id → null; unknown id → null; known id → correct SO.
    ///   • GetDamagedParts: all healthy → empty; one damaged → included; destroyed → included;
    ///     null condition entry → skipped.
    ///   • TakeSnapshot: empty registry → empty list; null condition → skipped;
    ///     full-health part → hpRatio 1; damaged part → correct ratio.
    ///   • LoadSnapshot: null input → no throw; valid snapshot → HP restored;
    ///     unknown partId → silently skipped; null entry → silently skipped.
    /// </summary>
    public class PartConditionRegistryTests
    {
        private PartConditionRegistry _registry;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetEntries(PartConditionRegistry registry,
                                       List<PartConditionRegistry.PartConditionEntry> entries)
        {
            SetField(registry, "_entries", entries);
        }

        private static PartConditionSO MakeCondition(float maxHP = 50f)
        {
            var so = ScriptableObject.CreateInstance<PartConditionSO>();
            SetField(so, "_maxHP", maxHP);
            // OnEnable already set CurrentHP = maxHP; re-invoke via LoadSnapshot(1f).
            so.LoadSnapshot(1f);
            return so;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<PartConditionRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_registry);
            _registry = null;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Entries_NotNull()
        {
            Assert.IsNotNull(_registry.Entries);
        }

        [Test]
        public void FreshInstance_Entries_IsEmpty()
        {
            Assert.AreEqual(0, _registry.Entries.Count);
        }

        // ── GetCondition ──────────────────────────────────────────────────────

        [Test]
        public void GetCondition_NullId_ReturnsNull()
        {
            Assert.IsNull(_registry.GetCondition(null));
        }

        [Test]
        public void GetCondition_EmptyId_ReturnsNull()
        {
            Assert.IsNull(_registry.GetCondition(""));
        }

        [Test]
        public void GetCondition_UnknownId_ReturnsNull()
        {
            Assert.IsNull(_registry.GetCondition("no_such_part"));
        }

        [Test]
        public void GetCondition_KnownId_ReturnsCorrectCondition()
        {
            var cond = MakeCondition();
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "arm_01", condition = cond },
            });

            var result = _registry.GetCondition("arm_01");

            Assert.AreSame(cond, result);
            Object.DestroyImmediate(cond);
        }

        // ── GetDamagedParts ───────────────────────────────────────────────────

        [Test]
        public void GetDamagedParts_AllHealthy_ReturnsEmpty()
        {
            var cond = MakeCondition();
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "arm_01", condition = cond },
            });

            var damaged = _registry.GetDamagedParts();

            Assert.AreEqual(0, damaged.Count);
            Object.DestroyImmediate(cond);
        }

        [Test]
        public void GetDamagedParts_OneDamagedPart_IncludedInResult()
        {
            var cond = MakeCondition();
            cond.TakeDamage(10f); // HPRatio = 40/50 = 0.8
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "arm_01", condition = cond },
            });

            var damaged = _registry.GetDamagedParts();

            Assert.AreEqual(1, damaged.Count);
            Assert.AreEqual("arm_01", damaged[0].partId);
            Object.DestroyImmediate(cond);
        }

        [Test]
        public void GetDamagedParts_DestroyedPart_IncludedInResult()
        {
            var cond = MakeCondition();
            cond.TakeDamage(cond.MaxHP); // destroy it — HPRatio = 0
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "leg_01", condition = cond },
            });

            var damaged = _registry.GetDamagedParts();

            Assert.AreEqual(1, damaged.Count);
            Object.DestroyImmediate(cond);
        }

        [Test]
        public void GetDamagedParts_NullConditionEntry_Skipped()
        {
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "arm_01", condition = null },
            });

            var damaged = _registry.GetDamagedParts();

            Assert.AreEqual(0, damaged.Count);
        }

        // ── TakeSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_EmptyRegistry_ReturnsEmptyList()
        {
            var snap = _registry.TakeSnapshot();
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.Count);
        }

        [Test]
        public void TakeSnapshot_NullCondition_EntrySkipped()
        {
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "arm_01", condition = null },
            });

            var snap = _registry.TakeSnapshot();

            Assert.AreEqual(0, snap.Count);
        }

        [Test]
        public void TakeSnapshot_FullHealthPart_HPRatioIsOne()
        {
            var cond = MakeCondition();
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "arm_01", condition = cond },
            });

            var snap = _registry.TakeSnapshot();

            Assert.AreEqual(1, snap.Count);
            Assert.AreEqual("arm_01", snap[0].partId);
            Assert.AreEqual(1f, snap[0].hpRatio, 0.001f);
            Object.DestroyImmediate(cond);
        }

        [Test]
        public void TakeSnapshot_DamagedPart_HPRatioMatchesActual()
        {
            var cond = MakeCondition(100f);
            cond.TakeDamage(25f); // CurrentHP = 75, HPRatio = 0.75
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "weapon_01", condition = cond },
            });

            var snap = _registry.TakeSnapshot();

            Assert.AreEqual(1, snap.Count);
            Assert.AreEqual(0.75f, snap[0].hpRatio, 0.001f);
            Object.DestroyImmediate(cond);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_NullInput_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _registry.LoadSnapshot(null));
        }

        [Test]
        public void LoadSnapshot_ValidSnapshot_RestoresHPRatio()
        {
            var cond = MakeCondition(100f);
            SetEntries(_registry, new List<PartConditionRegistry.PartConditionEntry>
            {
                new PartConditionRegistry.PartConditionEntry { partId = "arm_01", condition = cond },
            });

            _registry.LoadSnapshot(new List<PartConditionSnapshot>
            {
                new PartConditionSnapshot { partId = "arm_01", hpRatio = 0.6f },
            });

            Assert.AreEqual(60f, cond.CurrentHP, 0.01f);
            Object.DestroyImmediate(cond);
        }

        [Test]
        public void LoadSnapshot_UnknownPartId_SkippedSilently()
        {
            Assert.DoesNotThrow(() =>
                _registry.LoadSnapshot(new List<PartConditionSnapshot>
                {
                    new PartConditionSnapshot { partId = "no_such_part", hpRatio = 0.5f },
                }));
        }

        [Test]
        public void LoadSnapshot_NullEntry_SkippedSilently()
        {
            Assert.DoesNotThrow(() =>
                _registry.LoadSnapshot(new List<PartConditionSnapshot> { null }));
        }
    }
}

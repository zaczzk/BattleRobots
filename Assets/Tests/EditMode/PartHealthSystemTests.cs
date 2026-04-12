using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartHealthSystem"/>.
    ///
    /// Covers:
    ///   • Fresh instance with parts — GetLivingPartCount matches part count.
    ///   • DistributeDamage reduces a living part's HP.
    ///   • DistributeDamage ignores destroyed parts (only living parts receive damage).
    ///   • DistributeDamage zero amount — no-op.
    ///   • DistributeDamage with no registered parts — no-throw.
    ///   • AreAllPartsDestroyed false when parts list is empty.
    ///   • AreAllPartsDestroyed true when all parts destroyed.
    ///   • AreAllPartsDestroyed false when at least one part is living.
    ///   • DistributeDamage fires _onAllPartsDestroyed once when last part falls.
    ///   • DistributeDamage _onAllPartsDestroyed fires at most once (guarded).
    ///   • GetLivingPartCount decrements correctly as parts are destroyed.
    ///   • Reset restores all parts to full HP.
    ///   • Reset clears _allDestroyedFired so event can re-fire after reset.
    /// </summary>
    public class PartHealthSystemTests
    {
        private GameObject      _go;
        private PartHealthSystem _sys;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>Creates N PartConditionSO instances and wires them as _parts.</summary>
        private PartConditionSO[] AttachParts(int count, float maxHPEach = 50f)
        {
            var conditions = new PartConditionSO[count];
            var entries    = new PartHealthSystem.PartEntry[count];

            for (int i = 0; i < count; i++)
            {
                var so = ScriptableObject.CreateInstance<PartConditionSO>();
                SetField(so, "_maxHP", maxHPEach);
                // Manually invoke OnEnable equivalent: reset to max HP.
                so.ResetToMax();
                conditions[i]   = so;
                entries[i]      = new PartHealthSystem.PartEntry
                {
                    partId    = $"part_{i}",
                    condition = so
                };
            }

            SetField(_sys, "_parts", entries);
            return conditions;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go  = new GameObject("Robot");
            _sys = _go.AddComponent<PartHealthSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy all PartConditionSO instances attached to the system.
            var parts = _sys.Parts;
            if (parts != null)
                for (int i = 0; i < parts.Count; i++)
                    if (parts[i].condition != null)
                        Object.DestroyImmediate(parts[i].condition);

            Object.DestroyImmediate(_go);
            _sys = null;
        }

        // ── GetLivingPartCount ────────────────────────────────────────────────

        [Test]
        public void FreshWithParts_LivingPartCountMatchesPartCount()
        {
            AttachParts(3);

            Assert.AreEqual(3, _sys.GetLivingPartCount());
        }

        [Test]
        public void GetLivingPartCount_NoParts_ReturnsZero()
        {
            // _parts is null by default.
            Assert.AreEqual(0, _sys.GetLivingPartCount());
        }

        // ── DistributeDamage ──────────────────────────────────────────────────

        [Test]
        public void DistributeDamage_ReducesALivingPartHP()
        {
            var conds = AttachParts(1, maxHPEach: 100f);

            _sys.DistributeDamage(30f);

            // Single-part system: damage goes to the only part.
            Assert.AreEqual(70f, conds[0].CurrentHP, 0.001f);
        }

        [Test]
        public void DistributeDamage_SkipsDestroyedParts()
        {
            // Two parts: destroy the first, then distribute damage.
            // All damage must go to the second part (only living one).
            var conds = AttachParts(2, maxHPEach: 50f);
            conds[0].TakeDamage(50f); // destroy part 0
            Assert.IsTrue(conds[0].IsDestroyed, "Pre-condition: part 0 must be destroyed");

            // Apply enough damage to destroy part 1 as well.
            _sys.DistributeDamage(50f);

            Assert.AreEqual(0f, conds[1].CurrentHP, 0.001f,
                "All damage should go to the only living part (part 1).");
        }

        [Test]
        public void DistributeDamage_ZeroAmount_IsNoOp()
        {
            var conds = AttachParts(2);
            float before = conds[0].CurrentHP;

            _sys.DistributeDamage(0f);

            // Neither part should change.
            Assert.AreEqual(before, conds[0].CurrentHP, 0.001f);
        }

        [Test]
        public void DistributeDamage_NoParts_DoesNotThrow()
        {
            // _parts is null.
            Assert.DoesNotThrow(() => _sys.DistributeDamage(100f));
        }

        // ── AreAllPartsDestroyed ──────────────────────────────────────────────

        [Test]
        public void AreAllPartsDestroyed_EmptyParts_ReturnsFalse()
        {
            Assert.IsFalse(_sys.AreAllPartsDestroyed);
        }

        [Test]
        public void AreAllPartsDestroyed_TrueWhenAllDestroyed()
        {
            var conds = AttachParts(2, maxHPEach: 10f);
            conds[0].TakeDamage(10f);
            conds[1].TakeDamage(10f);

            Assert.IsTrue(_sys.AreAllPartsDestroyed);
        }

        [Test]
        public void AreAllPartsDestroyed_FalseWhenAtLeastOneLiving()
        {
            var conds = AttachParts(2, maxHPEach: 10f);
            conds[0].TakeDamage(10f); // destroy only first

            Assert.IsFalse(_sys.AreAllPartsDestroyed);
        }

        // ── _onAllPartsDestroyed event ────────────────────────────────────────

        [Test]
        public void DistributeDamage_FiresAllDestroyedEventWhenLastPartFalls()
        {
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            evt.RegisterCallback(() => count++);
            SetField(_sys, "_onAllPartsDestroyed", evt);

            AttachParts(1, maxHPEach: 20f);
            _sys.DistributeDamage(20f); // destroys the only part

            Assert.AreEqual(1, count, "Event must fire when the last part is destroyed.");

            Object.DestroyImmediate(evt);
        }

        [Test]
        public void DistributeDamage_AllDestroyedEventFiredAtMostOnce()
        {
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            evt.RegisterCallback(() => count++);
            SetField(_sys, "_onAllPartsDestroyed", evt);

            AttachParts(1, maxHPEach: 10f);
            _sys.DistributeDamage(10f); // destroys part + fires event
            _sys.DistributeDamage(10f); // all already destroyed — no additional fire

            Assert.AreEqual(1, count, "Event must not fire more than once per Reset cycle.");

            Object.DestroyImmediate(evt);
        }

        // ── GetLivingPartCount after damage ───────────────────────────────────

        [Test]
        public void GetLivingPartCount_DecrementsWhenPartDestroyed()
        {
            var conds = AttachParts(3, maxHPEach: 10f);

            conds[0].TakeDamage(10f); // destroy part 0

            Assert.AreEqual(2, _sys.GetLivingPartCount());
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_RestoresAllPartsToFullHP()
        {
            var conds = AttachParts(2, maxHPEach: 50f);
            _sys.DistributeDamage(25f); // damage a part
            _sys.Reset();

            // After reset, all parts should be at full HP.
            for (int i = 0; i < conds.Length; i++)
                Assert.AreEqual(conds[i].MaxHP, conds[i].CurrentHP, 0.001f,
                    $"Part {i} HP should be MaxHP after Reset.");
        }

        [Test]
        public void Reset_AllowsAllDestroyedEventToRefireAfterReset()
        {
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            int count = 0;
            evt.RegisterCallback(() => count++);
            SetField(_sys, "_onAllPartsDestroyed", evt);

            AttachParts(1, maxHPEach: 10f);
            _sys.DistributeDamage(10f); // fires event (count = 1)
            _sys.Reset();               // clears guard
            _sys.DistributeDamage(10f); // fires again (count = 2)

            Assert.AreEqual(2, count, "Event should be re-fireable after Reset.");

            Object.DestroyImmediate(evt);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CombatStatsApplicator"/>.
    ///
    /// Covers:
    ///   • <see cref="CombatStatsApplicator.ApplyStats"/> with a null RobotDefinition
    ///     (should not throw; health is reset to 0 or clamped to 1).
    ///   • Stat fields applied to HealthSO: TotalMaxHealth via InitForMatch+Reset.
    ///   • Stat fields applied to DamageReceiver: TotalArmorRating via SetArmorRating.
    ///   • Stat fields applied to RobotAIController: EffectiveDamageMultiplier.
    ///   • Stat fields applied to RobotLocomotionController: EffectiveSpeed via SetBaseSpeed.
    ///   • Optional fields absent: no NullReferenceException when _locomotion / _aiController
    ///     / _damageReceiver are null.
    ///   • VoidGameEvent subscription / unsubscription wiring.
    ///
    /// Private serialised fields are injected via reflection — same pattern as other tests.
    /// <c>RobotLocomotionController</c> requires [RequireComponent(ArticulationBody)] which
    /// Unity auto-adds on AddComponent; ArticulationBody works as a data container in
    /// EditMode even without PhysX ticking.
    /// </summary>
    public class CombatStatsApplicatorTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject              _go;
        private CombatStatsApplicator   _applicator;

        // ── SOs ───────────────────────────────────────────────────────────────
        private RobotDefinition         _robotDef;
        private HealthSO                _health;
        private VoidGameEvent           _matchStarted;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetPrivateFieldOnReceiver(DamageReceiver receiver, string name, object value)
        {
            FieldInfo fi = typeof(DamageReceiver)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on DamageReceiver.");
            fi.SetValue(receiver, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go         = new GameObject("TestApplicator");
            _applicator = _go.AddComponent<CombatStatsApplicator>();

            _robotDef     = ScriptableObject.CreateInstance<RobotDefinition>();
            _health       = ScriptableObject.CreateInstance<HealthSO>();
            _matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();

            _health.Reset();   // CurrentHealth = MaxHealth = 100

            // Wire mandatory fields.
            SetPrivateField(_applicator, "_robotDefinition",  _robotDef);
            SetPrivateField(_applicator, "_health",           _health);
            SetPrivateField(_applicator, "_matchStartedEvent", _matchStarted);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_robotDef);
            Object.DestroyImmediate(_health);
            Object.DestroyImmediate(_matchStarted);
            _go           = null;
            _applicator   = null;
            _robotDef     = null;
            _health       = null;
            _matchStarted = null;
        }

        // ── ApplyStats — null safety ──────────────────────────────────────────

        [Test]
        public void ApplyStats_NullRobotDefinition_DoesNotThrow()
        {
            SetPrivateField(_applicator, "_robotDefinition", null);
            // RobotStatsAggregator.Compute returns 0-stats on null def; HealthSO clamps
            // InitForMatch(0) to 1, then resets. Should not throw.
            Assert.DoesNotThrow(() => _applicator.ApplyStats());
        }

        [Test]
        public void ApplyStats_NullHealth_DoesNotThrow()
        {
            SetPrivateField(_applicator, "_health", null);
            Assert.DoesNotThrow(() => _applicator.ApplyStats());
        }

        [Test]
        public void ApplyStats_AllOptionalFieldsNull_DoesNotThrow()
        {
            // _locomotion, _aiController, _damageReceiver all default to null in
            // the freshly-added component. ApplyStats should handle this gracefully.
            Assert.DoesNotThrow(() => _applicator.ApplyStats());
        }

        // ── Health application ────────────────────────────────────────────────

        [Test]
        public void ApplyStats_SetsHealthToRobotDefMaxHitPoints()
        {
            // RobotDefinition default _maxHitPoints is 100 (field initialiser).
            // With no parts (no assembler), TotalMaxHealth == 100.
            _applicator.ApplyStats();
            Assert.AreEqual(100f, _health.MaxHealth, 0.001f);
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyStats_ResetsHealthToFull()
        {
            // Damage the robot, then re-apply stats — health should be restored.
            _health.ApplyDamage(60f);
            Assert.AreEqual(40f, _health.CurrentHealth, 0.001f);

            _applicator.ApplyStats();
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        // ── Armor rating application ──────────────────────────────────────────

        [Test]
        public void ApplyStats_SetsArmorRatingOnDamageReceiver()
        {
            // RobotDefinition has no parts → TotalArmorRating = 0.
            var receiverGo = new GameObject("Receiver");
            var receiver   = receiverGo.AddComponent<DamageReceiver>();
            SetPrivateFieldOnReceiver(receiver, "_health", _health);

            SetPrivateField(_applicator, "_damageReceiver", receiver);

            _applicator.ApplyStats();
            Assert.AreEqual(0, receiver.ArmorRating);

            Object.DestroyImmediate(receiverGo);
        }

        // ── Damage multiplier application ─────────────────────────────────────

        [Test]
        public void ApplyStats_SetsDamageMultiplierOnAIController()
        {
            // No parts → EffectiveDamageMultiplier == 1.0 (product of no multipliers).
            var aiGo = new GameObject("AI");
            var ai   = aiGo.AddComponent<RobotAIController>();

            SetPrivateField(_applicator, "_aiController", ai);

            _applicator.ApplyStats();
            Assert.AreEqual(1f, ai.DamageMultiplier, 0.001f);

            Object.DestroyImmediate(aiGo);
        }

        // ── Base speed application ────────────────────────────────────────────

        [Test]
        public void ApplyStats_SetsBaseSpeedOnLocomotionController()
        {
            // No parts → EffectiveSpeed == RobotDefinition.MoveSpeed (default 5).
            var locoGo = new GameObject("Loco");
            var loco   = locoGo.AddComponent<RobotLocomotionController>();

            SetPrivateField(_applicator, "_locomotion", loco);

            _applicator.ApplyStats();

            // RobotDefinition default _moveSpeed is 5.
            Assert.AreEqual(5f, loco.BaseSpeed, 0.001f);

            Object.DestroyImmediate(locoGo);
        }

        // ── VoidGameEvent subscription ────────────────────────────────────────

        [Test]
        public void ApplyStats_FiredViaSOEvent_SetsHealth()
        {
            // Enable the component so it registers its callback.
            _go.SetActive(false);
            _go.SetActive(true);   // triggers OnEnable → RegisterCallback

            _health.ApplyDamage(50f);       // damage to 50
            _matchStarted.Raise();          // should trigger ApplyStats → Reset health
            Assert.AreEqual(100f, _health.CurrentHealth, 0.001f);
        }

        [Test]
        public void ApplyStats_UnregisteredAfterOnDisable_NoLongerFires()
        {
            _go.SetActive(false);
            _go.SetActive(true);
            _go.SetActive(false);   // OnDisable → UnregisterCallback

            _health.ApplyDamage(50f);
            _matchStarted.Raise();          // should NOT trigger ApplyStats
            // Health should remain at 50, not reset to 100.
            Assert.AreEqual(50f, _health.CurrentHealth, 0.001f);
        }
    }
}

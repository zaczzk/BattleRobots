using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="RobotAIController"/>.
    ///
    /// Covers:
    ///   • Default state (Idle) and DamageMultiplier (1.0).
    ///   • SetDamageMultiplier — value storage and clamping to 0.01 minimum.
    ///   • SetTarget — target stored in private field.
    ///   • Disable — transitions any state back to Idle; halts locomotion inputs;
    ///     no throw when _locomotion is null.
    ///   • Awake — difficulty config applied from _difficultyConfig; overridden by
    ///     SelectedDifficultySO.Current when assigned; null config leaves defaults.
    ///   • FixedUpdate — null locomotion / null target return early without throw.
    ///   • FixedUpdate FSM transitions — Idle→Chase when target enters detection range;
    ///     Idle stays Idle when target is out of range; Chase→Attack when target enters
    ///     attack range; Attack→Chase when target moves beyond attack range.
    ///
    /// Private methods and fields are accessed via reflection, matching the
    /// established pattern used in MatchFlowControllerTests and PauseManagerTests.
    ///
    /// For Awake tests the inactive-GO pattern is used: the GO is set inactive before
    /// AddComponent so that Awake is deferred until SetActive(true), allowing fields
    /// to be injected before the lifecycle method runs.
    ///
    /// FixedUpdate FSM transition tests use real Unity Transform positions — distance
    /// computation works correctly in EditMode without PhysX ticking.
    /// </summary>
    public class RobotAIControllerTests
    {
        // ── Scene objects ──────────────────────────────────────────────────────
        private GameObject        _go;
        private RobotAIController _ai;

        private readonly List<GameObject>       _extraGOs = new List<GameObject>();
        private readonly List<ScriptableObject> _extraSOs = new List<ScriptableObject>();

        // ── Reflection helpers ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            return (T)fi.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestAI");
            _ai = _go.AddComponent<RobotAIController>();
            // Awake fires: _difficultyConfig is null → defaults kept.
            _extraGOs.Clear();
            _extraSOs.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            foreach (var g in _extraGOs) if (g != null) Object.DestroyImmediate(g);
            foreach (var s in _extraSOs) if (s != null) Object.DestroyImmediate(s);
            _extraGOs.Clear();
            _extraSOs.Clear();
            _go = null;
            _ai = null;
        }

        // ── Helper factories ───────────────────────────────────────────────────

        private GameObject MakeGO(string name = "GO")
        {
            var go = new GameObject(name);
            _extraGOs.Add(go);
            return go;
        }

        private T MakeSO<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            _extraSOs.Add(so);
            return so;
        }

        /// <summary>
        /// Creates a RobotLocomotionController (RequireComponent auto-adds ArticulationBody).
        /// ArticulationBody acts as a data container in EditMode; Halt() does not throw.
        /// </summary>
        private RobotLocomotionController MakeLocomotion(string name = "Loco")
        {
            var go = MakeGO(name);
            return go.AddComponent<RobotLocomotionController>();
        }

        /// <summary>
        /// Creates a BotDifficultyConfig with all mandatory fields set so Awake
        /// can copy them without clamping warnings.
        /// </summary>
        private BotDifficultyConfig MakeConfig(float detectionRange = 15f,
                                               float attackRange     = 3f,
                                               float attackDamage    = 10f,
                                               float attackCooldown  = 1f,
                                               float facingThreshold = 20f,
                                               float speedMult       = 1f)
        {
            var cfg = MakeSO<BotDifficultyConfig>();
            SetField(cfg, "_detectionRange",     detectionRange);
            SetField(cfg, "_attackRange",        attackRange);
            SetField(cfg, "_attackDamage",       attackDamage);
            SetField(cfg, "_attackCooldown",     attackCooldown);
            SetField(cfg, "_facingThreshold",    facingThreshold);
            SetField(cfg, "_moveSpeedMultiplier", speedMult);
            return cfg;
        }

        // ── Default state ──────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentState_IsIdle()
        {
            Assert.AreEqual(AIState.Idle, _ai.CurrentState,
                "RobotAIController must start in Idle state.");
        }

        [Test]
        public void DamageMultiplier_Default_IsOne()
        {
            Assert.AreEqual(1f, _ai.DamageMultiplier, 1e-6f,
                "Default DamageMultiplier must be 1.0 (neutral).");
        }

        // ── SetDamageMultiplier ────────────────────────────────────────────────

        [Test]
        public void SetDamageMultiplier_StoresValue()
        {
            _ai.SetDamageMultiplier(1.5f);
            Assert.AreEqual(1.5f, _ai.DamageMultiplier, 1e-6f);
        }

        [Test]
        public void SetDamageMultiplier_ClampsNegativeToMin()
        {
            _ai.SetDamageMultiplier(-5f);
            Assert.AreEqual(0.01f, _ai.DamageMultiplier, 1e-6f,
                "Negative input must be clamped to 0.01.");
        }

        [Test]
        public void SetDamageMultiplier_ClampsZeroToMin()
        {
            _ai.SetDamageMultiplier(0f);
            Assert.AreEqual(0.01f, _ai.DamageMultiplier, 1e-6f,
                "Zero input must be clamped to 0.01 to prevent dead damage output.");
        }

        [Test]
        public void SetDamageMultiplier_CalledTwice_LastValueWins()
        {
            _ai.SetDamageMultiplier(2f);
            _ai.SetDamageMultiplier(0.5f);
            Assert.AreEqual(0.5f, _ai.DamageMultiplier, 1e-6f,
                "SetDamageMultiplier must replace (not compound) the stored multiplier.");
        }

        // ── SetTarget ─────────────────────────────────────────────────────────

        [Test]
        public void SetTarget_StoresTarget()
        {
            var targetGO = MakeGO("Target");
            _ai.SetTarget(targetGO.transform);
            var stored = GetField<Transform>(_ai, "_target");
            Assert.AreSame(targetGO.transform, stored,
                "SetTarget must store the supplied Transform in _target.");
        }

        // ── Disable ───────────────────────────────────────────────────────────

        [Test]
        public void Disable_SetsStateToIdle_WhenAlreadyIdle()
        {
            _ai.Disable();
            Assert.AreEqual(AIState.Idle, _ai.CurrentState);
        }

        [Test]
        public void Disable_SetsStateToIdle_WhenChasing()
        {
            SetField(_ai, "_state", AIState.Chase);
            _ai.Disable();
            Assert.AreEqual(AIState.Idle, _ai.CurrentState,
                "Disable must reset Chase → Idle.");
        }

        [Test]
        public void Disable_SetsStateToIdle_WhenAttacking()
        {
            SetField(_ai, "_state", AIState.Attack);
            _ai.Disable();
            Assert.AreEqual(AIState.Idle, _ai.CurrentState,
                "Disable must reset Attack → Idle.");
        }

        [Test]
        public void Disable_NullLocomotion_DoesNotThrow()
        {
            // _locomotion is null by default — Halt() must be null-guarded.
            Assert.DoesNotThrow(() => _ai.Disable());
        }

        [Test]
        public void Disable_HaltsLocomotionInputs()
        {
            var loco = MakeLocomotion();
            loco.SetInputs(1f, 0.5f);
            SetField(_ai, "_locomotion", loco);

            _ai.Disable();

            Assert.AreEqual(0f, loco.MoveInput, 1e-6f, "Halt must zero MoveInput.");
            Assert.AreEqual(0f, loco.TurnInput, 1e-6f, "Halt must zero TurnInput.");
        }

        // ── Awake — difficulty override ────────────────────────────────────────

        [Test]
        public void Awake_WithDifficultyConfig_OverridesDetectionRange()
        {
            var go = new GameObject("AIAwakeTest");
            _extraGOs.Add(go);
            go.SetActive(false);

            var ai     = go.AddComponent<RobotAIController>();   // Awake deferred
            var config = MakeConfig(detectionRange: 99f);
            SetField(ai, "_difficultyConfig", config);

            go.SetActive(true);   // triggers Awake

            float applied = GetField<float>(ai, "_detectionRange");
            Assert.AreEqual(99f, applied, 1e-6f,
                "Awake must copy DetectionRange from _difficultyConfig.");
        }

        [Test]
        public void Awake_NullDifficultyConfig_KeepsInspectorDefault()
        {
            // Standard SetUp GO: _difficultyConfig is null; default _detectionRange is 15f.
            float applied = GetField<float>(_ai, "_detectionRange");
            Assert.AreEqual(15f, applied, 1e-6f,
                "Null _difficultyConfig must leave _detectionRange at inspector default (15).");
        }

        [Test]
        public void Awake_SelectedDifficulty_TakesPrecedenceOverDifficultyConfig()
        {
            var go = new GameObject("AISelectedTest");
            _extraGOs.Add(go);
            go.SetActive(false);

            var ai = go.AddComponent<RobotAIController>();   // Awake deferred

            // Easy config with distinctive detection range.
            var easy = MakeConfig(detectionRange: 8f);

            // SelectedDifficultySO.Select() writes Current = easy.
            var selected = MakeSO<SelectedDifficultySO>();
            selected.Select(easy);

            // Hard config that should be overridden.
            var hard = MakeConfig(detectionRange: 22f);

            SetField(ai, "_difficultyConfig",  hard);
            SetField(ai, "_selectedDifficulty", selected);

            go.SetActive(true);   // triggers Awake — SelectedDifficulty.Current wins

            float applied = GetField<float>(ai, "_detectionRange");
            Assert.AreEqual(8f, applied, 1e-6f,
                "SelectedDifficultySO.Current must override _difficultyConfig in Awake.");
        }

        // ── FixedUpdate — null guards ──────────────────────────────────────────

        [Test]
        public void FixedUpdate_NullLocomotion_DoesNotThrow()
        {
            // Both _locomotion and _target null → early return guard must fire.
            Assert.DoesNotThrow(() => InvokePrivate(_ai, "FixedUpdate"));
        }

        [Test]
        public void FixedUpdate_NullTarget_DoesNotThrow()
        {
            var loco = MakeLocomotion();
            SetField(_ai, "_locomotion", loco);
            // _target is null → early return guard must fire.
            Assert.DoesNotThrow(() => InvokePrivate(_ai, "FixedUpdate"));
        }

        // ── FixedUpdate — FSM state transitions ───────────────────────────────

        [Test]
        public void FixedUpdate_Idle_TargetInDetectionRange_TransitionsToChase()
        {
            var loco   = MakeLocomotion();
            var target = MakeGO("Target");

            SetField(_ai, "_locomotion",     loco);
            SetField(_ai, "_target",         target.transform);
            SetField(_ai, "_detectionRange", 20f);
            SetField(_ai, "_attackRange",    3f);

            _go.transform.position    = Vector3.zero;
            target.transform.position = new Vector3(10f, 0f, 0f);   // 10 m — within detection range

            Assert.AreEqual(AIState.Idle, _ai.CurrentState, "Pre-condition: state must be Idle.");

            InvokePrivate(_ai, "FixedUpdate");

            Assert.AreEqual(AIState.Chase, _ai.CurrentState,
                "AI must transition Idle → Chase when target is within detection range.");
        }

        [Test]
        public void FixedUpdate_Idle_TargetOutOfRange_StaysIdle()
        {
            var loco   = MakeLocomotion();
            var target = MakeGO("Target");

            SetField(_ai, "_locomotion",     loco);
            SetField(_ai, "_target",         target.transform);
            SetField(_ai, "_detectionRange", 5f);

            _go.transform.position    = Vector3.zero;
            target.transform.position = new Vector3(30f, 0f, 0f);   // 30 m — beyond 5 m range

            InvokePrivate(_ai, "FixedUpdate");

            Assert.AreEqual(AIState.Idle, _ai.CurrentState,
                "AI must stay Idle when target is beyond detection range.");
        }

        [Test]
        public void FixedUpdate_Chase_TargetInAttackRange_TransitionsToAttack()
        {
            var loco   = MakeLocomotion();
            var target = MakeGO("Target");

            SetField(_ai, "_locomotion",     loco);
            SetField(_ai, "_target",         target.transform);
            SetField(_ai, "_detectionRange", 20f);
            SetField(_ai, "_attackRange",    10f);
            SetField(_ai, "_state",          AIState.Chase);

            _go.transform.position    = Vector3.zero;
            target.transform.position = new Vector3(5f, 0f, 0f);   // 5 m — inside 10 m attack range

            InvokePrivate(_ai, "FixedUpdate");

            Assert.AreEqual(AIState.Attack, _ai.CurrentState,
                "AI must transition Chase → Attack when target is within attack range.");
        }

        [Test]
        public void FixedUpdate_Attack_TargetMovedOutOfRange_TransitionsToChase()
        {
            var loco   = MakeLocomotion();
            var target = MakeGO("Target");

            SetField(_ai, "_locomotion",  loco);
            SetField(_ai, "_target",      target.transform);
            SetField(_ai, "_detectionRange", 20f);
            SetField(_ai, "_attackRange", 5f);
            SetField(_ai, "_state",       AIState.Attack);

            _go.transform.position    = Vector3.zero;
            target.transform.position = new Vector3(8f, 0f, 0f);   // 8 m — beyond 5 m attack range

            InvokePrivate(_ai, "FixedUpdate");

            Assert.AreEqual(AIState.Chase, _ai.CurrentState,
                "AI must transition Attack → Chase when target moves beyond attack range.");
        }

        // ── Personality — Awake integration ───────────────────────────────────

        /// <summary>Helper: creates a BotPersonalitySO with all neutral defaults via reflection.</summary>
        private BotPersonalitySO MakeNeutralPersonality()
        {
            var p = MakeSO<BotPersonalitySO>();
            SetField(p, "_attackCooldownMultiplier",  1f);
            SetField(p, "_detectionRangeDelta",        0f);
            SetField(p, "_attackRangeDelta",            0f);
            SetField(p, "_facingThresholdMultiplier",  1f);
            return p;
        }

        [Test]
        public void Awake_NullPersonality_DoesNotChangeDefaults()
        {
            // Standard SetUp: _botPersonality is null → defaults unchanged.
            Assert.AreEqual(1f,  GetField<float>(_ai, "_attackCooldown"),  1e-6f);
            Assert.AreEqual(15f, GetField<float>(_ai, "_detectionRange"),  1e-6f);
            Assert.AreEqual(3f,  GetField<float>(_ai, "_attackRange"),     1e-6f);
            Assert.AreEqual(20f, GetField<float>(_ai, "_facingThreshold"), 1e-6f);
        }

        [Test]
        public void Awake_Personality_ScalesAttackCooldown()
        {
            var go = new GameObject("AIPersonality_Cooldown");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            var p = MakeNeutralPersonality();
            SetField(p, "_attackCooldownMultiplier", 0.5f);
            SetField(ai, "_botPersonality", p);

            go.SetActive(true);   // triggers Awake; _attackCooldown default 1f × 0.5 = 0.5f

            Assert.AreEqual(0.5f, GetField<float>(ai, "_attackCooldown"), 1e-6f,
                "Awake must multiply _attackCooldown by AttackCooldownMultiplier.");
        }

        [Test]
        public void Awake_Personality_AddsDetectionRangeDelta()
        {
            var go = new GameObject("AIPersonality_Detection");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            var p = MakeNeutralPersonality();
            SetField(p, "_detectionRangeDelta", 5f);
            SetField(ai, "_botPersonality", p);

            go.SetActive(true);   // 15 (default) + 5 = 20

            Assert.AreEqual(20f, GetField<float>(ai, "_detectionRange"), 1e-6f,
                "Awake must add DetectionRangeDelta to _detectionRange.");
        }

        [Test]
        public void Awake_Personality_AddsAttackRangeDelta()
        {
            var go = new GameObject("AIPersonality_Attack");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            var p = MakeNeutralPersonality();
            SetField(p, "_attackRangeDelta", 2f);
            SetField(ai, "_botPersonality", p);

            go.SetActive(true);   // 3 (default) + 2 = 5

            Assert.AreEqual(5f, GetField<float>(ai, "_attackRange"), 1e-6f,
                "Awake must add AttackRangeDelta to _attackRange.");
        }

        [Test]
        public void Awake_Personality_ScalesFacingThreshold()
        {
            var go = new GameObject("AIPersonality_Facing");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            var p = MakeNeutralPersonality();
            SetField(p, "_facingThresholdMultiplier", 2f);
            SetField(ai, "_botPersonality", p);

            go.SetActive(true);   // 20 (default) × 2 = 40

            Assert.AreEqual(40f, GetField<float>(ai, "_facingThreshold"), 1e-6f,
                "Awake must multiply _facingThreshold by FacingThresholdMultiplier.");
        }

        [Test]
        public void Awake_Personality_AppliedAfterDifficulty_Stacks()
        {
            var go = new GameObject("AIPersonality_Stack");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            // Difficulty sets attackCooldown = 2.0; personality halves it → 1.0.
            var config = MakeConfig(attackCooldown: 2f);
            var p      = MakeNeutralPersonality();
            SetField(p, "_attackCooldownMultiplier", 0.5f);

            SetField(ai, "_difficultyConfig", config);
            SetField(ai, "_botPersonality",   p);

            go.SetActive(true);   // 2.0 × 0.5 = 1.0

            Assert.AreEqual(1f, GetField<float>(ai, "_attackCooldown"), 1e-6f,
                "Personality must be applied AFTER difficulty: 2.0 × 0.5 = 1.0.");
        }

        [Test]
        public void Awake_Personality_AttackCooldownClampedToMinimum()
        {
            var go = new GameObject("AIPersonality_CooldownClamp");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            var p = MakeNeutralPersonality();
            // Force below-minimum multiplier via reflection (bypasses [Min] attribute).
            SetField(p, "_attackCooldownMultiplier", 0.05f);
            SetField(ai, "_botPersonality", p);

            go.SetActive(true);   // 1.0 × 0.05 = 0.05 → clamped to 0.1

            Assert.AreEqual(0.1f, GetField<float>(ai, "_attackCooldown"), 1e-6f,
                "Attack cooldown must be clamped to minimum 0.1 after personality multiply.");
        }

        [Test]
        public void Awake_Personality_NegativeDetectionDelta_ClampedToZero()
        {
            var go = new GameObject("AIPersonality_DetectionClamp");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            var p = MakeNeutralPersonality();
            SetField(p, "_detectionRangeDelta", -100f);   // would go deeply negative
            SetField(ai, "_botPersonality", p);

            go.SetActive(true);   // 15 + (−100) = −85 → clamped to 0

            Assert.AreEqual(0f, GetField<float>(ai, "_detectionRange"), 1e-6f,
                "Detection range must be clamped to 0 when delta drives it negative.");
        }

        // ── Awake: SelectedOpponentSO overrides ───────────────────────────────

        /// <summary>
        /// Creates a SelectedOpponentSO with the given profile already selected.
        /// The backing field is injected via reflection to bypass the Inspector serialisation path.
        /// </summary>
        private SelectedOpponentSO MakeSelectedOpponent(OpponentProfileSO profile)
        {
            var so = MakeSO<SelectedOpponentSO>();
            so.Select(profile);   // sets _current + _hasSelection = true
            return so;
        }

        [Test]
        public void Awake_NullSelectedOpponent_DoesNotChangeDefaults()
        {
            var go = new GameObject("AI_NullOpponent");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();
            // _selectedOpponent is null by default

            go.SetActive(true);

            // Default _detectionRange is 15 — opponent null path must not alter it.
            Assert.AreEqual(15f, GetField<float>(ai, "_detectionRange"), 1e-6f,
                "Null _selectedOpponent must not modify inspector defaults.");
        }

        [Test]
        public void Awake_SelectedOpponent_HasSelectionFalse_DoesNotOverride()
        {
            var go = new GameObject("AI_OpponentNoSelection");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            // Create a SelectedOpponentSO without calling Select() — HasSelection = false.
            var selectedSO = MakeSO<SelectedOpponentSO>();
            SetField(ai, "_selectedOpponent", selectedSO);

            go.SetActive(true);

            Assert.AreEqual(15f, GetField<float>(ai, "_detectionRange"), 1e-6f,
                "HasSelection == false must leave inspector defaults unchanged.");
        }

        [Test]
        public void Awake_SelectedOpponent_AppliesDifficultyConfig()
        {
            var go = new GameObject("AI_OpponentDifficulty");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            var config = MakeConfig(detectionRange: 99f, attackRange: 7f, attackCooldown: 0.3f);

            var profile = MakeSO<OpponentProfileSO>();
            SetField(profile, "_difficultyConfig", config);

            var selectedSO = MakeSelectedOpponent(profile);
            SetField(ai, "_selectedOpponent", selectedSO);

            go.SetActive(true);

            Assert.AreEqual(99f, GetField<float>(ai, "_detectionRange"), 1e-6f,
                "Opponent DifficultyConfig must override _detectionRange.");
            Assert.AreEqual(7f,  GetField<float>(ai, "_attackRange"),     1e-6f,
                "Opponent DifficultyConfig must override _attackRange.");
            Assert.AreEqual(0.3f, GetField<float>(ai, "_attackCooldown"), 1e-6f,
                "Opponent DifficultyConfig must override _attackCooldown.");
        }

        [Test]
        public void Awake_SelectedOpponent_AppliesPersonality()
        {
            var go = new GameObject("AI_OpponentPersonality");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            // Profile has no DifficultyConfig but has a personality that halves cooldown.
            var personality = MakeNeutralPersonality();
            SetField(personality, "_attackCooldownMultiplier", 0.5f);

            var profile = MakeSO<OpponentProfileSO>();
            SetField(profile, "_personality", personality);

            var selectedSO = MakeSelectedOpponent(profile);
            SetField(ai, "_selectedOpponent", selectedSO);

            go.SetActive(true);   // default cooldown 1.0 × 0.5 = 0.5

            Assert.AreEqual(0.5f, GetField<float>(ai, "_attackCooldown"), 1e-6f,
                "Opponent Personality must be applied: 1.0 × 0.5 = 0.5.");
        }

        [Test]
        public void Awake_SelectedOpponent_NullDifficultyInProfile_DoesNotOverrideDifficulty()
        {
            var go = new GameObject("AI_OpponentNullDiff");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            // Profile with no DifficultyConfig — should not override inspector defaults.
            var profile    = MakeSO<OpponentProfileSO>();
            var selectedSO = MakeSelectedOpponent(profile);
            SetField(ai, "_selectedOpponent", selectedSO);

            go.SetActive(true);

            Assert.AreEqual(15f, GetField<float>(ai, "_detectionRange"), 1e-6f,
                "Null DifficultyConfig in profile must not override _detectionRange.");
        }

        [Test]
        public void Awake_SelectedOpponent_StacksOnTopOfDirectDifficultyConfig()
        {
            var go = new GameObject("AI_OpponentStacks");
            _extraGOs.Add(go);
            go.SetActive(false);
            var ai = go.AddComponent<RobotAIController>();

            // Direct _difficultyConfig sets attackCooldown = 2.0
            var directConfig = MakeConfig(attackCooldown: 2f);
            SetField(ai, "_difficultyConfig", directConfig);

            // Opponent profile also carries a DifficultyConfig setting cooldown = 0.5
            // (opponent wins because it runs AFTER direct config in Awake).
            var opponentConfig = MakeConfig(attackCooldown: 0.5f);
            var profile        = MakeSO<OpponentProfileSO>();
            SetField(profile, "_difficultyConfig", opponentConfig);

            var selectedSO = MakeSelectedOpponent(profile);
            SetField(ai, "_selectedOpponent", selectedSO);

            go.SetActive(true);

            Assert.AreEqual(0.5f, GetField<float>(ai, "_attackCooldown"), 1e-6f,
                "SelectedOpponent must override _difficultyConfig (opponent runs LAST in Awake).");
        }
    }
}

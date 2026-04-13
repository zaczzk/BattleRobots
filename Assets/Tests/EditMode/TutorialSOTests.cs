using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // TutorialStepSO tests
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EditMode tests for <see cref="TutorialStepSO"/>.
    ///
    /// Covers:
    ///   • FreshInstance: StepId / HeaderText / BodyText default to empty string.
    ///   • Reflection round-trips: each field stores and returns the value set.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// </summary>
    public class TutorialStepSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        private TutorialStepSO _step;

        [SetUp]
        public void SetUp()
        {
            _step = ScriptableObject.CreateInstance<TutorialStepSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_step);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_StepId_IsEmpty()
        {
            Assert.AreEqual("", _step.StepId,
                "Fresh TutorialStepSO.StepId must default to empty string.");
        }

        [Test]
        public void FreshInstance_HeaderText_IsEmpty()
        {
            Assert.AreEqual("", _step.HeaderText,
                "Fresh TutorialStepSO.HeaderText must default to empty string.");
        }

        [Test]
        public void FreshInstance_BodyText_IsEmpty()
        {
            Assert.AreEqual("", _step.BodyText,
                "Fresh TutorialStepSO.BodyText must default to empty string.");
        }

        // ── Reflection round-trips ────────────────────────────────────────────

        [Test]
        public void StepId_RoundTrips()
        {
            SetField(_step, "_stepId", "step_assembly");
            Assert.AreEqual("step_assembly", _step.StepId,
                "StepId must reflect the value written to the backing field.");
        }

        [Test]
        public void HeaderText_RoundTrips()
        {
            SetField(_step, "_headerText", "Assemble Your Robot");
            Assert.AreEqual("Assemble Your Robot", _step.HeaderText,
                "HeaderText must reflect the value written to the backing field.");
        }

        [Test]
        public void BodyText_RoundTrips()
        {
            SetField(_step, "_bodyText", "Drag parts from the panel on the left.");
            Assert.AreEqual("Drag parts from the panel on the left.", _step.BodyText,
                "BodyText must reflect the value written to the backing field.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TutorialSequenceSO tests
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EditMode tests for <see cref="TutorialSequenceSO"/>.
    ///
    /// Covers:
    ///   • FreshInstance: Steps is non-null, Count is 0.
    ///   • Steps returns an IReadOnlyList.
    ///   • Insertion order is preserved.
    ///
    /// All tests run headless.
    /// </summary>
    public class TutorialSequenceSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        private TutorialSequenceSO _seq;

        [SetUp]
        public void SetUp()
        {
            _seq = ScriptableObject.CreateInstance<TutorialSequenceSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_seq);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_Steps_NotNull()
        {
            Assert.IsNotNull(_seq.Steps,
                "Fresh TutorialSequenceSO.Steps must not be null.");
        }

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            Assert.AreEqual(0, _seq.Count,
                "Fresh TutorialSequenceSO.Count must be 0 when no steps have been added.");
        }

        [Test]
        public void Steps_ReturnsIReadOnlyList()
        {
            Assert.IsInstanceOf<System.Collections.Generic.IReadOnlyList<TutorialStepSO>>(_seq.Steps,
                "Steps must implement IReadOnlyList<TutorialStepSO>.");
        }

        [Test]
        public void Steps_InsertionOrderPreserved()
        {
            var stepA = ScriptableObject.CreateInstance<TutorialStepSO>();
            var stepB = ScriptableObject.CreateInstance<TutorialStepSO>();
            SetField(stepA, "_stepId", "A");
            SetField(stepB, "_stepId", "B");

            var list = new List<TutorialStepSO> { stepA, stepB };
            SetField(_seq, "_steps", list);

            Assert.AreEqual("A", _seq.Steps[0].StepId, "First step must be stepA.");
            Assert.AreEqual("B", _seq.Steps[1].StepId, "Second step must be stepB.");
            Assert.AreEqual(2,   _seq.Count,            "Count must reflect the two added steps.");

            Object.DestroyImmediate(stepA);
            Object.DestroyImmediate(stepB);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TutorialProgressSO tests
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// EditMode tests for <see cref="TutorialProgressSO"/>.
    ///
    /// Covers:
    ///   • FreshInstance: IsComplete false, HasCompletedStep false for any id.
    ///   • MarkStepComplete: null/whitespace → no-op; valid id → HasCompletedStep true.
    ///   • MarkStepComplete: fires _onStepCompleted event.
    ///   • HasCompletedStep: null id → false.
    ///   • Complete: sets IsComplete true and fires _onTutorialCompleted.
    ///   • LoadSnapshot: rehydrates isComplete and completedIds; null ids → empty set.
    ///   • Reset: clears all; silent (no events).
    ///
    /// All tests run headless.
    /// </summary>
    public class TutorialProgressSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        private TutorialProgressSO _progress;
        private VoidGameEvent      _onStep;
        private VoidGameEvent      _onCompleted;

        [SetUp]
        public void SetUp()
        {
            _progress    = ScriptableObject.CreateInstance<TutorialProgressSO>();
            _onStep      = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onCompleted = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_progress);
            Object.DestroyImmediate(_onStep);
            Object.DestroyImmediate(_onCompleted);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_IsComplete_IsFalse()
        {
            Assert.IsFalse(_progress.IsComplete,
                "Fresh TutorialProgressSO.IsComplete must be false.");
        }

        [Test]
        public void FreshInstance_HasCompletedStep_IsFalse()
        {
            Assert.IsFalse(_progress.HasCompletedStep("any_step"),
                "Fresh TutorialProgressSO.HasCompletedStep must return false for any step.");
        }

        // ── MarkStepComplete — null / whitespace guards ───────────────────────

        [Test]
        public void MarkStepComplete_Null_IsNoOp()
        {
            SetField(_progress, "_onStepCompleted", _onStep);
            int fired = 0;
            _onStep.RegisterCallback(() => fired++);

            _progress.MarkStepComplete(null);

            Assert.IsFalse(_progress.HasCompletedStep(null),
                "Null stepId must not be added to completed set.");
            Assert.AreEqual(0, fired, "Null stepId must not fire _onStepCompleted.");
        }

        [Test]
        public void MarkStepComplete_Whitespace_IsNoOp()
        {
            _progress.MarkStepComplete("   ");
            Assert.IsFalse(_progress.HasCompletedStep("   "),
                "Whitespace-only stepId must not be added to completed set.");
        }

        // ── MarkStepComplete — happy path ─────────────────────────────────────

        [Test]
        public void MarkStepComplete_Valid_AddsToSet()
        {
            _progress.MarkStepComplete("step_01");
            Assert.IsTrue(_progress.HasCompletedStep("step_01"),
                "HasCompletedStep must return true after MarkStepComplete with that id.");
        }

        [Test]
        public void MarkStepComplete_FiresOnStepCompleted()
        {
            SetField(_progress, "_onStepCompleted", _onStep);
            int fired = 0;
            _onStep.RegisterCallback(() => fired++);

            _progress.MarkStepComplete("step_02");

            Assert.AreEqual(1, fired, "MarkStepComplete must fire _onStepCompleted.");
        }

        // ── HasCompletedStep — null guard ─────────────────────────────────────

        [Test]
        public void HasCompletedStep_Null_ReturnsFalse()
        {
            _progress.MarkStepComplete("step_01"); // ensure some state exists
            Assert.IsFalse(_progress.HasCompletedStep(null),
                "HasCompletedStep(null) must return false.");
        }

        // ── Complete ──────────────────────────────────────────────────────────

        [Test]
        public void Complete_SetsIsComplete()
        {
            _progress.Complete();
            Assert.IsTrue(_progress.IsComplete,
                "Complete() must set IsComplete to true.");
        }

        [Test]
        public void Complete_FiresOnTutorialCompleted()
        {
            SetField(_progress, "_onTutorialCompleted", _onCompleted);
            int fired = 0;
            _onCompleted.RegisterCallback(() => fired++);

            _progress.Complete();

            Assert.AreEqual(1, fired, "Complete() must fire _onTutorialCompleted.");
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsIsComplete()
        {
            _progress.LoadSnapshot(true, null);
            Assert.IsTrue(_progress.IsComplete,
                "LoadSnapshot(true, null) must set IsComplete to true.");
        }

        [Test]
        public void LoadSnapshot_NullIds_EmptySet()
        {
            _progress.LoadSnapshot(false, null);
            Assert.IsFalse(_progress.HasCompletedStep("any"),
                "LoadSnapshot with null completedIds must result in an empty completed set.");
        }

        [Test]
        public void LoadSnapshot_RehydratesCompletedIds()
        {
            var ids = new List<string> { "step_01", "step_02" };
            _progress.LoadSnapshot(false, ids);

            Assert.IsTrue(_progress.HasCompletedStep("step_01"),
                "LoadSnapshot must rehydrate 'step_01' into the completed set.");
            Assert.IsTrue(_progress.HasCompletedStep("step_02"),
                "LoadSnapshot must rehydrate 'step_02' into the completed set.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsIsComplete()
        {
            _progress.Complete();
            _progress.Reset();
            Assert.IsFalse(_progress.IsComplete,
                "Reset must clear IsComplete back to false.");
        }

        [Test]
        public void Reset_ClearsCompletedSteps()
        {
            _progress.MarkStepComplete("step_01");
            _progress.Reset();
            Assert.IsFalse(_progress.HasCompletedStep("step_01"),
                "Reset must clear all completed step IDs.");
        }

        [Test]
        public void Reset_Silent_DoesNotFireEvents()
        {
            SetField(_progress, "_onStepCompleted",     _onStep);
            SetField(_progress, "_onTutorialCompleted", _onCompleted);
            int fired = 0;
            _onStep.RegisterCallback(()      => fired++);
            _onCompleted.RegisterCallback(() => fired++);

            _progress.MarkStepComplete("step_01");
            _progress.Complete();
            fired = 0; // reset counter after setup

            _progress.Reset();

            Assert.AreEqual(0, fired,
                "Reset must be silent — must not fire _onStepCompleted or _onTutorialCompleted.");
        }
    }
}

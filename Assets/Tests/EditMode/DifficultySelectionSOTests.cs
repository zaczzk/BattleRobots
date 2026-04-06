using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="DifficultySelectionSO"/>.
    ///
    /// Coverage:
    ///   - Default state: SelectedLevel = Medium, HasSelection = false.
    ///   - Select(Easy/Medium/Hard) sets SelectedLevel and HasSelection = true.
    ///   - Select mutates the wired DifficultySO via LoadPreset.
    ///   - Selecting different levels updates SelectedLevel each time.
    ///   - Reset() clears HasSelection and restores default level.
    ///   - Reset() also applies the default preset to the wired DifficultySO.
    ///   - ActiveDifficulty returns the wired DifficultySO reference.
    ///   - Select/Reset with no DifficultySO wired does not throw.
    ///   - DifficultySO name matches expected preset after Select.
    ///   - Consecutive Select calls with different levels (last wins).
    ///   - Reset after Hard restores Medium values on DifficultySO.
    ///   - HasSelection remains true after a second Select call.
    /// </summary>
    [TestFixture]
    public sealed class DifficultySelectionSOTests
    {
        private DifficultySelectionSO _selection;
        private DifficultySO          _difficultySO;

        [SetUp]
        public void SetUp()
        {
            _difficultySO = ScriptableObject.CreateInstance<DifficultySO>();
            _selection    = ScriptableObject.CreateInstance<DifficultySelectionSO>();

            // Wire the DifficultySO via reflection (no Inspector in EditMode).
            typeof(DifficultySelectionSO)
                .GetField("_difficultySO",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_selection, _difficultySO);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_difficultySO);
            Object.DestroyImmediate(_selection);
        }

        // ── Default state ─────────────────────────────────────────────────────

        [Test]
        public void DefaultState_SelectedLevel_IsMedium()
        {
            Assert.AreEqual(DifficultyLevel.Medium, _selection.SelectedLevel);
        }

        [Test]
        public void DefaultState_HasSelection_IsFalse()
        {
            Assert.IsFalse(_selection.HasSelection);
        }

        [Test]
        public void ActiveDifficulty_ReturnsSameInstance()
        {
            Assert.AreSame(_difficultySO, _selection.ActiveDifficulty);
        }

        // ── Select ────────────────────────────────────────────────────────────

        [Test]
        public void Select_Easy_SetsSelectedLevelToEasy()
        {
            _selection.Select(DifficultyLevel.Easy);
            Assert.AreEqual(DifficultyLevel.Easy, _selection.SelectedLevel);
        }

        [Test]
        public void Select_Hard_SetsSelectedLevelToHard()
        {
            _selection.Select(DifficultyLevel.Hard);
            Assert.AreEqual(DifficultyLevel.Hard, _selection.SelectedLevel);
        }

        [Test]
        public void Select_SetsHasSelectionTrue()
        {
            _selection.Select(DifficultyLevel.Medium);
            Assert.IsTrue(_selection.HasSelection);
        }

        [Test]
        public void Select_Easy_MutatesDifficultySOName()
        {
            _selection.Select(DifficultyLevel.Easy);
            Assert.AreEqual("Easy", _difficultySO.DifficultyName);
        }

        [Test]
        public void Select_Hard_MutatesDifficultySOName()
        {
            _selection.Select(DifficultyLevel.Hard);
            Assert.AreEqual("Hard", _difficultySO.DifficultyName);
        }

        [Test]
        public void Select_Easy_AppliesLowerDamageMultiplierThanHard()
        {
            _selection.Select(DifficultyLevel.Easy);
            float easyMult = _difficultySO.DamageMultiplier;

            _selection.Select(DifficultyLevel.Hard);
            float hardMult = _difficultySO.DamageMultiplier;

            Assert.Less(easyMult, hardMult);
        }

        [Test]
        public void Select_SecondCall_OverridesFirstLevel()
        {
            _selection.Select(DifficultyLevel.Easy);
            _selection.Select(DifficultyLevel.Hard);
            Assert.AreEqual(DifficultyLevel.Hard, _selection.SelectedLevel);
        }

        [Test]
        public void Select_SecondCall_HasSelectionRemainsTrue()
        {
            _selection.Select(DifficultyLevel.Easy);
            _selection.Select(DifficultyLevel.Hard);
            Assert.IsTrue(_selection.HasSelection);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsHasSelection()
        {
            _selection.Select(DifficultyLevel.Hard);
            _selection.Reset();
            Assert.IsFalse(_selection.HasSelection);
        }

        [Test]
        public void Reset_RestoresDefaultLevel()
        {
            _selection.Select(DifficultyLevel.Hard);
            _selection.Reset();
            // Default level is Medium (matches the field default in the SO).
            Assert.AreEqual(DifficultyLevel.Medium, _selection.SelectedLevel);
        }

        [Test]
        public void Reset_AfterHard_RestoresMediumPresetOnDifficultySO()
        {
            _selection.Select(DifficultyLevel.Hard);
            _selection.Reset();
            // Medium damage multiplier is 1.0 by spec in DifficultySO.LoadPreset.
            Assert.AreEqual(1f, _difficultySO.DamageMultiplier, 0.001f);
        }

        [Test]
        public void Reset_WhenNeverSelected_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _selection.Reset());
        }

        // ── Null-safety ───────────────────────────────────────────────────────

        [Test]
        public void Select_WithNoDifficultySOWired_DoesNotThrow()
        {
            var bare = ScriptableObject.CreateInstance<DifficultySelectionSO>();
            try
            {
                Assert.DoesNotThrow(() => bare.Select(DifficultyLevel.Hard));
                Assert.AreEqual(DifficultyLevel.Hard, bare.SelectedLevel);
            }
            finally
            {
                Object.DestroyImmediate(bare);
            }
        }

        [Test]
        public void Reset_WithNoDifficultySOWired_DoesNotThrow()
        {
            var bare = ScriptableObject.CreateInstance<DifficultySelectionSO>();
            try
            {
                Assert.DoesNotThrow(() => bare.Reset());
            }
            finally
            {
                Object.DestroyImmediate(bare);
            }
        }
    }
}

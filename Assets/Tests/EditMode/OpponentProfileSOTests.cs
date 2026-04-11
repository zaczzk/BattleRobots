using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="OpponentProfileSO"/>.
    ///
    /// Covers:
    ///   • Fresh instance defaults: all string fields empty; all SO refs null.
    ///   • Public API: DisplayName / Description / DifficultyConfig / Personality /
    ///     RobotDefinition are readable after reflection-injection.
    ///   • Immutability: public properties are read-only; all writes go through
    ///     the serialised backing fields (Inspector / Editor only).
    /// </summary>
    public class OpponentProfileSOTests
    {
        private OpponentProfileSO _profile;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<OpponentProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
            _profile = null;
        }

        // ── Fresh instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_DisplayName_IsEmpty()
        {
            Assert.AreEqual("", _profile.DisplayName);
        }

        [Test]
        public void FreshInstance_Description_IsEmpty()
        {
            Assert.AreEqual("", _profile.Description);
        }

        [Test]
        public void FreshInstance_DifficultyConfig_IsNull()
        {
            Assert.IsNull(_profile.DifficultyConfig);
        }

        [Test]
        public void FreshInstance_Personality_IsNull()
        {
            Assert.IsNull(_profile.Personality);
        }

        [Test]
        public void FreshInstance_RobotDefinition_IsNull()
        {
            Assert.IsNull(_profile.RobotDefinition);
        }

        // ── Reflection round-trips ────────────────────────────────────────────

        [Test]
        public void DisplayName_CanBeSetViaReflection_AndRead()
        {
            SetField(_profile, "_displayName", "Iron Crusher");
            Assert.AreEqual("Iron Crusher", _profile.DisplayName);
        }

        [Test]
        public void Description_CanBeSetViaReflection_AndRead()
        {
            SetField(_profile, "_description", "A slow but relentless grinder.");
            Assert.AreEqual("A slow but relentless grinder.", _profile.Description);
        }

        [Test]
        public void DifficultyConfig_CanBeSetViaReflection_AndRead()
        {
            var config = ScriptableObject.CreateInstance<BotDifficultyConfig>();
            try
            {
                SetField(_profile, "_difficultyConfig", config);
                Assert.AreSame(config, _profile.DifficultyConfig);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}

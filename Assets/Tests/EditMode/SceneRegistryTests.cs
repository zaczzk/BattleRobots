using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="SceneRegistry"/>.
    ///
    /// Validates that the SO exposes correct default values and that all
    /// three scene-name properties return non-null, non-empty strings
    /// on a freshly created instance.
    ///
    /// Total: 7 test cases.
    /// </summary>
    public class SceneRegistryTests
    {
        private SceneRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<SceneRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_registry);
        }

        // ── Default values ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_MainMenuSceneName_IsDefaultMainMenu()
        {
            Assert.AreEqual("MainMenu", _registry.MainMenuSceneName);
        }

        [Test]
        public void FreshInstance_ArenaSceneName_IsDefaultArena()
        {
            Assert.AreEqual("Arena", _registry.ArenaSceneName);
        }

        [Test]
        public void FreshInstance_ShopSceneName_IsDefaultShop()
        {
            Assert.AreEqual("Shop", _registry.ShopSceneName);
        }

        // ── Non-null / non-empty contract ─────────────────────────────────────

        [Test]
        public void MainMenuSceneName_IsNotNullOrEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_registry.MainMenuSceneName),
                "MainMenuSceneName must not be null or whitespace on a fresh instance.");
        }

        [Test]
        public void ArenaSceneName_IsNotNullOrEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_registry.ArenaSceneName),
                "ArenaSceneName must not be null or whitespace on a fresh instance.");
        }

        [Test]
        public void ShopSceneName_IsNotNullOrEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_registry.ShopSceneName),
                "ShopSceneName must not be null or whitespace on a fresh instance.");
        }

        // ── All three names are distinct ──────────────────────────────────────

        [Test]
        public void AllDefaultSceneNames_AreDistinct()
        {
            string main  = _registry.MainMenuSceneName;
            string arena = _registry.ArenaSceneName;
            string shop  = _registry.ShopSceneName;

            Assert.AreNotEqual(main,  arena, "MainMenu and Arena scene names should be distinct.");
            Assert.AreNotEqual(main,  shop,  "MainMenu and Shop scene names should be distinct.");
            Assert.AreNotEqual(arena, shop,  "Arena and Shop scene names should be distinct.");
        }
    }
}

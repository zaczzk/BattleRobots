using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="UpgradeController"/>.
    ///
    /// Covers:
    ///   • <c>FormatTierStars</c> (internal static) — invoked via reflection to verify
    ///     star character formatting across tier/maxTier boundary conditions.
    ///   • <see cref="UpgradeController.Setup"/> null-safety guard — must not throw when
    ///     called with null or when all optional UI refs are absent.
    ///   • Reflection sanity — verifies <c>FormatTierStars</c> is findable.
    ///
    /// All tests run headless (no scene, no uGUI dependencies).  The
    /// <see cref="UpgradeController"/> MonoBehaviour is attached to a temporary
    /// <see cref="GameObject"/> created in SetUp and destroyed in TearDown.
    /// </summary>
    public class UpgradeControllerTests
    {
        // ── Reflection — FormatTierStars ───────────────────────────────────────

        private static readonly MethodInfo _formatTierStars =
            typeof(UpgradeController).GetMethod(
                "FormatTierStars",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(int), typeof(int) },
                null);

        // ── Scene objects ──────────────────────────────────────────────────────

        private GameObject       _go;
        private UpgradeController _ctrl;

        // ── Setup / Teardown ───────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("TestUpgradeController");
            _ctrl = _go.AddComponent<UpgradeController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            _go   = null;
            _ctrl = null;
        }

        // ── Reflection sanity ──────────────────────────────────────────────────

        [Test]
        public void ReflectionSanity_FormatTierStars_Found()
        {
            Assert.IsNotNull(_formatTierStars,
                "FormatTierStars(int, int) must be findable via reflection.");
        }

        // ── FormatTierStars — boundary: maxTier ≤ 0 ──────────────────────────

        [Test]
        public void FormatTierStars_MaxTierZero_ReturnsEmpty()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 0, 0 });
            Assert.AreEqual(string.Empty, result,
                "maxTier == 0 should return an empty string.");
        }

        [Test]
        public void FormatTierStars_NegativeMaxTier_ReturnsEmpty()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 0, -1 });
            Assert.AreEqual(string.Empty, result,
                "maxTier < 0 should return an empty string.");
        }

        // ── FormatTierStars — correct star characters ──────────────────────────

        [Test]
        public void FormatTierStars_TierZeroMaxThree_AllEmpty()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 0, 3 });
            Assert.AreEqual("\u2606\u2606\u2606", result,
                "Tier 0 of 3 should be three empty-star (☆) characters.");
        }

        [Test]
        public void FormatTierStars_TierOneMaxThree_OneFilledTwoEmpty()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 1, 3 });
            Assert.AreEqual("\u2605\u2606\u2606", result,
                "Tier 1 of 3 should be ★☆☆.");
        }

        [Test]
        public void FormatTierStars_TierTwoMaxThree_TwoFilledOneEmpty()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 2, 3 });
            Assert.AreEqual("\u2605\u2605\u2606", result,
                "Tier 2 of 3 should be ★★☆.");
        }

        [Test]
        public void FormatTierStars_TierEqualsMaxThree_AllFilled()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 3, 3 });
            Assert.AreEqual("\u2605\u2605\u2605", result,
                "Tier 3 of 3 should be ★★★ (all filled).");
        }

        [Test]
        public void FormatTierStars_MaxTierOne_TierZero_EmptyStar()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 0, 1 });
            Assert.AreEqual("\u2606", result,
                "Tier 0 of 1 should be a single empty-star (☆).");
        }

        [Test]
        public void FormatTierStars_MaxTierOne_TierOne_FilledStar()
        {
            string result = (string)_formatTierStars.Invoke(null, new object[] { 1, 1 });
            Assert.AreEqual("\u2605", result,
                "Tier 1 of 1 should be a single filled-star (★).");
        }

        [Test]
        public void FormatTierStars_TierExceedsMax_AllFilled()
        {
            // Tier beyond max: all positions i < tier, so all filled.
            string result = (string)_formatTierStars.Invoke(null, new object[] { 5, 3 });
            Assert.AreEqual("\u2605\u2605\u2605", result,
                "When tier > maxTier every slot is filled (★★★).");
        }

        // ── Setup — null-safety ────────────────────────────────────────────────

        [Test]
        public void Setup_NullPart_DoesNotThrow()
        {
            // All optional refs (_upgradeManager, _upgradeConfig, _onUpgradesChanged,
            // _tierLabel, _costLabel, _upgradeButton) are null on a freshly added MB.
            Assert.DoesNotThrow(() => _ctrl.Setup(null),
                "Setup(null) must not throw even when all optional refs are absent.");
        }

        [Test]
        public void Setup_ValidPartWithNoOptionalRefs_DoesNotThrow()
        {
            PartDefinition part = ScriptableObject.CreateInstance<PartDefinition>();
            try
            {
                Assert.DoesNotThrow(() => _ctrl.Setup(part),
                    "Setup(validPart) must not throw when all optional MB/UI refs are null.");
            }
            finally
            {
                Object.DestroyImmediate(part);
            }
        }

        [Test]
        public void Setup_CalledTwice_DoesNotThrow()
        {
            PartDefinition partA = ScriptableObject.CreateInstance<PartDefinition>();
            PartDefinition partB = ScriptableObject.CreateInstance<PartDefinition>();
            try
            {
                Assert.DoesNotThrow(() =>
                {
                    _ctrl.Setup(partA);
                    _ctrl.Setup(partB);
                }, "Calling Setup twice in succession must not throw.");
            }
            finally
            {
                Object.DestroyImmediate(partA);
                Object.DestroyImmediate(partB);
            }
        }
    }
}

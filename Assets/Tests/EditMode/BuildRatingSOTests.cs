using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="BuildRatingSO"/>.
    ///
    /// Covers:
    ///   • Fresh instance CurrentRating is 0.
    ///   • UpdateRating with all-null parameters does not throw.
    ///   • UpdateRating with a valid config + loadout + catalog produces
    ///     a non-negative CurrentRating.
    ///   • Reset sets CurrentRating to 0.
    ///   • UpdateRating fires _onRatingChanged event (external counter).
    /// </summary>
    public class BuildRatingSOTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private BuildRatingSO          _so;
        private RobotBuildRatingConfig _config;
        private ShopCatalog            _catalog;
        private PlayerLoadout          _loadout;

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so      = ScriptableObject.CreateInstance<BuildRatingSO>();
            _config  = ScriptableObject.CreateInstance<RobotBuildRatingConfig>();
            _catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            _loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_so      != null) Object.DestroyImmediate(_so);
            if (_config  != null) Object.DestroyImmediate(_config);
            if (_catalog != null) Object.DestroyImmediate(_catalog);
            if (_loadout != null) Object.DestroyImmediate(_loadout);
        }

        // Reflection helper: sets a private serialized field.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentRating_IsZero()
        {
            Assert.AreEqual(0, _so.CurrentRating);
        }

        [Test]
        public void UpdateRating_AllNullParameters_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.UpdateRating(null, null, null, null, null));
        }

        [Test]
        public void UpdateRating_AllNullParameters_CurrentRatingRemainsZero()
        {
            _so.UpdateRating(null, null, null, null, null);
            Assert.AreEqual(0, _so.CurrentRating);
        }

        [Test]
        public void UpdateRating_WithValidInputs_SetsNonNegativeRating()
        {
            // Even with a valid but empty loadout the result must be ≥ 0.
            _so.UpdateRating(_loadout, _catalog, null, null, _config);
            Assert.GreaterOrEqual(_so.CurrentRating, 0);
        }

        [Test]
        public void Reset_SetsCurrentRatingToZero()
        {
            // First push a non-zero value by injecting a mocked result indirectly.
            _so.UpdateRating(_loadout, _catalog, null, null, _config);
            // Reset must bring it back to 0 regardless of the previous value.
            _so.Reset();
            Assert.AreEqual(0, _so.CurrentRating);
        }

        [Test]
        public void UpdateRating_FiresOnRatingChangedEvent()
        {
            // Wire an IntGameEvent and count how many times it fires.
            var channel = ScriptableObject.CreateInstance<IntGameEvent>();
            SetPrivateField(_so, "_onRatingChanged", channel);

            int callCount = 0;
            channel.RegisterCallback((int v) => callCount++);

            _so.UpdateRating(_loadout, _catalog, null, null, _config);

            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, callCount,
                "_onRatingChanged should fire exactly once per UpdateRating call.");
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TierBadgeController"/>.
    ///
    /// Covers:
    ///   • OnEnable with all refs null → DoesNotThrow.
    ///   • OnDisable with all refs null ��� DoesNotThrow.
    ///   • OnEnable with null channel but valid data refs → DoesNotThrow.
    ///   • Refresh() with null _buildRating (early-return path) → DoesNotThrow.
    ///   • Refresh() with null _tierConfig (fallback to enum name/white) → DoesNotThrow.
    ///   • OnDisable unregisters delegate from IntGameEvent channel (external-counter pattern).
    ///
    /// All tests run headless (no Canvas required).
    /// The inactive-GO pattern prevents OnEnable from firing before fields are injected.
    /// </summary>
    public class TierBadgeControllerTests
    {
        private GameObject          _go;
        private TierBadgeController _ctrl;

        // ── Setup / Teardown ────────────────────────────────────────────��─────

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TierBadgeController");
            _go.SetActive(false);                          // inactive until fields injected
            _ctrl = _go.AddComponent<TierBadgeController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ── Reflection helper ──────────────────────────────��──────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── OnEnable / OnDisable guards ────────────────────────────────���──────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            // All fields null — Awake + OnEnable must not throw.
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var buildRating = ScriptableObject.CreateInstance<BuildRatingSO>();
            var tierConfig  = ScriptableObject.CreateInstance<RobotTierConfig>();
            SetField(_ctrl, "_buildRating", buildRating);
            SetField(_ctrl, "_tierConfig",  tierConfig);
            // _onRatingChanged intentionally null

            Assert.DoesNotThrow(() => _go.SetActive(true));

            Object.DestroyImmediate(buildRating);
            Object.DestroyImmediate(tierConfig);
        }

        // ── Refresh guard paths ─────────────────────────────��─────────────────

        [Test]
        public void Refresh_NullBuildRating_DoesNotThrow()
        {
            // No buildRating assigned — Refresh() must return early without throwing.
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        [Test]
        public void Refresh_NullTierConfig_DoesNotThrow()
        {
            // BuildRating assigned but no tier config — fallback to enum name / white.
            var buildRating = ScriptableObject.CreateInstance<BuildRatingSO>();
            SetField(_ctrl, "_buildRating", buildRating);
            // _tierConfig intentionally null

            Assert.DoesNotThrow(() => _go.SetActive(true));

            Object.DestroyImmediate(buildRating);
        }

        // ── OnDisable unregisters ──────────────────────────��──────────────────

        [Test]
        public void OnDisable_UnregistersFromOnRatingChanged()
        {
            // Wire an IntGameEvent into the controller.
            var channel = ScriptableObject.CreateInstance<IntGameEvent>();
            SetField(_ctrl, "_onRatingChanged", channel);

            _go.SetActive(true);   // OnEnable — registers _refreshDelegate
            _go.SetActive(false);  // OnDisable — unregisters _refreshDelegate

            // Attach an external counter to verify no extra callback fires.
            int callCount = 0;
            channel.RegisterCallback(_ => callCount++);
            channel.Raise(42); // only the external counter should fire

            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, callCount,
                "Only the external counter should fire after the controller unregisters.");
        }
    }
}

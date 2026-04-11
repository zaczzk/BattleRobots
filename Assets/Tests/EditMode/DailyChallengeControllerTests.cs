using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DailyChallengeController"/>.
    ///
    /// Covers:
    ///   • OnEnable with null _dailyChallenge → DoesNotThrow (Refresh early-return).
    ///   • OnEnable with null _onChallengeCompleted → DoesNotThrow.
    ///   • OnDisable with null _onChallengeCompleted → DoesNotThrow.
    ///   • Refresh() with null _dailyChallenge → DoesNotThrow (early return).
    ///   • Refresh() with null CurrentChallenge → DoesNotThrow (early return).
    ///   • Refresh() with a valid challenge but all UI refs null → DoesNotThrow.
    ///   • Refresh() sets _completedBadge active when IsCompleted = true.
    ///   • Refresh() sets _completedBadge inactive when IsCompleted = false.
    ///   • OnDisable unregisters Refresh from _onChallengeCompleted:
    ///       after disable, firing the event must not activate the badge.
    ///
    /// All tests run headless (no Canvas required).
    /// </summary>
    public class DailyChallengeControllerTests
    {
        private GameObject               _go;
        private DailyChallengeController _ctrl;
        private DailyChallengeSO         _dailyChallenge;
        private DailyChallengeConfig     _config;
        private VoidGameEvent            _onCompleted;
        private BonusConditionSO         _condition;

        // ── Reflection helper ─────────────────────────────────────────────────

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
            _dailyChallenge = ScriptableObject.CreateInstance<DailyChallengeSO>();
            _config         = ScriptableObject.CreateInstance<DailyChallengeConfig>();
            _onCompleted    = ScriptableObject.CreateInstance<VoidGameEvent>();
            _condition      = ScriptableObject.CreateInstance<BonusConditionSO>();

            SetField(_condition, "_conditionType",       BonusConditionType.NoDamageTaken);
            SetField(_condition, "_threshold",           0f);
            SetField(_condition, "_bonusAmount",         100);
            SetField(_condition, "_displayName",         "Perfect Shield");
            SetField(_condition, "_displayDescription",  "Win without taking any damage.");

            SetField(_config, "_challengePool",
                new List<BonusConditionSO> { _condition });
            SetField(_config, "_rewardMultiplier", 2f);

            _go   = new GameObject("DailyChallengeController");
            _go.SetActive(false); // inactive until fields are injected
            _ctrl = _go.AddComponent<DailyChallengeController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_dailyChallenge);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_onCompleted);
            Object.DestroyImmediate(_condition);
        }

        // ── OnEnable / OnDisable guards ───────────────────────────────────────

        [Test]
        public void OnEnable_NullDailyChallenge_DoesNotThrow()
        {
            // All fields null — OnEnable must not throw.
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        [Test]
        public void OnEnable_NullEventChannel_DoesNotThrow()
        {
            SetField(_ctrl, "_dailyChallenge", _dailyChallenge);
            // _onChallengeCompleted intentionally null
            Assert.DoesNotThrow(() => _go.SetActive(true));
        }

        [Test]
        public void OnDisable_NullEventChannel_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── Refresh guard paths ───────────────────────────────────────────────

        [Test]
        public void Refresh_NullDailyChallenge_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _ctrl.Refresh());
        }

        [Test]
        public void Refresh_NullCurrentChallenge_DoesNotThrow()
        {
            SetField(_ctrl, "_dailyChallenge", _dailyChallenge);
            // CurrentChallenge is null — no RefreshIfNeeded called
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _ctrl.Refresh());
        }

        [Test]
        public void Refresh_NullUIRefs_DoesNotThrow()
        {
            // Pre-set the challenge so Refresh() enters its body.
            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);

            SetField(_ctrl, "_dailyChallenge", _dailyChallenge);
            SetField(_ctrl, "_config",         _config);
            // All UI fields remain null — must not throw.
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _ctrl.Refresh());
        }

        // ── Refresh badge state ───────────────────────────────────────────────

        [Test]
        public void Refresh_CompletedChallenge_ShowsBadge()
        {
            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);
            _dailyChallenge.MarkCompleted();

            var badge = new GameObject("Badge");
            badge.SetActive(false);

            SetField(_ctrl, "_dailyChallenge",  _dailyChallenge);
            SetField(_ctrl, "_completedBadge",  badge);
            _go.SetActive(true);
            _ctrl.Refresh();

            Assert.IsTrue(badge.activeSelf,
                "Badge must be active when IsCompleted is true.");

            Object.DestroyImmediate(badge);
        }

        [Test]
        public void Refresh_NotCompleted_HidesBadge()
        {
            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);
            // IsCompleted stays false

            var badge = new GameObject("Badge");
            badge.SetActive(true); // start visible

            SetField(_ctrl, "_dailyChallenge",  _dailyChallenge);
            SetField(_ctrl, "_completedBadge",  badge);
            _go.SetActive(true);
            _ctrl.Refresh();

            Assert.IsFalse(badge.activeSelf,
                "Badge must be inactive when IsCompleted is false.");

            Object.DestroyImmediate(badge);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromCompletedEvent()
        {
            // Arrange: prime the challenge, wire both the SO event and the controller.
            SetField(_dailyChallenge, "_lastRefreshDate", "1900-01-01");
            _dailyChallenge.RefreshIfNeeded(_config);

            var badge = new GameObject("Badge");
            badge.SetActive(false);

            // Wire the same VoidGameEvent into both the SO and the controller.
            SetField(_dailyChallenge, "_onChallengeCompleted", _onCompleted);
            SetField(_ctrl, "_dailyChallenge",       _dailyChallenge);
            SetField(_ctrl, "_onChallengeCompleted", _onCompleted);
            SetField(_ctrl, "_completedBadge",       badge);

            _go.SetActive(true);  // OnEnable — registers Refresh on _onCompleted
            _go.SetActive(false); // OnDisable — unregisters Refresh

            // Act: completing the challenge fires _onCompleted.
            // After OnDisable, Refresh() must NOT be called, so badge stays inactive.
            _dailyChallenge.MarkCompleted();

            // Assert
            Assert.IsFalse(badge.activeSelf,
                "Refresh() must not be called after OnDisable unregisters the delegate.");

            Object.DestroyImmediate(badge);
        }
    }
}

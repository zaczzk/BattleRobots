using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchManager.HandleMatchStarted"/>.
    ///
    /// Covers:
    ///   • Null-guard: HandleMatchStarted bails out early (IsMatchRunning stays false)
    ///     when either HealthSO reference is missing.
    ///   • Happy path: IsMatchRunning transitions to true, TimeRemaining equals the
    ///     configured round duration.
    ///   • Side-effects: both HealthSOs are Reset() (CurrentHealth restored to MaxHealth)
    ///     before gameplay starts.
    ///   • Event broadcast: _onTimerUpdated FloatGameEvent fires with the initial
    ///     TimeRemaining value so the HUD can display it immediately.
    ///   • Re-entrancy: calling HandleMatchStarted a second time resets the timer to
    ///     the round duration rather than leaving stale state.
    ///
    /// <c>MatchManager</c> is a <c>MonoBehaviour</c>; a headless <c>GameObject</c> is
    /// created in SetUp and destroyed in TearDown.  Private serialised fields are
    /// injected via reflection — the same pattern used throughout this test suite.
    ///
    /// Note: <see cref="MatchManager.Update"/> and the death/timer win-condition paths
    /// require a running PhysX tick and are covered by Play-mode integration tests.
    /// </summary>
    public class MatchManagerTests
    {
        // ── Scene / MB objects ────────────────────────────────────────────────
        private GameObject    _go;
        private MatchManager  _manager;

        // ── ScriptableObjects ─────────────────────────────────────────────────
        private HealthSO _playerHealth;
        private HealthSO _enemyHealth;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go      = new GameObject("TestMatchManager");
            _manager = _go.AddComponent<MatchManager>();

            _playerHealth = ScriptableObject.CreateInstance<HealthSO>();
            _enemyHealth  = ScriptableObject.CreateInstance<HealthSO>();

            // Give each HealthSO a clean full-health state.
            _playerHealth.Reset();
            _enemyHealth.Reset();

            // Wire the two mandatory combatant health references.
            SetField(_manager, "_playerHealth", _playerHealth);
            SetField(_manager, "_enemyHealth",  _enemyHealth);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_playerHealth);
            Object.DestroyImmediate(_enemyHealth);
            _go            = null;
            _manager       = null;
            _playerHealth  = null;
            _enemyHealth   = null;
        }

        // ── Null-guard: missing HealthSO references ───────────────────────────

        [Test]
        public void HandleMatchStarted_BothHealthsNull_MatchDoesNotStart()
        {
            SetField(_manager, "_playerHealth", null);
            SetField(_manager, "_enemyHealth",  null);

            _manager.HandleMatchStarted();

            Assert.IsFalse(_manager.IsMatchRunning);
        }

        [Test]
        public void HandleMatchStarted_PlayerHealthNull_MatchDoesNotStart()
        {
            SetField(_manager, "_playerHealth", null);

            _manager.HandleMatchStarted();

            Assert.IsFalse(_manager.IsMatchRunning);
        }

        [Test]
        public void HandleMatchStarted_EnemyHealthNull_MatchDoesNotStart()
        {
            SetField(_manager, "_enemyHealth", null);

            _manager.HandleMatchStarted();

            Assert.IsFalse(_manager.IsMatchRunning);
        }

        // ── Happy path: state transitions ─────────────────────────────────────

        [Test]
        public void HandleMatchStarted_ValidHealths_SetsMatchRunningTrue()
        {
            _manager.HandleMatchStarted();

            Assert.IsTrue(_manager.IsMatchRunning);
        }

        [Test]
        public void HandleMatchStarted_ValidHealths_SetsTimeRemainingToRoundDuration()
        {
            // Override default (120s) so the assertion is explicit and independent
            // of the inspector default value.
            SetField(_manager, "_roundDuration", 90f);

            _manager.HandleMatchStarted();

            Assert.AreEqual(90f, _manager.TimeRemaining, 0.001f);
        }

        [Test]
        public void HandleMatchStarted_ValidHealths_ResetsPlayerHealth()
        {
            // Damage the player robot before the match starts.
            _playerHealth.ApplyDamage(40f);
            Assert.AreEqual(60f, _playerHealth.CurrentHealth, 0.001f);

            _manager.HandleMatchStarted();

            // Reset() should have been called, restoring health to 100.
            Assert.AreEqual(100f, _playerHealth.CurrentHealth, 0.001f);
        }

        [Test]
        public void HandleMatchStarted_ValidHealths_ResetsEnemyHealth()
        {
            _enemyHealth.ApplyDamage(70f);
            Assert.AreEqual(30f, _enemyHealth.CurrentHealth, 0.001f);

            _manager.HandleMatchStarted();

            Assert.AreEqual(100f, _enemyHealth.CurrentHealth, 0.001f);
        }

        // ── Timer broadcast on start ──────────────────────────────────────────

        [Test]
        public void HandleMatchStarted_BroadcastsInitialTimerValue()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            float received = -1f;
            timerEvent.RegisterCallback(v => received = v);

            SetField(_manager, "_onTimerUpdated", timerEvent);
            SetField(_manager, "_roundDuration",  60f);

            _manager.HandleMatchStarted();

            Assert.AreEqual(60f, received, 0.001f,
                "HandleMatchStarted must immediately raise _onTimerUpdated with the full round duration.");

            Object.DestroyImmediate(timerEvent);
        }

        // ── Re-entrancy: second call resets timer ─────────────────────────────

        [Test]
        public void HandleMatchStarted_CalledTwice_TimerResetsToRoundDuration()
        {
            SetField(_manager, "_roundDuration", 60f);
            _manager.HandleMatchStarted();

            // Simulate partial timer tick by overwriting _timeRemaining directly.
            // (Mimics Update() having run for a few seconds.)
            SetField(_manager, "_timeRemaining", 45f);
            Assert.AreEqual(45f, _manager.TimeRemaining, 0.001f);

            // Call again — should reset to the full duration.
            SetField(_manager, "_roundDuration", 90f);
            _manager.HandleMatchStarted();

            Assert.IsTrue(_manager.IsMatchRunning);
            Assert.AreEqual(90f, _manager.TimeRemaining, 0.001f);
        }
    }
}

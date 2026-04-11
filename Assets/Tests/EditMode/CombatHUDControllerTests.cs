using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CombatHUDController"/>.
    ///
    /// Covers:
    ///   • OnEnable with all null event channels → no throw.
    ///   • HandleMatchStarted (raised via VoidGameEvent):
    ///       null _hudRoot → no throw;
    ///       with HealthSOs assigned → primes health displays without throw.
    ///   • HandleMatchEnded (raised via VoidGameEvent):
    ///       null _hudRoot → no throw.
    ///   • HandleTimerUpdated (raised via FloatGameEvent):
    ///       null _timerText → no throw;
    ///       timer-dedup: same CeilToInt second → _lastDisplayedSeconds unchanged;
    ///       different second → _lastDisplayedSeconds updated.
    ///   • HandlePlayerHealthChanged / HandleEnemyHealthChanged
    ///       (raised via FloatGameEvent): null HealthSO → no throw.
    ///   • OnDisable unregisters all five delegates — raising events after
    ///       disable must not throw (callbacks safely removed).
    ///
    /// Private handlers are exercised indirectly by raising the corresponding
    /// SO event channels — the same approach used in MatchFlowControllerTests.
    /// The internal timer-dedup field <c>_lastDisplayedSeconds</c> is read via
    /// reflection to verify the optimisation without requiring a Text component.
    /// All tests run headless; no uGUI scene objects are needed.
    /// </summary>
    public class CombatHUDControllerTests
    {
        // ── Scene / MB objects ────────────────────────────────────────────────
        private GameObject            _go;
        private CombatHUDController   _ctrl;

        // ── Managed event channel SOs (created per test) ──────────────────────
        // (tests that need them create them inline and destroy at end of test)

        // ── Reflection helpers ────────────────────────────────────────────────

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

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("CombatHUDController");
            _go.SetActive(false); // inactive so Awake/OnEnable don't fire during setup
            _ctrl = _go.AddComponent<CombatHUDController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ── OnEnable — all null event channels ───────────────────────────────

        [Test]
        public void Enable_AllNullEventChannels_DoesNotThrow()
        {
            // No SO fields set; ?. guards in OnEnable must silently skip all registrations.
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "Activating CombatHUDController with all null event channels must not throw.");
        }

        // ── HandleMatchStarted — via VoidGameEvent ────────────────────────────

        [Test]
        public void MatchStarted_Raise_NullHudRoot_DoesNotThrow()
        {
            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onMatchStarted", matchStarted);

            _go.SetActive(true); // OnEnable registers handler

            Assert.DoesNotThrow(() => matchStarted.Raise(),
                "HandleMatchStarted with null _hudRoot must not throw.");

            Object.DestroyImmediate(matchStarted);
        }

        [Test]
        public void MatchStarted_WithHealthSOs_PrimesDisplays_DoesNotThrow()
        {
            var matchStarted  = ScriptableObject.CreateInstance<VoidGameEvent>();
            var playerHealth  = ScriptableObject.CreateInstance<HealthSO>();
            var enemyHealth   = ScriptableObject.CreateInstance<HealthSO>();

            SetField(_ctrl, "_onMatchStarted", matchStarted);
            SetField(_ctrl, "_playerHealth",   playerHealth);
            SetField(_ctrl, "_enemyHealth",    enemyHealth);

            _go.SetActive(true);

            // HandleMatchStarted reads playerHealth.CurrentHealth and enemyHealth.CurrentHealth
            // to prime the sliders; both slider refs are null, so UpdateHealthWidgets no-ops safely.
            Assert.DoesNotThrow(() => matchStarted.Raise(),
                "HandleMatchStarted with HealthSOs assigned but null slider refs must not throw.");

            Object.DestroyImmediate(matchStarted);
            Object.DestroyImmediate(playerHealth);
            Object.DestroyImmediate(enemyHealth);
        }

        // ── HandleMatchEnded — via VoidGameEvent ──────────────────────────────

        [Test]
        public void MatchEnded_Raise_NullHudRoot_DoesNotThrow()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onMatchEnded", matchEnded);

            _go.SetActive(true);

            Assert.DoesNotThrow(() => matchEnded.Raise(),
                "HandleMatchEnded with null _hudRoot must not throw.");

            Object.DestroyImmediate(matchEnded);
        }

        // ── HandleTimerUpdated — null timer text ──────────────────────────────

        [Test]
        public void TimerUpdated_Raise_NullTimerText_DoesNotThrow()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onTimerUpdated", timerEvent);

            _go.SetActive(true);

            // _timerText is null → HandleTimerUpdated must guard and not throw.
            Assert.DoesNotThrow(() => timerEvent.Raise(90f),
                "HandleTimerUpdated with null _timerText must not throw.");

            Object.DestroyImmediate(timerEvent);
        }

        // ── Timer dedup — _lastDisplayedSeconds ──────────────────────────────

        [Test]
        public void TimerUpdated_SameSecond_DeduplicatesLastDisplayedSeconds()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            _go.SetActive(true);

            // First raise: 60.0f → CeilToInt(60.0f) = 60. _lastDisplayedSeconds becomes 60.
            timerEvent.Raise(60.0f);
            int afterFirst = GetField<int>(_ctrl, "_lastDisplayedSeconds");
            Assert.AreEqual(60, afterFirst,
                "After first raise with 60.0f, _lastDisplayedSeconds should be 60.");

            // Second raise with same second: dedup guard must prevent any change.
            timerEvent.Raise(60.0f);
            int afterSecond = GetField<int>(_ctrl, "_lastDisplayedSeconds");
            Assert.AreEqual(60, afterSecond,
                "Same second should be deduped — _lastDisplayedSeconds must remain 60.");

            Object.DestroyImmediate(timerEvent);
        }

        [Test]
        public void TimerUpdated_DifferentSecond_UpdatesLastDisplayedSeconds()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            _go.SetActive(true);

            timerEvent.Raise(60.0f); // → _lastDisplayedSeconds = 60
            timerEvent.Raise(59.0f); // different second → must update to 59

            int afterChange = GetField<int>(_ctrl, "_lastDisplayedSeconds");
            Assert.AreEqual(59, afterChange,
                "A new integer second should update _lastDisplayedSeconds from 60 to 59.");

            Object.DestroyImmediate(timerEvent);
        }

        // ── HandlePlayerHealthChanged / EnemyHealthChanged ────────────────────

        [Test]
        public void PlayerHealthChanged_Raise_NullHealthSO_DoesNotThrow()
        {
            var healthEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onPlayerHealthChanged", healthEvent);
            // _playerHealth left null → UpdateHealthWidgets uses 1f as max → no crash.
            _go.SetActive(true);

            Assert.DoesNotThrow(() => healthEvent.Raise(50f),
                "HandlePlayerHealthChanged with null _playerHealth SO must not throw.");

            Object.DestroyImmediate(healthEvent);
        }

        [Test]
        public void EnemyHealthChanged_Raise_NullHealthSO_DoesNotThrow()
        {
            var healthEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onEnemyHealthChanged", healthEvent);
            _go.SetActive(true);

            Assert.DoesNotThrow(() => healthEvent.Raise(75f),
                "HandleEnemyHealthChanged with null _enemyHealth SO must not throw.");

            Object.DestroyImmediate(healthEvent);
        }

        // ── OnDisable — unregisters all delegates ─────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromAllChannels_DoesNotThrow()
        {
            // Wire all five event channels, enable the MB, disable it, then raise all
            // events.  The MB's five handlers must no longer be registered; no crash.
            var matchStarted    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var matchEnded      = ScriptableObject.CreateInstance<VoidGameEvent>();
            var timerUpdated    = ScriptableObject.CreateInstance<FloatGameEvent>();
            var playerHealth    = ScriptableObject.CreateInstance<FloatGameEvent>();
            var enemyHealth     = ScriptableObject.CreateInstance<FloatGameEvent>();

            SetField(_ctrl, "_onMatchStarted",          matchStarted);
            SetField(_ctrl, "_onMatchEnded",            matchEnded);
            SetField(_ctrl, "_onTimerUpdated",          timerUpdated);
            SetField(_ctrl, "_onPlayerHealthChanged",   playerHealth);
            SetField(_ctrl, "_onEnemyHealthChanged",    enemyHealth);

            _go.SetActive(true);   // OnEnable registers all 5 handlers.
            _go.SetActive(false);  // OnDisable must unregister all 5.

            // Raise every channel — if any handler is still registered and tries to
            // access a destroyed resource it would throw; we assert no throw.
            Assert.DoesNotThrow(() =>
            {
                matchStarted.Raise();
                matchEnded.Raise();
                timerUpdated.Raise(30f);
                playerHealth.Raise(80f);
                enemyHealth.Raise(40f);
            }, "Raising all event channels after OnDisable must not throw.");

            Object.DestroyImmediate(matchStarted);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(timerUpdated);
            Object.DestroyImmediate(playerHealth);
            Object.DestroyImmediate(enemyHealth);
        }
    }
}

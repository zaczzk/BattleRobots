using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ChallengeProgressHUDController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all null refs → DoesNotThrow.
    ///   • HandleMatchStarted sets _matchRunning = true (via VoidGameEvent raise).
    ///   • HandleMatchEnded sets _matchRunning = false (via VoidGameEvent raise).
    ///   • HandleTimerUpdated guard: does nothing when _matchRunning = false.
    ///   • HandleTimerUpdated captures round duration on first tick.
    ///   • HandleTimerUpdated dedup: same integer second → no second Refresh call
    ///     (_lastElapsedInt unchanged).
    ///   • BuildProgressText: null condition → empty string.
    ///   • BuildProgressText: NoDamageTaken below threshold → "on track" format.
    ///   • BuildProgressText: NoDamageTaken above threshold → "FAILED" format.
    ///   • BuildProgressText: WonUnderDuration → "elapsed / threshold" format.
    ///   • BuildProgressText: DamageDealtExceeds → "done / target" format.
    ///   • BuildProgressText: DamageEfficiency zero total → "0% / X%" format.
    ///   • BuildProgressText: DamageEfficiency with damage → correct percentage.
    ///   • OnDisable unregisters all three delegates — raising events after
    ///     disable must not throw.
    ///
    /// All tests run headless; no uGUI scene objects are required.
    /// </summary>
    public class ChallengeProgressHUDControllerTests
    {
        // ── Scene / MB objects ────────────────────────────────────────────────

        private GameObject                    _go;
        private ChallengeProgressHUDController _ctrl;

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

        // ── Helper: create a BonusConditionSO with reflection ─────────────────

        private static BonusConditionSO MakeCondition(
            BonusConditionType type, float threshold, int bonus = 50,
            string name = "Test Condition")
        {
            var so = ScriptableObject.CreateInstance<BonusConditionSO>();
            SetField(so, "_conditionType", type);
            SetField(so, "_threshold",     threshold);
            SetField(so, "_bonusAmount",   bonus);
            SetField(so, "_displayName",   name);
            return so;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("ChallengeProgressHUD");
            _go.SetActive(false); // inactive so Awake/OnEnable don't fire during setup
            _ctrl = _go.AddComponent<ChallengeProgressHUDController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ── 1. OnEnable with all null refs ────────────────────────────────────

        [Test]
        public void Enable_AllNullRefs_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "Activating with all null refs must not throw.");
        }

        // ── 2. OnDisable with all null refs ───────────────────────────────────

        [Test]
        public void Disable_AllNullRefs_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false),
                "Disabling with all null refs must not throw.");
        }

        // ── 3. HandleMatchStarted sets _matchRunning = true ───────────────────

        [Test]
        public void HandleMatchStarted_SetsMatchRunning_True()
        {
            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onMatchStarted", matchStarted);
            _go.SetActive(true);

            matchStarted.Raise();

            bool running = GetField<bool>(_ctrl, "_matchRunning");
            Assert.IsTrue(running, "_matchRunning must be true after HandleMatchStarted.");

            Object.DestroyImmediate(matchStarted);
        }

        // ── 4. HandleMatchEnded sets _matchRunning = false ────────────────────

        [Test]
        public void HandleMatchEnded_SetsMatchRunning_False()
        {
            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onMatchStarted", matchStarted);
            SetField(_ctrl, "_onMatchEnded",   matchEnded);
            _go.SetActive(true);

            matchStarted.Raise(); // _matchRunning = true
            matchEnded.Raise();   // _matchRunning = false

            bool running = GetField<bool>(_ctrl, "_matchRunning");
            Assert.IsFalse(running, "_matchRunning must be false after HandleMatchEnded.");

            Object.DestroyImmediate(matchStarted);
            Object.DestroyImmediate(matchEnded);
        }

        // ── 5. HandleTimerUpdated guard: does nothing when not running ─────────

        [Test]
        public void HandleTimerUpdated_WhenMatchNotRunning_DoesNotChangeElapsedInt()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            _go.SetActive(true);

            // _matchRunning is false by default; firing the timer must early-return.
            timerEvent.Raise(100f);

            int elapsed = GetField<int>(_ctrl, "_lastElapsedInt");
            Assert.AreEqual(-1, elapsed,
                "_lastElapsedInt must remain -1 when _matchRunning is false.");

            Object.DestroyImmediate(timerEvent);
        }

        // ── 6. HandleTimerUpdated captures round duration on first tick ────────

        [Test]
        public void HandleTimerUpdated_CapturesRoundDuration_OnFirstTick()
        {
            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            var timerEvent   = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onMatchStarted", matchStarted);
            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            _go.SetActive(true);

            matchStarted.Raise();      // _matchRunning = true, _durationCaptured = false
            timerEvent.Raise(120f);    // first tick: _roundDuration should be set to 120

            float roundDuration = GetField<float>(_ctrl, "_roundDuration");
            bool  captured      = GetField<bool>(_ctrl,  "_durationCaptured");

            Assert.AreEqual(120f, roundDuration, 0.001f,
                "_roundDuration must be captured from the first timer tick.");
            Assert.IsTrue(captured,
                "_durationCaptured must be true after the first timer tick.");

            Object.DestroyImmediate(matchStarted);
            Object.DestroyImmediate(timerEvent);
        }

        // ── 7. HandleTimerUpdated dedup: same integer second ───────────────────

        [Test]
        public void HandleTimerUpdated_SameIntegerSecond_DeduplicatesElapsedInt()
        {
            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            var timerEvent   = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onMatchStarted", matchStarted);
            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            _go.SetActive(true);

            matchStarted.Raise();   // prime: _matchRunning = true
            timerEvent.Raise(120f); // first tick: _roundDuration=120, elapsed=0 → _lastElapsedInt=0
            timerEvent.Raise(119.7f); // elapsed≈0.3 → FloorToInt=0 → same second, dedup

            int elapsedInt = GetField<int>(_ctrl, "_lastElapsedInt");
            Assert.AreEqual(0, elapsedInt,
                "_lastElapsedInt must remain 0 when elapsed is still < 1 integer second.");

            Object.DestroyImmediate(matchStarted);
            Object.DestroyImmediate(timerEvent);
        }

        // ── 8. BuildProgressText: null condition ──────────────────────────────

        [Test]
        public void BuildProgressText_NullCondition_ReturnsEmpty()
        {
            string result = ChallengeProgressHUDController.BuildProgressText(
                null, 15f, 50f, 10f);

            Assert.AreEqual("", result,
                "BuildProgressText with null condition must return empty string.");
        }

        // ── 9. BuildProgressText: NoDamageTaken, below threshold ──────────────

        [Test]
        public void BuildProgressText_NoDamageTaken_BelowThreshold_ShowsOnTrack()
        {
            var condition = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 5f);

            // damageTaken=3f ≤ threshold=5f → on-track path
            string result = ChallengeProgressHUDController.BuildProgressText(
                condition, 10f, 40f, 3f);

            StringAssert.Contains("No Damage", result);
            StringAssert.DoesNotContain("FAILED", result);

            Object.DestroyImmediate(condition);
        }

        // ── 10. BuildProgressText: NoDamageTaken, above threshold ─────────────

        [Test]
        public void BuildProgressText_NoDamageTaken_AboveThreshold_ShowsFailed()
        {
            var condition = MakeCondition(BonusConditionType.NoDamageTaken, threshold: 0f);

            // damageTaken=25f > threshold=0f → FAILED path
            string result = ChallengeProgressHUDController.BuildProgressText(
                condition, 30f, 60f, 25f);

            StringAssert.Contains("FAILED", result);
            StringAssert.Contains("25", result);

            Object.DestroyImmediate(condition);
        }

        // ── 11. BuildProgressText: WonUnderDuration ───────────────────────────

        [Test]
        public void BuildProgressText_WonUnderDuration_ShowsElapsedAndThreshold()
        {
            var condition = MakeCondition(BonusConditionType.WonUnderDuration, threshold: 30f);

            string result = ChallengeProgressHUDController.BuildProgressText(
                condition, 12f, 0f, 0f);

            // Expected: "Speed Run: 12s / 30s"
            StringAssert.Contains("12", result);
            StringAssert.Contains("30", result);
            StringAssert.Contains("Speed Run", result);

            Object.DestroyImmediate(condition);
        }

        // ── 12. BuildProgressText: DamageDealtExceeds ─────────────────────────

        [Test]
        public void BuildProgressText_DamageDealtExceeds_ShowsDamageAndTarget()
        {
            var condition = MakeCondition(BonusConditionType.DamageDealtExceeds, threshold: 80f);

            string result = ChallengeProgressHUDController.BuildProgressText(
                condition, 20f, 47f, 10f);

            // Expected: "Damage: 47 / 80"
            StringAssert.Contains("47", result);
            StringAssert.Contains("80", result);
            StringAssert.Contains("Damage", result);

            Object.DestroyImmediate(condition);
        }

        // ── 13. BuildProgressText: DamageEfficiency, zero total ───────────────

        [Test]
        public void BuildProgressText_DamageEfficiency_ZeroTotal_ShowsZeroPercent()
        {
            var condition = MakeCondition(BonusConditionType.DamageEfficiency, threshold: 0.8f);

            // No damage dealt or taken → efficiency = 0
            string result = ChallengeProgressHUDController.BuildProgressText(
                condition, 5f, 0f, 0f);

            // Expected: "Efficiency: 0% / 80%"
            StringAssert.Contains("Efficiency", result);
            StringAssert.Contains("0%", result);
            StringAssert.Contains("80%", result);

            Object.DestroyImmediate(condition);
        }

        // ── 14. BuildProgressText: DamageEfficiency, with damage ──────────────

        [Test]
        public void BuildProgressText_DamageEfficiency_WithDamage_ShowsCorrectPercent()
        {
            var condition = MakeCondition(BonusConditionType.DamageEfficiency, threshold: 0.8f);

            // damageDone=60, damageTaken=40 → efficiency = 60/100 = 60%
            string result = ChallengeProgressHUDController.BuildProgressText(
                condition, 20f, 60f, 40f);

            StringAssert.Contains("60%", result);
            StringAssert.Contains("80%", result);

            Object.DestroyImmediate(condition);
        }

        // ── 15. OnDisable unregisters all channels ────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromAllChannels_DoesNotThrow()
        {
            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            var matchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            var timerUpdated = ScriptableObject.CreateInstance<FloatGameEvent>();

            SetField(_ctrl, "_onMatchStarted", matchStarted);
            SetField(_ctrl, "_onMatchEnded",   matchEnded);
            SetField(_ctrl, "_onTimerUpdated", timerUpdated);

            _go.SetActive(true);  // OnEnable — registers all 3 handlers
            _go.SetActive(false); // OnDisable — must unregister all 3

            // Raising channels after disable must not invoke any handler (no crash).
            Assert.DoesNotThrow(() =>
            {
                matchStarted.Raise();
                matchEnded.Raise();
                timerUpdated.Raise(60f);
            }, "Raising all channels after OnDisable must not throw.");

            Object.DestroyImmediate(matchStarted);
            Object.DestroyImmediate(matchEnded);
            Object.DestroyImmediate(timerUpdated);
        }
    }
}

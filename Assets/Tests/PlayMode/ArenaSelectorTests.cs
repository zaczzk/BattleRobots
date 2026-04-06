using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// PlayMode tests for <see cref="ArenaSelectionSO"/> and the
    /// <see cref="MatchManager.ActiveArena"/> routing logic.
    ///
    /// Pure-SO tests (ArenaSelectionSO behaviour) use plain [Test] since they
    /// require no frame advance.  Tests that exercise MatchManager's Update loop
    /// use [UnityTest] with yield return null so the win-condition can fire.
    ///
    /// Field injection uses reflection — same pattern as MatchManagerTests.
    /// </summary>
    [TestFixture]
    public sealed class ArenaSelectorTests
    {
        // ── Reflection helpers ────────────────────────────────────────────────

        private static readonly BindingFlags k_private =
            BindingFlags.NonPublic | BindingFlags.Instance;

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, k_private);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Per-test fixtures ─────────────────────────────────────────────────

        private ArenaSelectionSO _selection;
        private ArenaConfig      _arenaA;   // used as fallback / selected in routing tests
        private ArenaConfig      _arenaB;   // secondary arena
        private GameObject       _mmGo;
        private MatchManager     _mm;
        private HealthSO         _playerHealth;
        private HealthSO         _opponentHealth;
        private PlayerWallet     _wallet;

        [SetUp]
        public void SetUp()
        {
            _selection = ScriptableObject.CreateInstance<ArenaSelectionSO>();

            _arenaA = ScriptableObject.CreateInstance<ArenaConfig>();
            SetField(_arenaA, "_arenaName",        "ArenaA");
            SetField(_arenaA, "_arenaIndex",        0);
            SetField(_arenaA, "_timeLimitSeconds",  0f);
            SetField(_arenaA, "_winBonusCurrency",  50);

            _arenaB = ScriptableObject.CreateInstance<ArenaConfig>();
            SetField(_arenaB, "_arenaName",        "ArenaB");
            SetField(_arenaB, "_arenaIndex",        1);
            SetField(_arenaB, "_timeLimitSeconds",  0f);
            SetField(_arenaB, "_winBonusCurrency",  150);

            _playerHealth   = ScriptableObject.CreateInstance<HealthSO>();
            _opponentHealth = ScriptableObject.CreateInstance<HealthSO>();

            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(_wallet, "_startingBalance", 500);
            _wallet.Reset();

            _mmGo = new GameObject("MatchManagerArenaTest");
            _mm   = _mmGo.AddComponent<MatchManager>();

            // Wire MatchManager: fallback arena = _arenaA, selection SO attached.
            SetField(_mm, "_arenaConfig",           _arenaA);
            SetField(_mm, "_arenaSelection",        _selection);
            SetField(_mm, "_robotHealthSOs",        new HealthSO[] { _playerHealth, _opponentHealth });
            SetField(_mm, "_wallet",                _wallet);
            SetField(_mm, "_baseWinReward",         100);
            // Event channels left null — MatchManager guards with ?. operator.
        }

        [TearDown]
        public void TearDown()
        {
            if (_mmGo != null) Object.Destroy(_mmGo);
            Object.DestroyImmediate(_selection);
            Object.DestroyImmediate(_arenaA);
            Object.DestroyImmediate(_arenaB);
            Object.DestroyImmediate(_playerHealth);
            Object.DestroyImmediate(_opponentHealth);
            Object.DestroyImmediate(_wallet);
        }

        // ── ArenaSelectionSO: isolated behaviour ──────────────────────────────

        /// <summary>Fresh ArenaSelectionSO has no selection.</summary>
        [Test]
        public void ArenaSelectionSO_Default_HasNoSelection()
        {
            Assert.IsFalse(_selection.HasSelection,
                "A new ArenaSelectionSO must not report HasSelection = true.");
            Assert.IsNull(_selection.SelectedArena,
                "SelectedArena must be null before Select() is called.");
        }

        /// <summary>Select(arena) stores the arena and sets HasSelection.</summary>
        [Test]
        public void Select_WithValidArena_SetsSelectionAndHasSelection()
        {
            _selection.Select(_arenaA);

            Assert.IsTrue(_selection.HasSelection,
                "HasSelection must be true after Select(arenaA).");
            Assert.AreSame(_arenaA, _selection.SelectedArena,
                "SelectedArena must equal the arena passed to Select().");
        }

        /// <summary>Select(null) is silently ignored; HasSelection stays false.</summary>
        [Test]
        public void Select_WithNullArena_IsIgnored()
        {
            _selection.Select(null);

            Assert.IsFalse(_selection.HasSelection,
                "Select(null) must not set HasSelection.");
            Assert.IsNull(_selection.SelectedArena,
                "SelectedArena must remain null after Select(null).");
        }

        /// <summary>Reset() clears a previously stored selection.</summary>
        [Test]
        public void Reset_AfterSelect_ClearsSelection()
        {
            _selection.Select(_arenaA);
            Assert.IsTrue(_selection.HasSelection, "Pre-condition: should have selection.");

            _selection.Reset();

            Assert.IsFalse(_selection.HasSelection,
                "HasSelection must be false after Reset().");
            Assert.IsNull(_selection.SelectedArena,
                "SelectedArena must be null after Reset().");
        }

        /// <summary>Reset() on an already-empty selection does not throw.</summary>
        [Test]
        public void Reset_WhenAlreadyEmpty_IsNoOp()
        {
            Assert.DoesNotThrow(() => _selection.Reset(),
                "Reset() on an empty ArenaSelectionSO must not throw.");
            Assert.IsFalse(_selection.HasSelection);
        }

        /// <summary>Calling Select() a second time replaces the previous arena.</summary>
        [Test]
        public void Select_CalledTwice_ReplacesFirstArena()
        {
            _selection.Select(_arenaA);
            _selection.Select(_arenaB);

            Assert.AreSame(_arenaB, _selection.SelectedArena,
                "A second Select() call must overwrite the first selection.");
        }

        // ── MatchManager.ActiveArena routing ──────────────────────────────────

        /// <summary>
        /// When ArenaSelectionSO.HasSelection is true, MatchManager uses the
        /// selected arena's WinBonusCurrency rather than the fallback _arenaConfig.
        /// Verified via wallet balance delta after a win.
        ///
        /// Setup:  _arenaA (fallback, bonus 50) · _arenaB selected (bonus 150).
        /// Expected reward = _baseWinReward(100) + _arenaB.WinBonusCurrency(150) = 250.
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveArena_WithSelection_UsesSelectedArena()
        {
            // Select arenaB (winBonus = 150) — overrides fallback arenaA (winBonus = 50).
            _selection.Select(_arenaB);

            int balanceBefore = _wallet.Balance;
            _mm.StartMatch();
            yield return null;

            // Kill opponent → triggers a player win.
            _opponentHealth.TakeDamage(_opponentHealth.MaxHp);
            yield return null; // allow Update to call EndMatch

            int expectedReward = 100 + 150; // _baseWinReward + _arenaB.WinBonusCurrency
            Assert.AreEqual(balanceBefore + expectedReward, _wallet.Balance,
                "MatchManager must use the ArenaSelectionSO's SelectedArena (arenaB, bonus 150).");
        }

        /// <summary>
        /// When ArenaSelectionSO.HasSelection is false (after Reset()), MatchManager
        /// falls back to the Inspector-wired _arenaConfig.
        ///
        /// Setup:  _arenaA (fallback, bonus 50) · _arenaB selected then reset.
        /// Expected reward = _baseWinReward(100) + _arenaA.WinBonusCurrency(50) = 150.
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveArena_WithoutSelection_UsesFallbackArenaConfig()
        {
            // Select arenaB first, then reset — HasSelection becomes false.
            _selection.Select(_arenaB);
            _selection.Reset();
            Assert.IsFalse(_selection.HasSelection, "Pre-condition: selection must be cleared.");

            int balanceBefore = _wallet.Balance;
            _mm.StartMatch();
            yield return null;

            _opponentHealth.TakeDamage(_opponentHealth.MaxHp);
            yield return null;

            int expectedReward = 100 + 50; // _baseWinReward + _arenaA.WinBonusCurrency
            Assert.AreEqual(balanceBefore + expectedReward, _wallet.Balance,
                "MatchManager must fall back to _arenaConfig (arenaA, bonus 50) when HasSelection is false.");
        }

        /// <summary>
        /// When _arenaSelection is null (not wired), MatchManager uses _arenaConfig.
        /// Exercises the null guard in the ActiveArena computed property.
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveArena_WhenSelectionSONotWired_UsesFallbackArenaConfig()
        {
            // Remove the ArenaSelectionSO wiring entirely.
            SetField(_mm, "_arenaSelection", null);

            int balanceBefore = _wallet.Balance;
            _mm.StartMatch();
            yield return null;

            _opponentHealth.TakeDamage(_opponentHealth.MaxHp);
            yield return null;

            int expectedReward = 100 + 50; // _baseWinReward + _arenaA.WinBonusCurrency
            Assert.AreEqual(balanceBefore + expectedReward, _wallet.Balance,
                "MatchManager must use _arenaConfig when _arenaSelection is null.");
        }

        /// <summary>
        /// Switching selection mid-match does not affect the already-started match.
        /// ActiveArena is evaluated in each Update tick — the new selection takes
        /// effect for time-limit checks but win-bonus was not yet captured.
        /// This test verifies the win reward reflects the arena selected at EndMatch time.
        /// </summary>
        [UnityTest]
        public IEnumerator ActiveArena_SelectionChangedMidMatch_RewardsUseCurrentSelectionAtMatchEnd()
        {
            // Start with arenaA selected (bonus 50).
            _selection.Select(_arenaA);

            int balanceBefore = _wallet.Balance;
            _mm.StartMatch();
            yield return null;

            // Switch to arenaB (bonus 150) while match is active.
            _selection.Select(_arenaB);
            yield return null;

            // End match by killing opponent — should use arenaB's bonus.
            _opponentHealth.TakeDamage(_opponentHealth.MaxHp);
            yield return null;

            int expectedReward = 100 + 150; // _baseWinReward + _arenaB.WinBonusCurrency
            Assert.AreEqual(balanceBefore + expectedReward, _wallet.Balance,
                "Win reward must reflect the arena active at the moment EndMatch is called.");
        }
    }
}

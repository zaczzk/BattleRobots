using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// PlayMode smoke-tests for <see cref="MatchManager"/>.
    ///
    /// These run in Play mode so <see cref="MonoBehaviour.Update"/> and
    /// <see cref="Time.deltaTime"/> advance naturally across yield points.
    ///
    /// All GameObjects and SO instances created in SetUp are destroyed in TearDown
    /// to keep tests hermetic.
    ///
    /// Field injection uses reflection (private serialized fields cannot be set
    /// through the public API from a separate assembly without InternalsVisibleTo).
    /// </summary>
    [TestFixture]
    public sealed class MatchManagerTests
    {
        // ── Per-test fixtures ─────────────────────────────────────────────────

        private GameObject   _mmGo;
        private MatchManager _mm;
        private HealthSO     _playerHealth;
        private HealthSO     _opponentHealth;
        private ArenaConfig  _arena;
        private PlayerWallet _wallet;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static readonly BindingFlags k_private =
            BindingFlags.NonPublic | BindingFlags.Instance;

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, k_private);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void SetField<TOwner>(object target, string fieldName, object value)
        {
            FieldInfo fi = typeof(TOwner).GetField(fieldName, k_private);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {typeof(TOwner).Name}.");
            fi.SetValue(target, value);
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // HealthSOs
            _playerHealth   = ScriptableObject.CreateInstance<HealthSO>();
            _opponentHealth = ScriptableObject.CreateInstance<HealthSO>();

            // ArenaConfig (no time limit by default; override per-test)
            _arena = ScriptableObject.CreateInstance<ArenaConfig>();
            SetField(_arena, "_arenaName",         "TestArena");
            SetField(_arena, "_arenaIndex",         0);
            SetField(_arena, "_timeLimitSeconds",   0f);   // unlimited
            SetField(_arena, "_winBonusCurrency",   50);

            // PlayerWallet
            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(_wallet, "_startingBalance", 500);
            _wallet.Reset();

            // MatchManager MonoBehaviour
            _mmGo = new GameObject("MatchManagerTest");
            _mm   = _mmGo.AddComponent<MatchManager>();

            // Wire serialized fields via reflection
            SetField(_mm, "_arenaConfig",           _arena);
            SetField(_mm, "_robotHealthSOs",        new HealthSO[] { _playerHealth, _opponentHealth });
            SetField(_mm, "_wallet",                _wallet);
            SetField(_mm, "_baseWinReward",         200);
            // Event channels left null — MatchManager guards with ?. operator.
        }

        [TearDown]
        public void TearDown()
        {
            if (_mmGo != null) Object.Destroy(_mmGo);
            Object.DestroyImmediate(_playerHealth);
            Object.DestroyImmediate(_opponentHealth);
            Object.DestroyImmediate(_arena);
            Object.DestroyImmediate(_wallet);
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        /// <summary>StartMatch transitions IsMatchActive to true.</summary>
        [UnityTest]
        public IEnumerator StartMatch_SetsIsMatchActive()
        {
            Assert.IsFalse(_mm.IsMatchActive, "Should be inactive before StartMatch.");
            _mm.StartMatch();
            yield return null; // one frame
            Assert.IsTrue(_mm.IsMatchActive, "IsMatchActive must be true after StartMatch.");
        }

        /// <summary>StartMatch initialises HealthSOs to their configured MaxHp.</summary>
        [UnityTest]
        public IEnumerator StartMatch_InitialisesHealthSOs()
        {
            _mm.StartMatch();
            yield return null;
            Assert.AreEqual(_playerHealth.MaxHp,   _playerHealth.CurrentHp,   0.001f,
                "Player HealthSO must be at MaxHp after StartMatch.");
            Assert.AreEqual(_opponentHealth.MaxHp, _opponentHealth.CurrentHp, 0.001f,
                "Opponent HealthSO must be at MaxHp after StartMatch.");
        }

        /// <summary>Calling StartMatch a second time while active is a no-op.</summary>
        [UnityTest]
        public IEnumerator StartMatch_WhenAlreadyActive_IsNoOp()
        {
            _mm.StartMatch();
            yield return null;
            float elapsed = _mm.ElapsedSeconds;

            // Damage player, then try to restart — HP should not be reset.
            _playerHealth.TakeDamage(20f);
            _mm.StartMatch(); // should log warning and return early
            yield return null;

            Assert.AreEqual(80f, _playerHealth.CurrentHp, 0.001f,
                "Second StartMatch call must not re-initialise HealthSOs.");
        }

        /// <summary>Opponent death ends the match with a player win.</summary>
        [UnityTest]
        public IEnumerator OpponentDeath_EndsMatch_AsPlayerWin()
        {
            _mm.StartMatch();
            yield return null;

            _opponentHealth.TakeDamage(_opponentHealth.MaxHp); // kill opponent
            yield return null; // let Update run

            Assert.IsFalse(_mm.IsMatchActive,
                "IsMatchActive must be false after opponent is killed.");
        }

        /// <summary>Player death ends the match with a loss.</summary>
        [UnityTest]
        public IEnumerator PlayerDeath_EndsMatch_AsLoss()
        {
            _mm.StartMatch();
            yield return null;

            _playerHealth.TakeDamage(_playerHealth.MaxHp); // kill player
            yield return null;

            Assert.IsFalse(_mm.IsMatchActive,
                "IsMatchActive must be false after player is killed.");
        }

        /// <summary>Player win grants the base reward + arena bonus to the wallet.</summary>
        [UnityTest]
        public IEnumerator PlayerWin_AddsCurrencyToWallet()
        {
            int expectedReward = 200 + 50; // _baseWinReward + _winBonusCurrency
            int balanceBefore  = _wallet.Balance;

            _mm.StartMatch();
            yield return null;

            _opponentHealth.TakeDamage(_opponentHealth.MaxHp);
            yield return null; // EndMatch runs

            Assert.AreEqual(
                balanceBefore + expectedReward,
                _wallet.Balance,
                $"Wallet should increase by {expectedReward} on win.");
        }

        /// <summary>Player loss does not change the wallet balance.</summary>
        [UnityTest]
        public IEnumerator PlayerLoss_DoesNotChangWalletBalance()
        {
            int balanceBefore = _wallet.Balance;

            _mm.StartMatch();
            yield return null;

            _playerHealth.TakeDamage(_playerHealth.MaxHp);
            yield return null;

            Assert.AreEqual(balanceBefore, _wallet.Balance,
                "Wallet balance must not change on loss.");
        }

        /// <summary>
        /// Time-limit expiry ends the match (treated as a loss for economy).
        /// Uses a 0.05 s limit so a single frame almost certainly exceeds it.
        /// </summary>
        [UnityTest]
        public IEnumerator TimeLimit_Expires_EndsMatch()
        {
            // Override to a very short time limit.
            SetField(_arena, "_timeLimitSeconds", 0.05f);

            _mm.StartMatch();

            // Wait up to 10 frames for Update to fire and detect expiry.
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                if (!_mm.IsMatchActive) break;
            }

            Assert.IsFalse(_mm.IsMatchActive,
                "Match must end when the time limit is exceeded.");
        }

        /// <summary>AbortMatch deactivates the match without writing a record.</summary>
        [UnityTest]
        public IEnumerator AbortMatch_DeactivatesMatch()
        {
            _mm.StartMatch();
            yield return null;
            Assert.IsTrue(_mm.IsMatchActive);

            _mm.AbortMatch();
            yield return null;

            Assert.IsFalse(_mm.IsMatchActive, "IsMatchActive must be false after AbortMatch.");
        }

        /// <summary>ElapsedSeconds advances while the match is active.</summary>
        [UnityTest]
        public IEnumerator ElapsedSeconds_AdvancesWhileMatchActive()
        {
            _mm.StartMatch();
            yield return null;
            float after1 = _mm.ElapsedSeconds;
            yield return null;
            float after2 = _mm.ElapsedSeconds;

            Assert.Greater(after2, after1,
                "ElapsedSeconds must increase each frame while match is active.");
        }

        /// <summary>ElapsedSeconds does not advance after AbortMatch.</summary>
        [UnityTest]
        public IEnumerator ElapsedSeconds_FreezeAfterAbort()
        {
            _mm.StartMatch();
            yield return null;

            _mm.AbortMatch();
            float atAbort = _mm.ElapsedSeconds;
            yield return null;
            yield return null;

            Assert.AreEqual(atAbort, _mm.ElapsedSeconds, 0.001f,
                "ElapsedSeconds must not change after AbortMatch.");
        }
    }
}

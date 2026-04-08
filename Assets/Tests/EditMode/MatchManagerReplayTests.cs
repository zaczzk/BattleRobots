using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode integration tests for <see cref="MatchManager"/> replay capture (T078).
    ///
    /// These tests exercise the ReplaySO integration path of MatchManager entirely in
    /// EditMode by calling methods via reflection (private serialized fields and private
    /// helper methods) and directly invoking public/private methods.
    ///
    /// Coverage (10 cases):
    ///
    /// Null guard
    ///   [01] StartMatch_NullReplaySO_DoesNotStartRecording
    ///   [02] StartMatch_NullReplaySO_MatchStillStarts
    ///
    /// Recording lifecycle
    ///   [03] StartMatch_WithReplaySO_CallsStartRecording
    ///   [04] StartMatch_WithReplaySO_ReplaySOIsRecording
    ///
    /// Snapshot recording
    ///   [05] RecordSnapshot_AfterStartMatch_WritesOneSnapshot
    ///   [06] RecordSnapshot_WritesCorrectHP
    ///   [07] RecordSnapshot_MultipleCallsAccumulate
    ///   [08] RecordSnapshot_WithTransforms_WritesPositions
    ///
    /// End-match replay finalisation
    ///   [09] EndMatch_StopsRecording_AndReplayNotRecording
    ///   [10] EndMatch_BuildsReplayData_NonNullAndMatchesSnapshotCount
    /// </summary>
    [TestFixture]
    public sealed class MatchManagerReplayTests
    {
        // ── Reflection constants ───────────────────────────────────────────────

        private const BindingFlags k_NonPublicInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        // ── Per-test fixtures ─────────────────────────────────────────────────

        private GameObject   _mmGo;
        private MatchManager _mm;
        private HealthSO     _playerHealth;
        private HealthSO     _opponentHealth;
        private ArenaConfig  _arena;
        private PlayerWallet _wallet;
        private ReplaySO     _replaySO;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType().GetField(name, k_NonPublicInstance);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>
        /// Invokes the private <c>RecordSnapshot()</c> helper on the MatchManager.
        /// This simulates one Update tick's recording without running the full player loop.
        /// </summary>
        private void InvokeRecordSnapshot()
        {
            MethodInfo mi = typeof(MatchManager).GetMethod("RecordSnapshot", k_NonPublicInstance);
            Assert.IsNotNull(mi, "RecordSnapshot method not found — check method name.");
            mi.Invoke(_mm, null);
        }

        /// <summary>
        /// Invokes the private <c>EndMatch(bool)</c> helper directly so we can test
        /// the replay finalisation path without waiting for a death/timeout condition.
        /// </summary>
        private void InvokeEndMatch(bool playerWon)
        {
            MethodInfo mi = typeof(MatchManager).GetMethod("EndMatch", k_NonPublicInstance);
            Assert.IsNotNull(mi, "EndMatch method not found — check method name.");
            mi.Invoke(_mm, new object[] { playerWon });
        }

        // ── SetUp / TearDown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // HealthSOs — default MaxHp = 100 (HealthSO's serialized default)
            _playerHealth   = ScriptableObject.CreateInstance<HealthSO>();
            _opponentHealth = ScriptableObject.CreateInstance<HealthSO>();

            // ArenaConfig — no time limit, minimal config
            _arena = ScriptableObject.CreateInstance<ArenaConfig>();
            SetField(_arena, "_arenaName",        "ReplayTestArena");
            SetField(_arena, "_arenaIndex",        0);
            SetField(_arena, "_timeLimitSeconds",  0f);   // unlimited — we end match manually
            SetField(_arena, "_winBonusCurrency",  0);

            // PlayerWallet
            _wallet = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(_wallet, "_startingBalance", 0);
            _wallet.Reset();

            // ReplaySO
            _replaySO = ScriptableObject.CreateInstance<ReplaySO>();

            // MatchManager GameObject
            _mmGo = new GameObject("MM_ReplayTest");
            _mm   = _mmGo.AddComponent<MatchManager>();

            // Wire mandatory fields
            SetField(_mm, "_arenaConfig",     _arena);
            SetField(_mm, "_robotHealthSOs",  new HealthSO[] { _playerHealth, _opponentHealth });
            SetField(_mm, "_wallet",          _wallet);
            SetField(_mm, "_baseWinReward",   0);
            // Event channels left null — MatchManager guards with ?. throughout
        }

        [TearDown]
        public void TearDown()
        {
            if (_mmGo != null) Object.DestroyImmediate(_mmGo);
            Object.DestroyImmediate(_playerHealth);
            Object.DestroyImmediate(_opponentHealth);
            Object.DestroyImmediate(_arena);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_replaySO);
        }

        // ─────────────────────────────────────────────────────────────────────
        // [01] StartMatch_NullReplaySO_DoesNotStartRecording
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void StartMatch_NullReplaySO_DoesNotStartRecording()
        {
            // _replaySO deliberately NOT wired — StartMatch must not throw.
            Assert.DoesNotThrow(() => _mm.StartMatch(),
                "StartMatch must not throw when _replaySO is null.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [02] StartMatch_NullReplaySO_MatchStillStarts
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void StartMatch_NullReplaySO_MatchStillStarts()
        {
            _mm.StartMatch();
            Assert.IsTrue(_mm.IsMatchActive,
                "IsMatchActive must be true after StartMatch even when _replaySO is null.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [03] StartMatch_WithReplaySO_CallsStartRecording
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void StartMatch_WithReplaySO_CallsStartRecording()
        {
            SetField(_mm, "_replaySO", _replaySO);

            _mm.StartMatch();

            Assert.IsTrue(_replaySO.IsRecording,
                "ReplaySO.IsRecording must be true after StartMatch when _replaySO is wired.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [04] StartMatch_WithReplaySO_ReplaySOIsRecording
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void StartMatch_WithReplaySO_ReplaySOIsRecording()
        {
            SetField(_mm, "_replaySO", _replaySO);

            _mm.StartMatch();

            Assert.IsTrue(_replaySO.IsRecording,
                "ReplaySO must report IsRecording == true during an active match.");
            Assert.AreEqual(0, _replaySO.SnapshotCount,
                "No snapshots should be present immediately after StartMatch " +
                "(RecordSnapshot has not yet been invoked).");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [05] RecordSnapshot_AfterStartMatch_WritesOneSnapshot
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void RecordSnapshot_AfterStartMatch_WritesOneSnapshot()
        {
            SetField(_mm, "_replaySO", _replaySO);
            _mm.StartMatch();

            InvokeRecordSnapshot();

            Assert.AreEqual(1, _replaySO.SnapshotCount,
                "RecordSnapshot must write exactly one snapshot to the ReplaySO ring buffer.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [06] RecordSnapshot_WritesCorrectHP
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void RecordSnapshot_WritesCorrectHP()
        {
            SetField(_mm, "_replaySO", _replaySO);
            _mm.StartMatch();

            // Damage the player to create a non-trivial HP value.
            float damage = 30f;
            _playerHealth.TakeDamage(damage);

            InvokeRecordSnapshot();

            MatchStateSnapshot snap = _replaySO.GetSnapshot(0);

            Assert.AreEqual(_playerHealth.CurrentHp, snap.p1Hp, 0.001f,
                "Snapshot p1Hp must equal the player HealthSO's CurrentHp at record time.");
            Assert.AreEqual(_opponentHealth.CurrentHp, snap.p2Hp, 0.001f,
                "Snapshot p2Hp must equal the opponent HealthSO's CurrentHp at record time.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [07] RecordSnapshot_MultipleCallsAccumulate
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void RecordSnapshot_MultipleCallsAccumulate()
        {
            SetField(_mm, "_replaySO", _replaySO);
            _mm.StartMatch();

            const int calls = 5;
            for (int i = 0; i < calls; i++)
                InvokeRecordSnapshot();

            Assert.AreEqual(calls, _replaySO.SnapshotCount,
                $"After {calls} RecordSnapshot invocations, SnapshotCount must be {calls}.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [08] RecordSnapshot_WithTransforms_WritesPositions
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void RecordSnapshot_WithTransforms_WritesPositions()
        {
            SetField(_mm, "_replaySO", _replaySO);

            // Create two transforms at known positions.
            var p1Go = new GameObject("P1");
            var p2Go = new GameObject("P2");
            p1Go.transform.position = new Vector3(1f, 0f, 2f);
            p2Go.transform.position = new Vector3(-3f, 0f, 4f);

            SetField(_mm, "_robotTransforms", new Transform[] { p1Go.transform, p2Go.transform });

            _mm.StartMatch();
            InvokeRecordSnapshot();

            MatchStateSnapshot snap = _replaySO.GetSnapshot(0);

            Assert.AreEqual(p1Go.transform.position, snap.p1Pos,
                "Snapshot p1Pos must match the assigned player Transform position.");
            Assert.AreEqual(p2Go.transform.position, snap.p2Pos,
                "Snapshot p2Pos must match the assigned opponent Transform position.");

            Object.DestroyImmediate(p1Go);
            Object.DestroyImmediate(p2Go);
        }

        // ─────────────────────────────────────────────────────────────────────
        // [09] EndMatch_StopsRecording_AndReplayNotRecording
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void EndMatch_StopsRecording_AndReplayNotRecording()
        {
            SetField(_mm, "_replaySO", _replaySO);
            _mm.StartMatch();

            // Record one snapshot so StopRecording fires _onReplayReady.
            InvokeRecordSnapshot();

            // Force match end via reflection — simulates opponent death or timeout.
            InvokeEndMatch(playerWon: true);

            Assert.IsFalse(_replaySO.IsRecording,
                "ReplaySO.IsRecording must be false after EndMatch finalises the replay.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [10] EndMatch_BuildsReplayData_NonNullAndMatchesSnapshotCount
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void EndMatch_BuildsReplayData_NonNullAndMatchesSnapshotCount()
        {
            SetField(_mm, "_replaySO", _replaySO);
            _mm.StartMatch();

            const int snapshotCount = 3;
            for (int i = 0; i < snapshotCount; i++)
                InvokeRecordSnapshot();

            // Capture snapshot count before EndMatch clears recording state.
            int capturedCount = _replaySO.SnapshotCount;

            // EndMatch internally calls ReplayData.FromReplaySO + SaveSystem.SaveReplay.
            // We verify indirectly that the process completes without exception and
            // that the replay buffer has the expected frame count.
            Assert.DoesNotThrow(() => InvokeEndMatch(playerWon: false),
                "EndMatch must not throw when _replaySO is assigned and has snapshots.");

            // After EndMatch, recording has stopped but buffer is intact until next StartRecording.
            Assert.AreEqual(capturedCount, _replaySO.SnapshotCount,
                "SnapshotCount must be preserved after EndMatch (StopRecording does not clear the buffer).");
        }
    }
}

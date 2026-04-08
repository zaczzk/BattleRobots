using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T071 — ReplaySO ring-buffer correctness.
    ///
    /// Coverage (20 cases):
    ///
    /// Default / initial state
    ///   [01] Default_IsRecording_IsFalse
    ///   [02] Default_SnapshotCount_IsZero
    ///   [03] Default_IsEmpty_IsTrue
    ///   [04] Default_TotalDuration_IsZero
    ///
    /// StartRecording
    ///   [05] StartRecording_SetsIsRecordingTrue
    ///   [06] StartRecording_ClearsExistingSnapshots
    ///
    /// StopRecording
    ///   [07] StopRecording_WhenNotStarted_IsNoOp
    ///   [08] StopRecording_AfterRecording_SetsIsRecordingFalse
    ///
    /// Record
    ///   [09] Record_WhenNotRecording_DoesNotAddSnapshot
    ///   [10] Record_FirstSnapshot_SnapshotCountIsOne
    ///   [11] Record_MultipleSnapshots_SnapshotCountAccumulates
    ///   [12] Record_ExceedsCapacity_CountCappedAtCapacity
    ///   [13] Record_ExceedsCapacity_OldestSnapshotOverwritten
    ///
    /// GetSnapshot
    ///   [14] GetSnapshot_SingleEntry_ReturnsIt
    ///   [15] GetSnapshot_MultipleEntries_ReturnsOldestFirst
    ///
    /// Seek
    ///   [16] Seek_EmptyBuffer_ReturnsDefaultSnapshot
    ///   [17] Seek_ExactTimeMatch_ReturnsCorrectSnapshot
    ///   [18] Seek_BetweenTwoSnapshots_ReturnsNearest
    ///
    /// TotalDuration
    ///   [19] TotalDuration_AfterRecording_ReturnsNewestElapsedTime
    ///
    /// Clear
    ///   [20] Clear_ResetsCountAndStopsRecording
    /// </summary>
    [TestFixture]
    public sealed class ReplaySOTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private ReplaySO _replay;

        // Small capacity used for ring-overflow tests; avoids recording 600 snapshots.
        private const int SmallCapacity = 4;

        [SetUp]
        public void SetUp()
        {
            _replay = ScriptableObject.CreateInstance<ReplaySO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_replay);
        }

        // ── [01] Default_IsRecording_IsFalse ──────────────────────────────────

        [Test]
        public void Default_IsRecording_IsFalse()
        {
            Assert.IsFalse(_replay.IsRecording,
                "A freshly created ReplaySO must not be recording.");
        }

        // ── [02] Default_SnapshotCount_IsZero ────────────────────────────────

        [Test]
        public void Default_SnapshotCount_IsZero()
        {
            Assert.AreEqual(0, _replay.SnapshotCount,
                "SnapshotCount must be 0 before any recording.");
        }

        // ── [03] Default_IsEmpty_IsTrue ───────────────────────────────────────

        [Test]
        public void Default_IsEmpty_IsTrue()
        {
            Assert.IsTrue(_replay.IsEmpty,
                "IsEmpty must be true on a fresh instance.");
        }

        // ── [04] Default_TotalDuration_IsZero ─────────────────────────────────

        [Test]
        public void Default_TotalDuration_IsZero()
        {
            Assert.AreEqual(0f, _replay.TotalDuration, 0.0001f,
                "TotalDuration must be 0 when no snapshots have been recorded.");
        }

        // ── [05] StartRecording_SetsIsRecordingTrue ───────────────────────────

        [Test]
        public void StartRecording_SetsIsRecordingTrue()
        {
            _replay.StartRecording();
            Assert.IsTrue(_replay.IsRecording);
        }

        // ── [06] StartRecording_ClearsExistingSnapshots ───────────────────────

        [Test]
        public void StartRecording_ClearsExistingSnapshots()
        {
            // Record two snapshots in the first recording pass.
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f));
            _replay.Record(MakeSnapshot(2f));
            Assert.AreEqual(2, _replay.SnapshotCount);

            // Starting a new recording must clear the previous data.
            _replay.StartRecording();
            Assert.AreEqual(0, _replay.SnapshotCount,
                "StartRecording() must clear all previously recorded snapshots.");
        }

        // ── [07] StopRecording_WhenNotStarted_IsNoOp ─────────────────────────

        [Test]
        public void StopRecording_WhenNotStarted_IsNoOp()
        {
            // IsRecording is false; calling StopRecording must not throw and must
            // leave IsRecording false.
            Assert.DoesNotThrow(() => _replay.StopRecording());
            Assert.IsFalse(_replay.IsRecording,
                "IsRecording must remain false when StopRecording is called without a preceding StartRecording.");
        }

        // ── [08] StopRecording_AfterRecording_SetsIsRecordingFalse ───────────

        [Test]
        public void StopRecording_AfterRecording_SetsIsRecordingFalse()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f));
            _replay.StopRecording();

            Assert.IsFalse(_replay.IsRecording,
                "IsRecording must be false after StopRecording().");
        }

        // ── [09] Record_WhenNotRecording_DoesNotAddSnapshot ───────────────────

        [Test]
        public void Record_WhenNotRecording_DoesNotAddSnapshot()
        {
            // Do NOT call StartRecording.
            _replay.Record(MakeSnapshot(1f));

            Assert.AreEqual(0, _replay.SnapshotCount,
                "Record() must be a no-op when IsRecording is false.");
        }

        // ── [10] Record_FirstSnapshot_SnapshotCountIsOne ─────────────────────

        [Test]
        public void Record_FirstSnapshot_SnapshotCountIsOne()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(0.5f));

            Assert.AreEqual(1, _replay.SnapshotCount);
        }

        // ── [11] Record_MultipleSnapshots_SnapshotCountAccumulates ───────────

        [Test]
        public void Record_MultipleSnapshots_SnapshotCountAccumulates()
        {
            _replay.StartRecording();
            for (int i = 0; i < SmallCapacity; i++)
                _replay.Record(MakeSnapshot(i * 0.1f));

            Assert.AreEqual(SmallCapacity, _replay.SnapshotCount);
        }

        // ── [12] Record_ExceedsCapacity_CountCappedAtCapacity ─────────────────

        [Test]
        public void Record_ExceedsCapacity_CountCappedAtCapacity()
        {
            SetCapacity(SmallCapacity);
            _replay.StartRecording();

            // Record more entries than the capacity allows.
            for (int i = 0; i < SmallCapacity + 2; i++)
                _replay.Record(MakeSnapshot(i * 0.1f));

            Assert.AreEqual(SmallCapacity, _replay.SnapshotCount,
                $"SnapshotCount must never exceed capacity ({SmallCapacity}).");
        }

        // ── [13] Record_ExceedsCapacity_OldestSnapshotOverwritten ─────────────

        [Test]
        public void Record_ExceedsCapacity_OldestSnapshotOverwritten()
        {
            SetCapacity(SmallCapacity);
            _replay.StartRecording();

            // Fill the buffer with times 0.0, 0.1, 0.2, 0.3 …
            for (int i = 0; i < SmallCapacity; i++)
                _replay.Record(MakeSnapshot(i * 0.1f));

            // Write one more (time = 0.4).  This overwrites the oldest (time = 0.0).
            _replay.Record(MakeSnapshot(SmallCapacity * 0.1f));

            // Oldest (index 0) must now be the second-ever-recorded snapshot (time = 0.1),
            // NOT the original first (time = 0.0).
            MatchStateSnapshot oldest = _replay.GetSnapshot(0);
            Assert.AreEqual(0.1f, oldest.elapsedTime, 0.0001f,
                "After ring-buffer overflow, GetSnapshot(0) must return the second-oldest entry.");

            // Newest (last index) must be the just-written snapshot.
            MatchStateSnapshot newest = _replay.GetSnapshot(_replay.SnapshotCount - 1);
            Assert.AreEqual(SmallCapacity * 0.1f, newest.elapsedTime, 0.0001f,
                "GetSnapshot(count-1) must return the most recently recorded snapshot.");
        }

        // ── [14] GetSnapshot_SingleEntry_ReturnsIt ────────────────────────────

        [Test]
        public void GetSnapshot_SingleEntry_ReturnsIt()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(3.14f, p1Hp: 80f));

            MatchStateSnapshot snap = _replay.GetSnapshot(0);
            Assert.AreEqual(3.14f, snap.elapsedTime, 0.0001f);
            Assert.AreEqual(80f,   snap.p1Hp,        0.0001f);
        }

        // ── [15] GetSnapshot_MultipleEntries_ReturnsOldestFirst ───────────────

        [Test]
        public void GetSnapshot_MultipleEntries_ReturnsOldestFirst()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f));
            _replay.Record(MakeSnapshot(2f));
            _replay.Record(MakeSnapshot(3f));

            Assert.AreEqual(1f, _replay.GetSnapshot(0).elapsedTime, 0.0001f,
                "Index 0 must be the oldest (first recorded) snapshot.");
            Assert.AreEqual(3f, _replay.GetSnapshot(2).elapsedTime, 0.0001f,
                "Last index must be the newest (last recorded) snapshot.");
        }

        // ── [16] Seek_EmptyBuffer_ReturnsDefaultSnapshot ─────────────────────

        [Test]
        public void Seek_EmptyBuffer_ReturnsDefaultSnapshot()
        {
            MatchStateSnapshot result = _replay.Seek(5f);
            // default(MatchStateSnapshot) has elapsedTime == 0, p1Hp == 0 etc.
            Assert.AreEqual(0f, result.elapsedTime, 0.0001f,
                "Seek on an empty buffer must return default(MatchStateSnapshot).");
        }

        // ── [17] Seek_ExactTimeMatch_ReturnsCorrectSnapshot ───────────────────

        [Test]
        public void Seek_ExactTimeMatch_ReturnsCorrectSnapshot()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f, p1Hp: 90f));
            _replay.Record(MakeSnapshot(2f, p1Hp: 70f));
            _replay.Record(MakeSnapshot(3f, p1Hp: 50f));

            MatchStateSnapshot result = _replay.Seek(2f);
            Assert.AreEqual(2f,   result.elapsedTime, 0.0001f);
            Assert.AreEqual(70f,  result.p1Hp,        0.0001f);
        }

        // ── [18] Seek_BetweenTwoSnapshots_ReturnsNearest ─────────────────────

        [Test]
        public void Seek_BetweenTwoSnapshots_ReturnsNearest()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f));
            _replay.Record(MakeSnapshot(3f));

            // Time 1.9 is closer to 1.0 than to 3.0.
            MatchStateSnapshot nearerToFirst = _replay.Seek(1.9f);
            Assert.AreEqual(1f, nearerToFirst.elapsedTime, 0.0001f,
                "Seek(1.9) must return the snapshot at t=1 (distance 0.9) over t=3 (distance 1.1).");

            // Time 2.1 is closer to 3.0.
            MatchStateSnapshot nearerToSecond = _replay.Seek(2.1f);
            Assert.AreEqual(3f, nearerToSecond.elapsedTime, 0.0001f,
                "Seek(2.1) must return the snapshot at t=3 (distance 0.9) over t=1 (distance 1.1).");
        }

        // ── [19] TotalDuration_AfterRecording_ReturnsNewestElapsedTime ────────

        [Test]
        public void TotalDuration_AfterRecording_ReturnsNewestElapsedTime()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f));
            _replay.Record(MakeSnapshot(5f));
            _replay.Record(MakeSnapshot(9f));

            Assert.AreEqual(9f, _replay.TotalDuration, 0.0001f,
                "TotalDuration must equal the elapsedTime of the last recorded snapshot.");
        }

        // ── [20] Clear_ResetsCountAndStopsRecording ───────────────────────────

        [Test]
        public void Clear_ResetsCountAndStopsRecording()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f));
            _replay.Record(MakeSnapshot(2f));

            _replay.Clear();

            Assert.AreEqual(0,     _replay.SnapshotCount, "SnapshotCount must be 0 after Clear().");
            Assert.IsTrue  (_replay.IsEmpty,               "IsEmpty must be true after Clear().");
            Assert.IsFalse (_replay.IsRecording,           "IsRecording must be false after Clear().");
            Assert.AreEqual(0f,    _replay.TotalDuration, 0.0001f,
                "TotalDuration must be 0 after Clear().");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the private <c>_capacity</c> field on <see cref="_replay"/> and forces
        /// <c>EnsureBuffer</c> to run by calling <see cref="ReplaySO.StartRecording"/>
        /// (which calls EnsureBuffer internally), then immediately clearing.
        /// Call BEFORE any StartRecording / Record sequence that needs the small capacity.
        /// </summary>
        private void SetCapacity(int capacity)
        {
            FieldInfo fi = typeof(ReplaySO).GetField(
                "_capacity",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, "Field '_capacity' not found on ReplaySO via reflection.");
            fi.SetValue(_replay, capacity);

            // Trigger EnsureBuffer by starting (and immediately clearing) a recording.
            // This forces the buffer to reallocate at the new capacity.
            _replay.StartRecording();
            _replay.Clear();
        }

        private static MatchStateSnapshot MakeSnapshot(float elapsedTime,
            float p1Hp = 100f, float p2Hp = 100f)
        {
            return new MatchStateSnapshot(
                elapsedTime,
                p1Hp,
                p2Hp,
                Vector3.zero,
                Vector3.zero);
        }
    }
}

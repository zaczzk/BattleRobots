using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T072 — ReplayData export and SaveSystem replay persistence.
    ///
    /// Coverage (20 cases):
    ///
    /// ReplayData.FromReplaySO — builder correctness
    ///   [01] FromReplaySO_NullReplay_ThrowsArgumentNullException
    ///   [02] FromReplaySO_NullRecord_SetsEmptyTimestampAndZeroArena
    ///   [03] FromReplaySO_EmptyReplay_ReturnsNoFrames
    ///   [04] FromReplaySO_SingleSnapshot_CreatesOneFrame
    ///   [05] FromReplaySO_MultipleSnapshots_FrameCountMatches
    ///   [06] FromReplaySO_Frame_ElapsedTimePreserved
    ///   [07] FromReplaySO_Frame_HpValuesPreserved
    ///   [08] FromReplaySO_Frame_PositionComponentsPreserved
    ///   [09] FromReplaySO_MatchTimestamp_CopiedFromRecord
    ///   [10] FromReplaySO_ArenaIndex_CopiedFromRecord
    ///   [11] FromReplaySO_Frames_AreOrderedOldestFirst
    ///
    /// SaveSystem.SaveReplay / LoadReplay / DeleteReplay / ReplayExists
    ///   [12] SaveReplay_NullData_ThrowsArgumentNullException
    ///   [13] ReplayExists_WhenNoFile_ReturnsFalse
    ///   [14] ReplayExists_AfterSave_ReturnsTrue
    ///   [15] SaveReplay_LoadReplay_RoundTripsFrameCount
    ///   [16] SaveReplay_LoadReplay_RoundTripsFrameData
    ///   [17] SaveReplay_LoadReplay_RoundTripsMetadata
    ///   [18] LoadReplay_WhenNoFile_ReturnsNull
    ///   [19] DeleteReplay_RemovesFile_ExistsReturnsFalse
    ///   [20] SaveReplay_CalledTwiceSameTimestamp_OverwritesWithLatestData
    /// </summary>
    [TestFixture]
    public sealed class ReplayExportTests
    {
        // ── Constants ──────────────────────────────────────────────────────────

        // Use a fixed timestamp so cleanup is deterministic.
        private const string TestTimestamp = "2026-04-08T10:00:00Z";
        private const string AltTimestamp  = "2026-04-08T11:00:00Z";

        // ── Fixtures ──────────────────────────────────────────────────────────

        private ReplaySO _replay;

        [SetUp]
        public void SetUp()
        {
            _replay = ScriptableObject.CreateInstance<ReplaySO>();
            // Delete any replay files from previous failed runs so tests start clean.
            SaveSystem.DeleteReplay(TestTimestamp);
            SaveSystem.DeleteReplay(AltTimestamp);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_replay);
            SaveSystem.DeleteReplay(TestTimestamp);
            SaveSystem.DeleteReplay(AltTimestamp);
        }

        // ── [01] FromReplaySO_NullReplay_ThrowsArgumentNullException ──────────

        [Test]
        public void FromReplaySO_NullReplay_ThrowsArgumentNullException()
        {
            var record = MakeRecord(TestTimestamp);
            Assert.Throws<ArgumentNullException>(
                () => ReplayData.FromReplaySO(null, record),
                "Passing null for the ReplaySO must throw ArgumentNullException.");
        }

        // ── [02] FromReplaySO_NullRecord_SetsEmptyTimestampAndZeroArena ───────

        [Test]
        public void FromReplaySO_NullRecord_SetsEmptyTimestampAndZeroArena()
        {
            ReplayData data = ReplayData.FromReplaySO(_replay, null);

            Assert.AreEqual(string.Empty, data.matchTimestamp,
                "A null MatchRecord must produce an empty matchTimestamp.");
            Assert.AreEqual(0, data.arenaIndex,
                "A null MatchRecord must produce arenaIndex 0.");
        }

        // ── [03] FromReplaySO_EmptyReplay_ReturnsNoFrames ─────────────────────

        [Test]
        public void FromReplaySO_EmptyReplay_ReturnsNoFrames()
        {
            // ReplaySO never started recording — SnapshotCount is 0.
            ReplayData data = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp));

            Assert.IsNotNull(data.frames, "frames must never be null.");
            Assert.AreEqual(0, data.frames.Count,
                "An empty ReplaySO must produce a ReplayData with 0 frames.");
        }

        // ── [04] FromReplaySO_SingleSnapshot_CreatesOneFrame ──────────────────

        [Test]
        public void FromReplaySO_SingleSnapshot_CreatesOneFrame()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f, 90f, 80f,
                new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f)));
            _replay.StopRecording();

            ReplayData data = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp));

            Assert.AreEqual(1, data.frames.Count,
                "One recorded snapshot must produce exactly one ReplayFrame.");
        }

        // ── [05] FromReplaySO_MultipleSnapshots_FrameCountMatches ─────────────

        [Test]
        public void FromReplaySO_MultipleSnapshots_FrameCountMatches()
        {
            const int count = 5;
            _replay.StartRecording();
            for (int i = 0; i < count; i++)
                _replay.Record(MakeSnapshot(i * 0.1f));
            _replay.StopRecording();

            ReplayData data = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp));

            Assert.AreEqual(count, data.frames.Count,
                $"FromReplaySO must create exactly {count} frames from {count} recorded snapshots.");
        }

        // ── [06] FromReplaySO_Frame_ElapsedTimePreserved ─────────────────────

        [Test]
        public void FromReplaySO_Frame_ElapsedTimePreserved()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(3.75f));
            _replay.StopRecording();

            ReplayData data = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp));

            Assert.AreEqual(3.75f, data.frames[0].elapsedTime, 0.0001f,
                "ReplayFrame.elapsedTime must equal the source snapshot's elapsedTime.");
        }

        // ── [07] FromReplaySO_Frame_HpValuesPreserved ─────────────────────────

        [Test]
        public void FromReplaySO_Frame_HpValuesPreserved()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f, p1Hp: 72f, p2Hp: 44f));
            _replay.StopRecording();

            ReplayFrame frame = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp)).frames[0];

            Assert.AreEqual(72f, frame.p1Hp, 0.0001f, "p1Hp must round-trip through FromReplaySO.");
            Assert.AreEqual(44f, frame.p2Hp, 0.0001f, "p2Hp must round-trip through FromReplaySO.");
        }

        // ── [08] FromReplaySO_Frame_PositionComponentsPreserved ──────────────

        [Test]
        public void FromReplaySO_Frame_PositionComponentsPreserved()
        {
            var p1 = new Vector3(1.5f, 0f, -2.5f);
            var p2 = new Vector3(-3f,  0f,  4f);

            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f, p1Pos: p1, p2Pos: p2));
            _replay.StopRecording();

            ReplayFrame frame = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp)).frames[0];

            Assert.AreEqual(p1.x, frame.p1X, 0.0001f, "p1X must match Vector3.x.");
            Assert.AreEqual(p1.y, frame.p1Y, 0.0001f, "p1Y must match Vector3.y.");
            Assert.AreEqual(p1.z, frame.p1Z, 0.0001f, "p1Z must match Vector3.z.");
            Assert.AreEqual(p2.x, frame.p2X, 0.0001f, "p2X must match Vector3.x.");
            Assert.AreEqual(p2.y, frame.p2Y, 0.0001f, "p2Y must match Vector3.y.");
            Assert.AreEqual(p2.z, frame.p2Z, 0.0001f, "p2Z must match Vector3.z.");
        }

        // ── [09] FromReplaySO_MatchTimestamp_CopiedFromRecord ─────────────────

        [Test]
        public void FromReplaySO_MatchTimestamp_CopiedFromRecord()
        {
            ReplayData data = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp));

            Assert.AreEqual(TestTimestamp, data.matchTimestamp,
                "matchTimestamp must be copied verbatim from MatchRecord.timestamp.");
        }

        // ── [10] FromReplaySO_ArenaIndex_CopiedFromRecord ─────────────────────

        [Test]
        public void FromReplaySO_ArenaIndex_CopiedFromRecord()
        {
            var record = MakeRecord(TestTimestamp, arenaIndex: 3);
            ReplayData data = ReplayData.FromReplaySO(_replay, record);

            Assert.AreEqual(3, data.arenaIndex,
                "arenaIndex must be copied verbatim from MatchRecord.arenaIndex.");
        }

        // ── [11] FromReplaySO_Frames_AreOrderedOldestFirst ───────────────────

        [Test]
        public void FromReplaySO_Frames_AreOrderedOldestFirst()
        {
            _replay.StartRecording();
            _replay.Record(MakeSnapshot(1f));
            _replay.Record(MakeSnapshot(2f));
            _replay.Record(MakeSnapshot(3f));
            _replay.StopRecording();

            ReplayData data = ReplayData.FromReplaySO(_replay, MakeRecord(TestTimestamp));

            Assert.AreEqual(1f, data.frames[0].elapsedTime, 0.0001f,
                "Frame[0] must be the oldest snapshot (elapsedTime=1).");
            Assert.AreEqual(3f, data.frames[2].elapsedTime, 0.0001f,
                "Frame[2] must be the newest snapshot (elapsedTime=3).");
        }

        // ── [12] SaveReplay_NullData_ThrowsArgumentNullException ─────────────

        [Test]
        public void SaveReplay_NullData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => SaveSystem.SaveReplay(null),
                "SaveReplay(null) must throw ArgumentNullException.");
        }

        // ── [13] ReplayExists_WhenNoFile_ReturnsFalse ─────────────────────────

        [Test]
        public void ReplayExists_WhenNoFile_ReturnsFalse()
        {
            Assert.IsFalse(SaveSystem.ReplayExists(TestTimestamp),
                "ReplayExists must return false before any file is written.");
        }

        // ── [14] ReplayExists_AfterSave_ReturnsTrue ───────────────────────────

        [Test]
        public void ReplayExists_AfterSave_ReturnsTrue()
        {
            SaveSystem.SaveReplay(MakeReplayData(TestTimestamp));

            Assert.IsTrue(SaveSystem.ReplayExists(TestTimestamp),
                "ReplayExists must return true immediately after SaveReplay.");
        }

        // ── [15] SaveReplay_LoadReplay_RoundTripsFrameCount ───────────────────

        [Test]
        public void SaveReplay_LoadReplay_RoundTripsFrameCount()
        {
            const int frameCount = 7;
            SaveSystem.SaveReplay(MakeReplayData(TestTimestamp, frameCount: frameCount));

            ReplayData loaded = SaveSystem.LoadReplay(TestTimestamp);

            Assert.IsNotNull(loaded, "LoadReplay must not return null for a file that was saved.");
            Assert.AreEqual(frameCount, loaded.frames.Count,
                "Frame count must survive a SaveReplay → LoadReplay round-trip.");
        }

        // ── [16] SaveReplay_LoadReplay_RoundTripsFrameData ────────────────────

        [Test]
        public void SaveReplay_LoadReplay_RoundTripsFrameData()
        {
            var data = new ReplayData { matchTimestamp = TestTimestamp };
            data.frames.Add(new ReplayFrame
            {
                elapsedTime = 1.5f,
                p1Hp        = 75f, p2Hp = 50f,
                p1X         =  2f, p1Y  =  0f, p1Z = -3f,
                p2X         = -2f, p2Y  =  0f, p2Z =  3f,
            });
            SaveSystem.SaveReplay(data);

            ReplayFrame frame = SaveSystem.LoadReplay(TestTimestamp).frames[0];

            Assert.AreEqual(1.5f, frame.elapsedTime, 0.0001f, "elapsedTime must survive round-trip.");
            Assert.AreEqual(75f,  frame.p1Hp,        0.0001f, "p1Hp must survive round-trip.");
            Assert.AreEqual(50f,  frame.p2Hp,        0.0001f, "p2Hp must survive round-trip.");
            Assert.AreEqual( 2f,  frame.p1X,         0.0001f, "p1X must survive round-trip.");
            Assert.AreEqual( 0f,  frame.p1Y,         0.0001f, "p1Y must survive round-trip.");
            Assert.AreEqual(-3f,  frame.p1Z,         0.0001f, "p1Z must survive round-trip.");
            Assert.AreEqual(-2f,  frame.p2X,         0.0001f, "p2X must survive round-trip.");
            Assert.AreEqual( 0f,  frame.p2Y,         0.0001f, "p2Y must survive round-trip.");
            Assert.AreEqual( 3f,  frame.p2Z,         0.0001f, "p2Z must survive round-trip.");
        }

        // ── [17] SaveReplay_LoadReplay_RoundTripsMetadata ─────────────────────

        [Test]
        public void SaveReplay_LoadReplay_RoundTripsMetadata()
        {
            SaveSystem.SaveReplay(MakeReplayData(TestTimestamp, arenaIndex: 2));

            ReplayData loaded = SaveSystem.LoadReplay(TestTimestamp);

            Assert.AreEqual(TestTimestamp, loaded.matchTimestamp,
                "matchTimestamp must survive a round-trip.");
            Assert.AreEqual(2, loaded.arenaIndex,
                "arenaIndex must survive a round-trip.");
        }

        // ── [18] LoadReplay_WhenNoFile_ReturnsNull ────────────────────────────

        [Test]
        public void LoadReplay_WhenNoFile_ReturnsNull()
        {
            ReplayData result = SaveSystem.LoadReplay(TestTimestamp);

            Assert.IsNull(result,
                "LoadReplay must return null when no file exists for the given timestamp.");
        }

        // ── [19] DeleteReplay_RemovesFile_ExistsReturnsFalse ─────────────────

        [Test]
        public void DeleteReplay_RemovesFile_ExistsReturnsFalse()
        {
            SaveSystem.SaveReplay(MakeReplayData(TestTimestamp));
            Assert.IsTrue(SaveSystem.ReplayExists(TestTimestamp),
                "Pre-condition: file must exist before DeleteReplay.");

            SaveSystem.DeleteReplay(TestTimestamp);

            Assert.IsFalse(SaveSystem.ReplayExists(TestTimestamp),
                "ReplayExists must return false after DeleteReplay.");
        }

        // ── [20] SaveReplay_CalledTwiceSameTimestamp_OverwritesWithLatestData ──

        [Test]
        public void SaveReplay_CalledTwiceSameTimestamp_OverwritesWithLatestData()
        {
            SaveSystem.SaveReplay(MakeReplayData(TestTimestamp, frameCount: 3));
            SaveSystem.SaveReplay(MakeReplayData(TestTimestamp, frameCount: 9));

            ReplayData loaded = SaveSystem.LoadReplay(TestTimestamp);

            Assert.AreEqual(9, loaded.frames.Count,
                "A second SaveReplay for the same timestamp must overwrite the first (frame count = 9).");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static MatchRecord MakeRecord(string timestamp, int arenaIndex = 0) =>
            new MatchRecord { timestamp = timestamp, arenaIndex = arenaIndex };

        private static MatchStateSnapshot MakeSnapshot(float elapsedTime,
            float p1Hp = 100f, float p2Hp = 100f,
            Vector3 p1Pos = default, Vector3 p2Pos = default) =>
            new MatchStateSnapshot(elapsedTime, p1Hp, p2Hp, p1Pos, p2Pos);

        private static ReplayData MakeReplayData(string timestamp,
            int arenaIndex = 0, int frameCount = 0)
        {
            var data = new ReplayData
            {
                matchTimestamp = timestamp,
                arenaIndex     = arenaIndex,
            };

            for (int i = 0; i < frameCount; i++)
            {
                data.frames.Add(new ReplayFrame
                {
                    elapsedTime = i * 0.1f,
                    p1Hp        = 100f,
                    p2Hp        = 100f,
                });
            }

            return data;
        }
    }
}

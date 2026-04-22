using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSamplerTests
    {
        private static ZoneControlCaptureSamplerSO CreateSO(
            int recordingsNeeded  = 7,
            int glitchPerBot      = 2,
            int bonusPerRecording = 1735)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSamplerSO>();
            typeof(ZoneControlCaptureSamplerSO)
                .GetField("_recordingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, recordingsNeeded);
            typeof(ZoneControlCaptureSamplerSO)
                .GetField("_glitchPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, glitchPerBot);
            typeof(ZoneControlCaptureSamplerSO)
                .GetField("_bonusPerRecording", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRecording);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSamplerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSamplerController>();
        }

        [Test]
        public void SO_FreshInstance_Recordings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Recordings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RecordCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RecordCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRecordings()
        {
            var so = CreateSO(recordingsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Recordings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(recordingsNeeded: 3, bonusPerRecording: 1735);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1735));
            Assert.That(so.RecordCount, Is.EqualTo(1));
            Assert.That(so.Recordings,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(recordingsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesRecordings()
        {
            var so = CreateSO(recordingsNeeded: 7, glitchPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Recordings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(recordingsNeeded: 7, glitchPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Recordings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordingProgress_Clamped()
        {
            var so = CreateSO(recordingsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.RecordingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSamplerRecorded_FiresEvent()
        {
            var so    = CreateSO(recordingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSamplerSO)
                .GetField("_onSamplerRecorded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(recordingsNeeded: 2, bonusPerRecording: 1735);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Recordings,        Is.EqualTo(0));
            Assert.That(so.RecordCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRecords_Accumulate()
        {
            var so = CreateSO(recordingsNeeded: 2, bonusPerRecording: 1735);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RecordCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3470));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SamplerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SamplerSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureSamplerController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(true);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}

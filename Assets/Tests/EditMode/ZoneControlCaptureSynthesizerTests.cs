using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSynthesizerTests
    {
        private static ZoneControlCaptureSynthesizerSO CreateSO(
            int voicesNeeded  = 5,
            int resetPerBot   = 1,
            int bonusPerSynth = 1720)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSynthesizerSO>();
            typeof(ZoneControlCaptureSynthesizerSO)
                .GetField("_voicesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, voicesNeeded);
            typeof(ZoneControlCaptureSynthesizerSO)
                .GetField("_resetPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, resetPerBot);
            typeof(ZoneControlCaptureSynthesizerSO)
                .GetField("_bonusPerSynth", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSynth);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSynthesizerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSynthesizerController>();
        }

        [Test]
        public void SO_FreshInstance_Voices_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Voices, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SynthCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SynthCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesVoices()
        {
            var so = CreateSO(voicesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Voices, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(voicesNeeded: 3, bonusPerSynth: 1720);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(1720));
            Assert.That(so.SynthCount, Is.EqualTo(1));
            Assert.That(so.Voices,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(voicesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesVoices()
        {
            var so = CreateSO(voicesNeeded: 5, resetPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Voices, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(voicesNeeded: 5, resetPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Voices, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VoiceProgress_Clamped()
        {
            var so = CreateSO(voicesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.VoiceProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSynthesizerPlayed_FiresEvent()
        {
            var so    = CreateSO(voicesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSynthesizerSO)
                .GetField("_onSynthesizerPlayed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(voicesNeeded: 2, bonusPerSynth: 1720);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Voices,            Is.EqualTo(0));
            Assert.That(so.SynthCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSynths_Accumulate()
        {
            var so = CreateSO(voicesNeeded: 2, bonusPerSynth: 1720);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SynthCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3440));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SynthesizerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SynthesizerSO, Is.Null);
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
            typeof(ZoneControlCaptureSynthesizerController)
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

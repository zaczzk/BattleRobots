using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCrestTests
    {
        private static ZoneControlCaptureCrestSO CreateSO(
            float waveRise = 1f, float waveFall = 0.5f, float waveMax = 5f, int bonus = 400)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCrestSO>();
            typeof(ZoneControlCaptureCrestSO)
                .GetField("_waveRisePerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, waveRise);
            typeof(ZoneControlCaptureCrestSO)
                .GetField("_waveFallPerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, waveFall);
            typeof(ZoneControlCaptureCrestSO)
                .GetField("_waveHeightForCrest", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, waveMax);
            typeof(ZoneControlCaptureCrestSO)
                .GetField("_bonusPerCrest", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCrestController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCrestController>();
        }

        [Test]
        public void SO_FreshInstance_CrestCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CrestCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentWaveHeight_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentWaveHeight, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncreasesWaveHeight()
        {
            var so = CreateSO(waveRise: 1f, waveMax: 5f);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentWaveHeight, Is.EqualTo(1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesMax_TriggersCrestAndResetsWave()
        {
            var so = CreateSO(waveRise: 1f, waveMax: 3f, bonus: 400);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CrestCount,        Is.EqualTo(1));
            Assert.That(so.CurrentWaveHeight, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Crest_UpdatesTotalBonus()
        {
            var so = CreateSO(waveRise: 1f, waveMax: 2f, bonus: 400);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Crest_FiresEvent()
        {
            var so    = CreateSO(waveRise: 1f, waveMax: 1f, bonus: 400);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCrestSO)
                .GetField("_onCrest", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesWaveHeight()
        {
            var so = CreateSO(waveRise: 1f, waveFall: 0.5f, waveMax: 5f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentWaveHeight, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WaveNotBelowZero()
        {
            var so = CreateSO(waveRise: 1f, waveFall: 5f, waveMax: 10f);
            so.RecordBotCapture();
            Assert.That(so.CurrentWaveHeight, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WaveProgress_CorrectFraction()
        {
            var so = CreateSO(waveRise: 1f, waveMax: 4f);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.WaveProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(waveRise: 1f, waveMax: 3f, bonus: 400);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CrestCount,        Is.EqualTo(0));
            Assert.That(so.CurrentWaveHeight, Is.EqualTo(0f));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CrestSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CrestSO, Is.Null);
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
            typeof(ZoneControlCaptureCrestController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(true);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();
            typeof(ZoneControlCaptureCrestController)
                .GetField("_crestSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureCrestController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}

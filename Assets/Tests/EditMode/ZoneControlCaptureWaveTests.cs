using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T453: <see cref="ZoneControlCaptureWaveSO"/> and
    /// <see cref="ZoneControlCaptureWaveController"/>.
    ///
    /// ZoneControlCaptureWaveTests (12):
    ///   SO_FreshInstance_CurrentWaveCaptures_Zero                           x1
    ///   SO_FreshInstance_IsWaveActive_False                                 x1
    ///   SO_RecordCapture_IncrementsCaptures                                 x1
    ///   SO_RecordCapture_ArmsWave                                           x1
    ///   SO_Tick_BeforeCooldown_NoScore                                      x1
    ///   SO_Tick_AtCooldown_ScoresWave                                       x1
    ///   SO_Tick_AfterScore_WaveInactive                                     x1
    ///   SO_Tick_UpdatesBestWaveCaptures                                     x1
    ///   SO_Reset_ClearsAll                                                  x1
    ///   Controller_FreshInstance_WaveSO_Null                                x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlCaptureWaveTests
    {
        private static ZoneControlCaptureWaveSO CreateSO(
            float waveCooldown     = 8f,
            int   pointsPerCapture = 25)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureWaveSO>();
            typeof(ZoneControlCaptureWaveSO)
                .GetField("_waveCooldown", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, waveCooldown);
            typeof(ZoneControlCaptureWaveSO)
                .GetField("_pointsPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pointsPerCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureWaveController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureWaveController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentWaveCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentWaveCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsWaveActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsWaveActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCaptures()
        {
            var so = CreateSO();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.CurrentWaveCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ArmsWave()
        {
            var so = CreateSO();
            so.RecordCapture();
            Assert.That(so.IsWaveActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BeforeCooldown_NoScore()
        {
            var so = CreateSO(waveCooldown: 5f, pointsPerCapture: 25);
            so.RecordCapture();
            so.Tick(2f);
            Assert.That(so.TotalWavesScored,   Is.EqualTo(0));
            Assert.That(so.CurrentWaveCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AtCooldown_ScoresWave()
        {
            var so = CreateSO(waveCooldown: 5f, pointsPerCapture: 25);
            so.RecordCapture();
            so.RecordCapture();
            so.Tick(5f);
            Assert.That(so.TotalWavesScored,  Is.EqualTo(1));
            Assert.That(so.LastWaveBonus,     Is.EqualTo(50));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AfterScore_WaveInactive()
        {
            var so = CreateSO(waveCooldown: 5f);
            so.RecordCapture();
            so.Tick(5f);
            Assert.That(so.IsWaveActive,        Is.False);
            Assert.That(so.CurrentWaveCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_UpdatesBestWaveCaptures()
        {
            var so = CreateSO(waveCooldown: 5f, pointsPerCapture: 10);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            so.Tick(5f);
            Assert.That(so.BestWaveCaptures, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(waveCooldown: 5f, pointsPerCapture: 25);
            so.RecordCapture();
            so.Tick(5f);
            so.Reset();
            Assert.That(so.CurrentWaveCaptures, Is.EqualTo(0));
            Assert.That(so.TotalWavesScored,    Is.EqualTo(0));
            Assert.That(so.BestWaveCaptures,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Assert.That(so.IsWaveActive,        Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_WaveSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.WaveSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureWaveController)
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

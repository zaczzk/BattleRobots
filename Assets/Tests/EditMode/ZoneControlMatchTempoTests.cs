using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T419: <see cref="ZoneControlMatchTempoSO"/> and
    /// <see cref="ZoneControlMatchTempoController"/>.
    ///
    /// ZoneControlMatchTempoTests (12):
    ///   SO_FreshInstance_CurrentTempo_Low                       x1
    ///   SO_EvaluateTempo_BelowSlowThreshold_StaysLow           x1
    ///   SO_EvaluateTempo_AtSlowThreshold_BecomesNormal         x1
    ///   SO_EvaluateTempo_AboveSlowBelowFast_Normal             x1
    ///   SO_EvaluateTempo_AtFastThreshold_BecomesHigh           x1
    ///   SO_EvaluateTempo_SameTempo_NoEvent                     x1
    ///   SO_EvaluateTempo_Transition_FiresEvent                  x1
    ///   SO_GetTempoLabel_ReturnsCorrectString                   x1
    ///   SO_Reset_RestoresLow                                    x1
    ///   SO_OnValidate_ClampsThresholds                          x1
    ///   Controller_FreshInstance_TempoSO_Null                   x1
    ///   Controller_Refresh_NullSO_HidesPanel                    x1
    /// </summary>
    public sealed class ZoneControlMatchTempoTests
    {
        private static ZoneControlMatchTempoSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchTempoSO>();

        private static ZoneControlMatchTempoController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchTempoController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentTempo_Low()
        {
            var so = CreateSO();
            Assert.That(so.CurrentTempo, Is.EqualTo(MatchTempo.Low));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTempo_BelowSlowThreshold_StaysLow()
        {
            var so = CreateSO();
            so.EvaluateTempo(so.SlowThreshold - 0.1f);
            Assert.That(so.CurrentTempo, Is.EqualTo(MatchTempo.Low));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTempo_AtSlowThreshold_BecomesNormal()
        {
            var so = CreateSO();
            so.EvaluateTempo(so.SlowThreshold);
            Assert.That(so.CurrentTempo, Is.EqualTo(MatchTempo.Normal));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTempo_AboveSlowBelowFast_Normal()
        {
            var so = CreateSO();
            so.EvaluateTempo((so.SlowThreshold + so.FastThreshold) / 2f);
            Assert.That(so.CurrentTempo, Is.EqualTo(MatchTempo.Normal));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTempo_AtFastThreshold_BecomesHigh()
        {
            var so = CreateSO();
            so.EvaluateTempo(so.FastThreshold);
            Assert.That(so.CurrentTempo, Is.EqualTo(MatchTempo.High));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateTempo_SameTempo_NoEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchTempoSO)
                .GetField("_onTempoChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            // stays at Low → no event
            so.EvaluateTempo(0f);
            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_EvaluateTempo_Transition_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchTempoSO)
                .GetField("_onTempoChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.EvaluateTempo(so.FastThreshold);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetTempoLabel_ReturnsCorrectString()
        {
            var so = CreateSO();
            so.EvaluateTempo(so.FastThreshold);
            Assert.That(so.GetTempoLabel(), Is.EqualTo("High"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_RestoresLow()
        {
            var so = CreateSO();
            so.EvaluateTempo(so.FastThreshold);
            so.Reset();
            Assert.That(so.CurrentTempo, Is.EqualTo(MatchTempo.Low));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnValidate_ClampsThresholds()
        {
            var so = CreateSO();
            // Read private fields and set fast below slow via reflection to trigger OnValidate
            var fi = typeof(ZoneControlMatchTempoSO);
            var slowField = fi.GetField("_slowThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
            var fastField = fi.GetField("_fastThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
            slowField?.SetValue(so, 5f);
            fastField?.SetValue(so, 2f);
            // Invoke OnValidate via reflection
            fi.GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(so, null);
            Assert.That(so.FastThreshold, Is.GreaterThanOrEqualTo(so.SlowThreshold));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TempoSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TempoSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchTempoController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T426: <see cref="ZoneControlCaptureMomentumShiftSO"/> and
    /// <see cref="ZoneControlCaptureMomentumShiftController"/>.
    ///
    /// ZoneControlCaptureMomentumShiftTests (12):
    ///   SO_FreshInstance_ShiftCount_Zero                          x1
    ///   SO_FreshInstance_IsTied_True                              x1
    ///   SO_RecordPlayerCapture_IncrementsPlayerCaptures           x1
    ///   SO_RecordBotCapture_IncrementsBotCaptures                 x1
    ///   SO_FirstLeadEstablished_NoShiftFired                      x1
    ///   SO_LeadChange_FiresShiftEvent                             x1
    ///   SO_MultipleShifts_CountsCorrectly                         x1
    ///   SO_TiedState_NoShiftFired                                 x1
    ///   SO_Reset_ClearsAll                                        x1
    ///   SO_IsPlayerLeading_Correct                                x1
    ///   Controller_FreshInstance_MomentumShiftSO_Null             x1
    ///   Controller_Refresh_NullSO_HidesPanel                      x1
    /// </summary>
    public sealed class ZoneControlCaptureMomentumShiftTests
    {
        private static ZoneControlCaptureMomentumShiftSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureMomentumShiftSO>();

        private static ZoneControlCaptureMomentumShiftController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMomentumShiftController>();
        }

        [Test]
        public void SO_FreshInstance_ShiftCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ShiftCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsTied_True()
        {
            var so = CreateSO();
            Assert.That(so.IsTied, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsPlayerCaptures()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotCaptures()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.BotCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FirstLeadEstablished_NoShiftFired()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMomentumShiftSO)
                .GetField("_onMomentumShift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture(); // establishes lead silently

            Assert.That(fired,         Is.EqualTo(0));
            Assert.That(so.ShiftCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_LeadChange_FiresShiftEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMomentumShiftSO)
                .GetField("_onMomentumShift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture(); // player leads (established, no shift)
            so.RecordBotCapture();    // tied — no shift
            so.RecordBotCapture();    // bot leads — SHIFT

            Assert.That(fired,         Is.EqualTo(1));
            Assert.That(so.ShiftCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_MultipleShifts_CountsCorrectly()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(); // player leads (established)
            so.RecordBotCapture();    // tied
            so.RecordBotCapture();    // bot leads — shift 1
            so.RecordPlayerCapture(); // tied
            so.RecordPlayerCapture(); // player leads — shift 2
            Assert.That(so.ShiftCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TiedState_NoShiftFired()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMomentumShiftSO)
                .GetField("_onMomentumShift", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture(); // leads established
            so.RecordBotCapture();    // tied — no shift

            Assert.That(fired,        Is.EqualTo(0));
            Assert.That(so.IsTied,    Is.True);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Assert.That(so.BotCaptures,    Is.EqualTo(0));
            Assert.That(so.ShiftCount,     Is.EqualTo(0));
            Assert.That(so.IsTied,         Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsPlayerLeading_Correct()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsPlayerLeading, Is.True);
            Assert.That(so.IsTied,          Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MomentumShiftSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MomentumShiftSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureMomentumShiftController)
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

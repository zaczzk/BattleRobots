using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T433: <see cref="ZoneControlCaptureSpeedBonusSO"/> and
    /// <see cref="ZoneControlCaptureSpeedBonusController"/>.
    ///
    /// ZoneControlCaptureSpeedBonusTests (12):
    ///   SO_FreshInstance_HasAwarded_False                          x1
    ///   SO_FreshInstance_MatchStarted_False                        x1
    ///   SO_StartMatch_SetsMatchStarted                             x1
    ///   SO_RecordFirstCapture_WithinWindow_AwardsBonus             x1
    ///   SO_RecordFirstCapture_AtWindowStart_MaxBonus               x1
    ///   SO_RecordFirstCapture_OutsideWindow_ZeroBonus              x1
    ///   SO_RecordFirstCapture_Idempotent_NoDoubleAward             x1
    ///   SO_RecordFirstCapture_FiresEvent                           x1
    ///   SO_RecordFirstCapture_NoMatchStarted_ReturnsZero           x1
    ///   SO_Reset_ClearsAll                                         x1
    ///   Controller_FreshInstance_SpeedBonusSO_Null                 x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlCaptureSpeedBonusTests
    {
        private static ZoneControlCaptureSpeedBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureSpeedBonusSO>();

        private static ZoneControlCaptureSpeedBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpeedBonusController>();
        }

        [Test]
        public void SO_FreshInstance_HasAwarded_False()
        {
            var so = CreateSO();
            Assert.That(so.HasAwarded, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MatchStarted_False()
        {
            var so = CreateSO();
            Assert.That(so.MatchStarted, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartMatch_SetsMatchStarted()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            Assert.That(so.MatchStarted, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordFirstCapture_WithinWindow_AwardsBonus()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            int bonus = so.RecordFirstCapture(so.WindowSeconds * 0.5f);
            Assert.That(bonus, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordFirstCapture_AtWindowStart_MaxBonus()
        {
            var so = CreateSO();
            so.StartMatch(100f);
            int bonus = so.RecordFirstCapture(100f); // elapsed = 0 → ratio=1 → maxBonus
            Assert.That(bonus, Is.EqualTo(so.MaxBonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordFirstCapture_OutsideWindow_ZeroBonus()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            int bonus = so.RecordFirstCapture(so.WindowSeconds + 10f);
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordFirstCapture_Idempotent_NoDoubleAward()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            int first  = so.RecordFirstCapture(1f);
            int second = so.RecordFirstCapture(1f);
            Assert.That(first,  Is.GreaterThan(0));
            Assert.That(second, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordFirstCapture_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpeedBonusSO)
                .GetField("_onSpeedBonusAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.StartMatch(0f);
            so.RecordFirstCapture(0f); // max bonus, should fire

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordFirstCapture_NoMatchStarted_ReturnsZero()
        {
            var so    = CreateSO();
            int bonus = so.RecordFirstCapture(5f);
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartMatch(0f);
            so.RecordFirstCapture(1f);
            so.Reset();
            Assert.That(so.HasAwarded,   Is.False);
            Assert.That(so.MatchStarted, Is.False);
            Assert.That(so.LastBonusAmount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpeedBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpeedBonusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureSpeedBonusController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T432: <see cref="ZoneControlZoneOvertimeSO"/> and
    /// <see cref="ZoneControlZoneOvertimeController"/>.
    ///
    /// ZoneControlZoneOvertimeTests (12):
    ///   SO_FreshInstance_OvertimeCount_Zero                        x1
    ///   SO_FreshInstance_IsArmed_False                             x1
    ///   SO_RecordBotProgress_BelowThreshold_NotArmed               x1
    ///   SO_RecordBotProgress_AtThreshold_Armed                     x1
    ///   SO_RecordPlayerCapture_WhenArmed_IncrementsCount           x1
    ///   SO_RecordPlayerCapture_WhenArmed_FiresEvent                x1
    ///   SO_RecordPlayerCapture_WhenNotArmed_NoIncrement            x1
    ///   SO_RecordPlayerCapture_DisarmsAfterCapture                 x1
    ///   SO_TotalBonusAwarded_AccumulatesPerOvertime                x1
    ///   SO_Reset_ClearsAll                                         x1
    ///   Controller_FreshInstance_OvertimeSO_Null                   x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlZoneOvertimeTests
    {
        private static ZoneControlZoneOvertimeSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneOvertimeSO>();

        private static ZoneControlZoneOvertimeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneOvertimeController>();
        }

        [Test]
        public void SO_FreshInstance_OvertimeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OvertimeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsArmed_False()
        {
            var so = CreateSO();
            Assert.That(so.IsArmed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotProgress_BelowThreshold_NotArmed()
        {
            var so = CreateSO();
            so.RecordBotProgress(so.OvertimeThreshold * 0.5f);
            Assert.That(so.IsArmed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotProgress_AtThreshold_Armed()
        {
            var so = CreateSO();
            so.RecordBotProgress(so.OvertimeThreshold);
            Assert.That(so.IsArmed, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenArmed_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordBotProgress(1f);
            so.RecordPlayerCapture();
            Assert.That(so.OvertimeCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenArmed_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneOvertimeSO)
                .GetField("_onOvertimeCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotProgress(1f);
            so.RecordPlayerCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenNotArmed_NoIncrement()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.OvertimeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DisarmsAfterCapture()
        {
            var so = CreateSO();
            so.RecordBotProgress(1f);
            so.RecordPlayerCapture();
            Assert.That(so.IsArmed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesPerOvertime()
        {
            var so    = CreateSO();
            int bonus = so.BonusPerOvertime;

            so.RecordBotProgress(1f);
            so.RecordPlayerCapture();

            Assert.That(so.TotalBonusAwarded, Is.EqualTo(bonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordBotProgress(1f);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.OvertimeCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.IsArmed,           Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OvertimeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OvertimeSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneOvertimeController)
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

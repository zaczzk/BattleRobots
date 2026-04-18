using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T423: <see cref="ZoneControlComebackBonusSO"/> and
    /// <see cref="ZoneControlComebackBonusController"/>.
    ///
    /// ZoneControlComebackBonusTests (12):
    ///   SO_FreshInstance_ComebackCount_Zero                         x1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                     x1
    ///   SO_RecordComeback_IncrementsComebackCount                   x1
    ///   SO_RecordComeback_AccumulatesTotalBonus                     x1
    ///   SO_RecordComeback_MultipleCallsAccumulate                   x1
    ///   SO_RecordComeback_FiresEvent                                x1
    ///   SO_Reset_ClearsComebackCount                                x1
    ///   SO_Reset_ClearsTotalBonus                                   x1
    ///   SO_BonusOnComeback_DefaultIsPositive                        x1
    ///   SO_RecordComeback_TotalBonus_EqualsCountTimesBonus          x1
    ///   Controller_FreshInstance_ComebackBonusSO_Null               x1
    ///   Controller_Refresh_NullSO_HidesPanel                        x1
    /// </summary>
    public sealed class ZoneControlComebackBonusTests
    {
        private static ZoneControlComebackBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlComebackBonusSO>();

        private static ZoneControlComebackBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlComebackBonusController>();
        }

        [Test]
        public void SO_FreshInstance_ComebackCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComebackCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordComeback_IncrementsComebackCount()
        {
            var so = CreateSO();
            so.RecordComeback();
            Assert.That(so.ComebackCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordComeback_AccumulatesTotalBonus()
        {
            var so = CreateSO();
            so.RecordComeback();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.BonusOnComeback));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordComeback_MultipleCallsAccumulate()
        {
            var so = CreateSO();
            so.RecordComeback();
            so.RecordComeback();
            so.RecordComeback();
            Assert.That(so.ComebackCount,     Is.EqualTo(3));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.BonusOnComeback * 3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordComeback_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlComebackBonusSO)
                .GetField("_onComebackBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordComeback();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsComebackCount()
        {
            var so = CreateSO();
            so.RecordComeback();
            so.Reset();
            Assert.That(so.ComebackCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsTotalBonus()
        {
            var so = CreateSO();
            so.RecordComeback();
            so.Reset();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BonusOnComeback_DefaultIsPositive()
        {
            var so = CreateSO();
            Assert.That(so.BonusOnComeback, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordComeback_TotalBonus_EqualsCountTimesBonus()
        {
            var so = CreateSO();
            so.RecordComeback();
            so.RecordComeback();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(so.ComebackCount * so.BonusOnComeback));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ComebackBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ComebackBonusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlComebackBonusController)
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

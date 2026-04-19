using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T473: <see cref="ZoneControlMatchPivotSO"/> and
    /// <see cref="ZoneControlMatchPivotController"/>.
    ///
    /// ZoneControlMatchPivotTests (12):
    ///   SO_FreshInstance_PivotCount_Zero                                    x1
    ///   SO_FreshInstance_LeadEstablished_False                              x1
    ///   SO_RecordLeadState_FirstCall_EstablishesLead                        x1
    ///   SO_RecordLeadState_SameDirection_NoPivot                            x1
    ///   SO_RecordLeadState_PlayerReclaims_IncrementsPivot                   x1
    ///   SO_RecordLeadState_BotTakesLead_NoPivot                             x1
    ///   SO_RecordLeadState_MultiPivot_AccumulatesBonus                      x1
    ///   SO_RecordLeadState_PlayerFirst_ThenBotThenPlayer_OnePivot           x1
    ///   SO_Reset_ClearsState                                                x1
    ///   Controller_FreshInstance_PivotSO_Null                               x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                                x1
    /// </summary>
    public sealed class ZoneControlMatchPivotTests
    {
        private static ZoneControlMatchPivotSO CreateSO(int bonus = 300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPivotSO>();
            typeof(ZoneControlMatchPivotSO)
                .GetField("_bonusPerPivot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchPivotController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchPivotController>();
        }

        [Test]
        public void SO_FreshInstance_PivotCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PivotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LeadEstablished_False()
        {
            var so = CreateSO();
            Assert.That(so.LeadEstablished, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadState_FirstCall_EstablishesLead()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            Assert.That(so.LeadEstablished, Is.True);
            Assert.That(so.PivotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadState_SameDirection_NoPivot()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            so.RecordLeadState(true);
            Assert.That(so.PivotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadState_PlayerReclaims_IncrementsPivot()
        {
            var so = CreateSO();
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            Assert.That(so.PivotCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadState_BotTakesLead_NoPivot()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            Assert.That(so.PivotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadState_MultiPivot_AccumulatesBonus()
        {
            var so = CreateSO(bonus: 100);
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            Assert.That(so.PivotCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordLeadState_PlayerFirst_ThenBotThenPlayer_OnePivot()
        {
            var so = CreateSO();
            so.RecordLeadState(true);
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            Assert.That(so.PivotCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO();
            so.RecordLeadState(false);
            so.RecordLeadState(true);
            so.Reset();
            Assert.That(so.PivotCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.LeadEstablished,   Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PivotSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PivotSO, Is.Null);
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
            typeof(ZoneControlMatchPivotController)
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

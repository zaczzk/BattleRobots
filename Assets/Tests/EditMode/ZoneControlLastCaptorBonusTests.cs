using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T457: <see cref="ZoneControlLastCaptorBonusSO"/> and
    /// <see cref="ZoneControlLastCaptorBonusController"/>.
    ///
    /// ZoneControlLastCaptorBonusTests (12):
    ///   SO_FreshInstance_HasAnyCapture_False                                   x1
    ///   SO_FreshInstance_LastBonus_Zero                                        x1
    ///   SO_RecordPlayerCapture_SetsPlayerWasLast                               x1
    ///   SO_RecordBotCapture_ClearsPlayerWasLast                                x1
    ///   SO_ApplyMatchEndBonus_PlayerLast_AwardsBonus                           x1
    ///   SO_ApplyMatchEndBonus_BotLast_NoBonus                                  x1
    ///   SO_ApplyMatchEndBonus_NoCaptures_NoBonus                               x1
    ///   SO_ApplyMatchEndBonus_AccumulatesTotalBonus                            x1
    ///   SO_Reset_ClearsAll                                                     x1
    ///   Controller_FreshInstance_LastCaptorSO_Null                             x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                              x1
    ///   Controller_Refresh_NullSO_HidesPanel                                   x1
    /// </summary>
    public sealed class ZoneControlLastCaptorBonusTests
    {
        private static ZoneControlLastCaptorBonusSO CreateSO(int bonusAmount = 250)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlLastCaptorBonusSO>();
            typeof(ZoneControlLastCaptorBonusSO)
                .GetField("_bonusAmount", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusAmount);
            so.Reset();
            return so;
        }

        private static ZoneControlLastCaptorBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlLastCaptorBonusController>();
        }

        [Test]
        public void SO_FreshInstance_HasAnyCapture_False()
        {
            var so = CreateSO();
            Assert.That(so.HasAnyCapture, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LastBonus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LastBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SetsPlayerWasLast()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerWasLast, Is.True);
            Assert.That(so.HasAnyCapture, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClearsPlayerWasLast()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PlayerWasLast, Is.False);
            Assert.That(so.HasAnyCapture, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyMatchEndBonus_PlayerLast_AwardsBonus()
        {
            var so = CreateSO(bonusAmount: 250);
            so.RecordPlayerCapture();
            int bonus = so.ApplyMatchEndBonus();
            Assert.That(bonus,       Is.EqualTo(250));
            Assert.That(so.LastBonus, Is.EqualTo(250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyMatchEndBonus_BotLast_NoBonus()
        {
            var so = CreateSO(bonusAmount: 250);
            so.RecordBotCapture();
            int bonus = so.ApplyMatchEndBonus();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyMatchEndBonus_NoCaptures_NoBonus()
        {
            var so = CreateSO(bonusAmount: 250);
            int bonus = so.ApplyMatchEndBonus();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyMatchEndBonus_AccumulatesTotalBonus()
        {
            var so = CreateSO(bonusAmount: 250);
            so.RecordPlayerCapture();
            so.ApplyMatchEndBonus();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bonusAmount: 250);
            so.RecordPlayerCapture();
            so.ApplyMatchEndBonus();
            so.Reset();
            Assert.That(so.HasAnyCapture,    Is.False);
            Assert.That(so.PlayerWasLast,    Is.False);
            Assert.That(so.LastBonus,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LastCaptorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LastCaptorSO, Is.Null);
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
            typeof(ZoneControlLastCaptorBonusController)
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

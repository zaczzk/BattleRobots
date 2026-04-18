using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T410: <see cref="ZoneControlZoneFlipBonusSO"/> and
    /// <see cref="ZoneControlZoneFlipBonusController"/>.
    ///
    /// ZoneControlZoneFlipBonusTests (12):
    ///   SO_FreshInstance_FlipCount_Zero                              x1
    ///   SO_FreshInstance_TotalBonusAwarded_Zero                      x1
    ///   SO_RecordPlayerCapture_WithoutBotCapture_NoFlip              x1
    ///   SO_RecordBotCapture_ThenPlayerCapture_CountsFlip             x1
    ///   SO_RecordBotCapture_ThenPlayerCapture_AccumulatesBonus       x1
    ///   SO_RecordBotCapture_ThenPlayerCapture_FiresEvent             x1
    ///   SO_MultipleFlips_AccumulatesCorrectly                        x1
    ///   SO_Reset_ClearsAll                                           x1
    ///   Controller_FreshInstance_FlipBonusSO_Null                    x1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                    x1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                   x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlZoneFlipBonusTests
    {
        private static ZoneControlZoneFlipBonusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneFlipBonusSO>();

        private static ZoneControlZoneFlipBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneFlipBonusController>();
        }

        [Test]
        public void SO_FreshInstance_FlipCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FlipCount, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_WithoutBotCapture_NoFlip()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.FlipCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ThenPlayerCapture_CountsFlip()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.FlipCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ThenPlayerCapture_AccumulatesBonus()
        {
            var so     = CreateSO();
            int bonus  = so.BonusPerFlip;
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(bonus));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ThenPlayerCapture_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneFlipBonusSO)
                .GetField("_onFlipBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_MultipleFlips_AccumulatesCorrectly()
        {
            var so    = CreateSO();
            int bonus = so.BonusPerFlip;
            for (int i = 0; i < 3; i++)
            {
                so.RecordBotCapture();
                so.RecordPlayerCapture();
            }
            Assert.That(so.FlipCount, Is.EqualTo(3));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(bonus * 3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.FlipCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.BotCapturedLast,   Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FlipBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FlipBonusSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
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
            typeof(ZoneControlZoneFlipBonusController)
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

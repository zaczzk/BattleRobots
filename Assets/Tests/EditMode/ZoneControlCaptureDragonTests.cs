using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDragonTests
    {
        private static ZoneControlCaptureDragonSO CreateSO(
            int hoardNeeded   = 7,
            int plunderPerBot = 2,
            int bonusPerHoard = 610)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDragonSO>();
            typeof(ZoneControlCaptureDragonSO)
                .GetField("_hoardNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, hoardNeeded);
            typeof(ZoneControlCaptureDragonSO)
                .GetField("_plunderPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, plunderPerBot);
            typeof(ZoneControlCaptureDragonSO)
                .GetField("_bonusPerHoard", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerHoard);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDragonController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDragonController>();
        }

        [Test]
        public void SO_FreshInstance_Gold_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Gold, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HoardCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HoardCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGold()
        {
            var so = CreateSO(hoardNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Gold, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_HoardsAtThreshold()
        {
            var so    = CreateSO(hoardNeeded: 3, bonusPerHoard: 610);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(610));
            Assert.That(so.HoardCount,  Is.EqualTo(1));
            Assert.That(so.Gold,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(hoardNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_PlundersGold()
        {
            var so = CreateSO(hoardNeeded: 7, plunderPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gold, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(hoardNeeded: 7, plunderPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Gold, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GoldProgress_Clamped()
        {
            var so = CreateSO(hoardNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.GoldProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHoardFilled_FiresEvent()
        {
            var so    = CreateSO(hoardNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDragonSO)
                .GetField("_onHoardFilled", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(hoardNeeded: 2, bonusPerHoard: 610);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Gold,              Is.EqualTo(0));
            Assert.That(so.HoardCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleHoards_Accumulate()
        {
            var so = CreateSO(hoardNeeded: 2, bonusPerHoard: 610);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.HoardCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1220));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DragonSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DragonSO, Is.Null);
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
            typeof(ZoneControlCaptureDragonController)
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

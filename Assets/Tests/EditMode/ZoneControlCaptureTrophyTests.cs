using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTrophyTests
    {
        private static ZoneControlCaptureTrophySO CreateSO(
            int medalsNeeded   = 5,
            int losePerBot     = 1,
            int bonusPerTrophy = 685)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTrophySO>();
            typeof(ZoneControlCaptureTrophySO)
                .GetField("_medalsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, medalsNeeded);
            typeof(ZoneControlCaptureTrophySO)
                .GetField("_losePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, losePerBot);
            typeof(ZoneControlCaptureTrophySO)
                .GetField("_bonusPerTrophy", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTrophy);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTrophyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTrophyController>();
        }

        [Test]
        public void SO_FreshInstance_Medals_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Medals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TrophyCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TrophyCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMedals()
        {
            var so = CreateSO(medalsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Medals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AwardsAtThreshold()
        {
            var so    = CreateSO(medalsNeeded: 3, bonusPerTrophy: 685);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(685));
            Assert.That(so.TrophyCount,  Is.EqualTo(1));
            Assert.That(so.Medals,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(medalsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesMedals()
        {
            var so = CreateSO(medalsNeeded: 5, losePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Medals, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(medalsNeeded: 5, losePerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Medals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MedalProgress_Clamped()
        {
            var so = CreateSO(medalsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.MedalProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTrophyAwarded_FiresEvent()
        {
            var so    = CreateSO(medalsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTrophySO)
                .GetField("_onTrophyAwarded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(medalsNeeded: 2, bonusPerTrophy: 685);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Medals,            Is.EqualTo(0));
            Assert.That(so.TrophyCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTrophies_Accumulate()
        {
            var so = CreateSO(medalsNeeded: 2, bonusPerTrophy: 685);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TrophyCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1370));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TrophySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TrophySO, Is.Null);
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
            typeof(ZoneControlCaptureTrophyController)
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

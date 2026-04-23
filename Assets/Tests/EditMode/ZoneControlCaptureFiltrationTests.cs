using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFiltrationTests
    {
        private static ZoneControlCaptureFiltrationSO CreateSO(
            int levelsNeeded      = 7,
            int collapsePerBot    = 2,
            int bonusPerFiltration = 2695)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFiltrationSO>();
            typeof(ZoneControlCaptureFiltrationSO)
                .GetField("_levelsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, levelsNeeded);
            typeof(ZoneControlCaptureFiltrationSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureFiltrationSO)
                .GetField("_bonusPerFiltration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFiltration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFiltrationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFiltrationController>();
        }

        [Test]
        public void SO_FreshInstance_Levels_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Levels, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FiltrationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FiltrationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLevels()
        {
            var so = CreateSO(levelsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Levels, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(levelsNeeded: 3, bonusPerFiltration: 2695);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(2695));
            Assert.That(so.FiltrationCount,  Is.EqualTo(1));
            Assert.That(so.Levels,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(levelsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesLevels()
        {
            var so = CreateSO(levelsNeeded: 7, collapsePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Levels, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(levelsNeeded: 7, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Levels, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LevelProgress_Clamped()
        {
            var so = CreateSO(levelsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.LevelProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFiltrationAscended_FiresEvent()
        {
            var so    = CreateSO(levelsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFiltrationSO)
                .GetField("_onFiltrationAscended", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(levelsNeeded: 2, bonusPerFiltration: 2695);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Levels,            Is.EqualTo(0));
            Assert.That(so.FiltrationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFiltrations_Accumulate()
        {
            var so = CreateSO(levelsNeeded: 2, bonusPerFiltration: 2695);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FiltrationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5390));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FiltrationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FiltrationSO, Is.Null);
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
            typeof(ZoneControlCaptureFiltrationController)
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

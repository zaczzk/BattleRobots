using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSurgeWindowTests
    {
        private static ZoneControlCaptureSurgeWindowSO CreateSO(
            int botTrigger = 3, int surgeCaptures = 3, int bonusPerSurge = 110)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSurgeWindowSO>();
            typeof(ZoneControlCaptureSurgeWindowSO)
                .GetField("_botTriggerCount", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, botTrigger);
            typeof(ZoneControlCaptureSurgeWindowSO)
                .GetField("_surgePlayerCaptures", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, surgeCaptures);
            typeof(ZoneControlCaptureSurgeWindowSO)
                .GetField("_bonusPerSurgeCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSurge);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSurgeWindowController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSurgeWindowController>();
        }

        [Test]
        public void SO_FreshInstance_SurgeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SurgeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsSurgeActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsSurgeActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BotStreak_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BotStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotStreak()
        {
            var so = CreateSO(botTrigger: 3);
            so.RecordBotCapture();
            Assert.That(so.BotStreak, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtTrigger_OpensSurge()
        {
            var so = CreateSO(botTrigger: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsSurgeActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenNotSurge_ReturnsZero()
        {
            var so = CreateSO();
            Assert.That(so.RecordPlayerCapture(), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DuringSurge_ReturnsBonus()
        {
            var so = CreateSO(botTrigger: 1, bonusPerSurge: 110);
            so.RecordBotCapture();
            Assert.That(so.RecordPlayerCapture(), Is.EqualTo(110));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FillsSurge_ClosesSurge()
        {
            var so = CreateSO(botTrigger: 1, surgeCaptures: 2);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsSurgeActive, Is.False);
            Assert.That(so.SurgeCount,    Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DuringSurge_ClosesSurge()
        {
            var so = CreateSO(botTrigger: 1);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsSurgeActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OutsideSurge_ResetsBotStreak()
        {
            var so = CreateSO(botTrigger: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BotStreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(botTrigger: 1, surgeCaptures: 1, bonusPerSurge: 110);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SurgeCount,          Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Assert.That(so.IsSurgeActive,       Is.False);
            Assert.That(so.BotStreak,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SurgeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SurgeSO, Is.Null);
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
            typeof(ZoneControlCaptureSurgeWindowController)
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

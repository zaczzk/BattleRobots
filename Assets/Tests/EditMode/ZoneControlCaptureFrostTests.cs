using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFrostTests
    {
        private static ZoneControlCaptureFrostSO CreateSO(int freezeThreshold = 3, int thawCaptures = 2)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFrostSO>();
            typeof(ZoneControlCaptureFrostSO)
                .GetField("_freezeThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, freezeThreshold);
            typeof(ZoneControlCaptureFrostSO)
                .GetField("_thawCaptures", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, thawCaptures);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFrostController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFrostController>();
        }

        [Test]
        public void SO_FreshInstance_IsFrozen_False()
        {
            var so = CreateSO();
            Assert.That(so.IsFrozen, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCaptures_BelowThreshold_NotFrozen()
        {
            var so = CreateSO(freezeThreshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsFrozen, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCaptures_AtThreshold_IsFrozen()
        {
            var so = CreateSO(freezeThreshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsFrozen, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Frozen_RecordBotCapture_HasNoEffect()
        {
            var so = CreateSO(freezeThreshold: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            int consecutive = so.ConsecutiveBotCaptures;
            so.RecordBotCapture();
            Assert.That(so.ConsecutiveBotCaptures, Is.EqualTo(consecutive));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Frozen_PlayerCaptures_ThawsWhenThresholdMet()
        {
            var so = CreateSO(freezeThreshold: 2, thawCaptures: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsFrozen, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCapture_ResetsConsecutiveBotCount()
        {
            var so = CreateSO(freezeThreshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ConsecutiveBotCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalFreezes_IncrementedOnFreeze()
        {
            var so = CreateSO(freezeThreshold: 1, thawCaptures: 1);
            so.RecordBotCapture();
            Assert.That(so.TotalFreezes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FrostProgress_WhenFrozen_UsesThawProgress()
        {
            var so = CreateSO(freezeThreshold: 2, thawCaptures: 4);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsFrozen, Is.True);
            Assert.That(so.FrostProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(freezeThreshold: 1);
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.IsFrozen,               Is.False);
            Assert.That(so.ConsecutiveBotCaptures, Is.EqualTo(0));
            Assert.That(so.ThawProgress,           Is.EqualTo(0));
            Assert.That(so.TotalFreezes,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FrostSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FrostSO, Is.Null);
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
            typeof(ZoneControlCaptureFrostController)
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

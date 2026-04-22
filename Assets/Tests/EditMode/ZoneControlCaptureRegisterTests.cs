using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRegisterTests
    {
        private static ZoneControlCaptureRegisterSO CreateSO(
            int wordsNeeded   = 4,
            int clearPerBot   = 1,
            int bonusPerWrite = 1855)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRegisterSO>();
            typeof(ZoneControlCaptureRegisterSO)
                .GetField("_wordsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wordsNeeded);
            typeof(ZoneControlCaptureRegisterSO)
                .GetField("_clearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, clearPerBot);
            typeof(ZoneControlCaptureRegisterSO)
                .GetField("_bonusPerWrite", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerWrite);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRegisterController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRegisterController>();
        }

        [Test]
        public void SO_FreshInstance_Words_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Words, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WriteCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WriteCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWords()
        {
            var so = CreateSO(wordsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Words, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(wordsNeeded: 3, bonusPerWrite: 1855);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1855));
            Assert.That(so.WriteCount,  Is.EqualTo(1));
            Assert.That(so.Words,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(wordsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesWords()
        {
            var so = CreateSO(wordsNeeded: 4, clearPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Words, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(wordsNeeded: 4, clearPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Words, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WordProgress_Clamped()
        {
            var so = CreateSO(wordsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.WordProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRegisterWritten_FiresEvent()
        {
            var so    = CreateSO(wordsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRegisterSO)
                .GetField("_onRegisterWritten", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(wordsNeeded: 2, bonusPerWrite: 1855);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Words,            Is.EqualTo(0));
            Assert.That(so.WriteCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleWrites_Accumulate()
        {
            var so = CreateSO(wordsNeeded: 2, bonusPerWrite: 1855);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.WriteCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3710));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RegisterSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RegisterSO, Is.Null);
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
            typeof(ZoneControlCaptureRegisterController)
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

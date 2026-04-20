using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRuneTests
    {
        private static ZoneControlCaptureRuneSO CreateSO(int runeValue = 20, int maxRunes = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRuneSO>();
            typeof(ZoneControlCaptureRuneSO)
                .GetField("_runeValue", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, runeValue);
            typeof(ZoneControlCaptureRuneSO)
                .GetField("_maxRunes", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxRunes);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRuneController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRuneController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentRunes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentRunes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FirstCapture_AddsRune()
        {
            var so = CreateSO(runeValue: 10, maxRunes: 3);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentRunes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsRuneValueTimesCurrentRunes()
        {
            var so    = CreateSO(runeValue: 10, maxRunes: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(10));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ScalesWithRunes()
        {
            var so = CreateSO(runeValue: 10, maxRunes: 5);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(20));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClampsAtMaxRunes()
        {
            var so = CreateSO(runeValue: 5, maxRunes: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentRunes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtMax_StillReturnsBonus()
        {
            var so = CreateSO(runeValue: 10, maxRunes: 1);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(10));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresOnRuneScribed()
        {
            var so    = CreateSO(maxRunes: 3);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRuneSO)
                .GetField("_onRuneScribed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ErasesOneRune()
        {
            var so = CreateSO(maxRunes: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentRunes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsAtZero()
        {
            var so = CreateSO(maxRunes: 5);
            so.RecordBotCapture();
            Assert.That(so.CurrentRunes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RuneProgress_ReflectsRatio()
        {
            var so = CreateSO(maxRunes: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.RuneProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(runeValue: 10, maxRunes: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentRunes,      Is.EqualTo(0));
            Assert.That(so.RuneCaptures,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RuneSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RuneSO, Is.Null);
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
            typeof(ZoneControlCaptureRuneController)
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

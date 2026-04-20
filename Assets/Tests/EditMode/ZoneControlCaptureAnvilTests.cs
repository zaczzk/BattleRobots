using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAnvilTests
    {
        private static ZoneControlCaptureAnvilSO CreateSO(
            int strikesPerBlow = 3, int maxBlows = 5, int bonusPerBlow = 80)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAnvilSO>();
            typeof(ZoneControlCaptureAnvilSO)
                .GetField("_strikesPerBlow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, strikesPerBlow);
            typeof(ZoneControlCaptureAnvilSO)
                .GetField("_maxBlows",       BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxBlows);
            typeof(ZoneControlCaptureAnvilSO)
                .GetField("_bonusPerBlow",   BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBlow);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAnvilController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAnvilController>();
        }

        [Test]
        public void SO_FreshInstance_StrikeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StrikeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BlowCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BlowCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowStrikeThreshold_ReturnsZero()
        {
            var so    = CreateSO(strikesPerBlow: 3);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowStrikeThreshold_IncrementsStrikeCount()
        {
            var so = CreateSO(strikesPerBlow: 3);
            so.RecordPlayerCapture();
            Assert.That(so.StrikeCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesStrikeThreshold_IncreasesBlows()
        {
            var so = CreateSO(strikesPerBlow: 2, maxBlows: 5, bonusPerBlow: 80);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(so.BlowCount, Is.EqualTo(1));
            Assert.That(bonus, Is.EqualTo(80));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SecondBlow_EscalatesBonus()
        {
            var so = CreateSO(strikesPerBlow: 1, maxBlows: 5, bonusPerBlow: 80);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(so.BlowCount, Is.EqualTo(2));
            Assert.That(bonus, Is.EqualTo(160));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BlowsCappedAtMax()
        {
            var so = CreateSO(strikesPerBlow: 1, maxBlows: 2, bonusPerBlow: 50);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BlowCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Blow_FiresEvent()
        {
            var so    = CreateSO(strikesPerBlow: 1, maxBlows: 5);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAnvilSO)
                .GetField("_onBlow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsStrikesAndBlows()
        {
            var so = CreateSO(strikesPerBlow: 1, maxBlows: 5, bonusPerBlow: 80);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StrikeCount, Is.EqualTo(0));
            Assert.That(so.BlowCount,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AnvilProgress_ReflectsStrikeRatio()
        {
            var so = CreateSO(strikesPerBlow: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.AnvilProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(strikesPerBlow: 1, maxBlows: 5, bonusPerBlow: 80);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.StrikeCount,       Is.EqualTo(0));
            Assert.That(so.BlowCount,          Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AnvilSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AnvilSO, Is.Null);
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
            typeof(ZoneControlCaptureAnvilController)
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

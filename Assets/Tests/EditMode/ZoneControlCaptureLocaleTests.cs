using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLocaleTests
    {
        private static ZoneControlCaptureLocaleSO CreateSO(
            int opensNeeded   = 7,
            int closePerBot   = 2,
            int bonusPerCover = 3295)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLocaleSO>();
            typeof(ZoneControlCaptureLocaleSO)
                .GetField("_opensNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, opensNeeded);
            typeof(ZoneControlCaptureLocaleSO)
                .GetField("_closePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, closePerBot);
            typeof(ZoneControlCaptureLocaleSO)
                .GetField("_bonusPerCover", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCover);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLocaleController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLocaleController>();
        }

        [Test]
        public void SO_FreshInstance_Opens_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Opens, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CoverCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CoverCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesOpens()
        {
            var so = CreateSO(opensNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Opens, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(opensNeeded: 3, bonusPerCover: 3295);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3295));
            Assert.That(so.CoverCount,  Is.EqualTo(1));
            Assert.That(so.Opens,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(opensNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesOpens()
        {
            var so = CreateSO(opensNeeded: 7, closePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Opens, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(opensNeeded: 7, closePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Opens, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OpenProgress_Clamped()
        {
            var so = CreateSO(opensNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.OpenProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLocaleCovered_FiresEvent()
        {
            var so    = CreateSO(opensNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLocaleSO)
                .GetField("_onLocaleCovered", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(opensNeeded: 2, bonusPerCover: 3295);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Opens,             Is.EqualTo(0));
            Assert.That(so.CoverCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCoverings_Accumulate()
        {
            var so = CreateSO(opensNeeded: 2, bonusPerCover: 3295);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CoverCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6590));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LocaleSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LocaleSO, Is.Null);
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
            typeof(ZoneControlCaptureLocaleController)
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

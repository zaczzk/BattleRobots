using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSheaveTests
    {
        private static ZoneControlCaptureSheaveSO CreateSO(
            int sectionsNeeded = 5,
            int restrictPerBot = 1,
            int bonusPerGluing = 3445)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSheaveSO>();
            typeof(ZoneControlCaptureSheaveSO)
                .GetField("_sectionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, sectionsNeeded);
            typeof(ZoneControlCaptureSheaveSO)
                .GetField("_restrictPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, restrictPerBot);
            typeof(ZoneControlCaptureSheaveSO)
                .GetField("_bonusPerGluing", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGluing);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSheaveController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSheaveController>();
        }

        [Test]
        public void SO_FreshInstance_Sections_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Sections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GluingCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GluingCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSections()
        {
            var so = CreateSO(sectionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Sections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(sectionsNeeded: 3, bonusPerGluing: 3445);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3445));
            Assert.That(so.GluingCount, Is.EqualTo(1));
            Assert.That(so.Sections,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(sectionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RestrictsSections()
        {
            var so = CreateSO(sectionsNeeded: 5, restrictPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Sections, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(sectionsNeeded: 5, restrictPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Sections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SheaveProgress_Clamped()
        {
            var so = CreateSO(sectionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SheaveProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSheaveGlued_FiresEvent()
        {
            var so    = CreateSO(sectionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSheaveSO)
                .GetField("_onSheaveGlued", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(sectionsNeeded: 2, bonusPerGluing: 3445);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Sections,         Is.EqualTo(0));
            Assert.That(so.GluingCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGluings_Accumulate()
        {
            var so = CreateSO(sectionsNeeded: 2, bonusPerGluing: 3445);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GluingCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6890));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SheaveSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SheaveSO, Is.Null);
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
            typeof(ZoneControlCaptureSheaveController)
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

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSheafTests
    {
        private static ZoneControlCaptureSheafSO CreateSO(
            int sectionsNeeded = 7,
            int tearPerBot     = 2,
            int bonusPerGlue   = 2515)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSheafSO>();
            typeof(ZoneControlCaptureSheafSO)
                .GetField("_sectionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, sectionsNeeded);
            typeof(ZoneControlCaptureSheafSO)
                .GetField("_tearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tearPerBot);
            typeof(ZoneControlCaptureSheafSO)
                .GetField("_bonusPerGlue", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGlue);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSheafController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSheafController>();
        }

        [Test]
        public void SO_FreshInstance_Sections_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Sections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GlueCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GlueCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSections()
        {
            var so = CreateSO(sectionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Sections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(sectionsNeeded: 3, bonusPerGlue: 2515);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2515));
            Assert.That(so.GlueCount,   Is.EqualTo(1));
            Assert.That(so.Sections,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(sectionsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSections()
        {
            var so = CreateSO(sectionsNeeded: 7, tearPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Sections, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(sectionsNeeded: 7, tearPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Sections, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SectionProgress_Clamped()
        {
            var so = CreateSO(sectionsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.SectionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSheafGlued_FiresEvent()
        {
            var so    = CreateSO(sectionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSheafSO)
                .GetField("_onSheafGlued", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(sectionsNeeded: 2, bonusPerGlue: 2515);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Sections,          Is.EqualTo(0));
            Assert.That(so.GlueCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGlues_Accumulate()
        {
            var so = CreateSO(sectionsNeeded: 2, bonusPerGlue: 2515);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GlueCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5030));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SheafSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SheafSO, Is.Null);
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
            typeof(ZoneControlCaptureSheafController)
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

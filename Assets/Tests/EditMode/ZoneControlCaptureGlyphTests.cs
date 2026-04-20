using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGlyphTests
    {
        private static ZoneControlCaptureGlyphSO CreateSO(
            int glyphsNeeded       = 4,
            int bonusPerInscription = 200,
            int empoweredCaptures  = 3,
            int empoweredBonus     = 80)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGlyphSO>();
            typeof(ZoneControlCaptureGlyphSO)
                .GetField("_glyphsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, glyphsNeeded);
            typeof(ZoneControlCaptureGlyphSO)
                .GetField("_bonusPerInscription", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInscription);
            typeof(ZoneControlCaptureGlyphSO)
                .GetField("_empoweredCaptures", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, empoweredCaptures);
            typeof(ZoneControlCaptureGlyphSO)
                .GetField("_empoweredBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, empoweredBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGlyphController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGlyphController>();
        }

        [Test]
        public void SO_FreshInstance_InscriptionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InscriptionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsEmpowered_False()
        {
            var so = CreateSO();
            Assert.That(so.IsEmpowered, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(glyphsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_InscribesGlyph()
        {
            var so = CreateSO(glyphsNeeded: 3);
            for (int i = 0; i < 3; i++) so.RecordPlayerCapture();
            Assert.That(so.InscriptionCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_ReturnsBonusPerInscription()
        {
            var so = CreateSO(glyphsNeeded: 2, bonusPerInscription: 200);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhileEmpowered_ReturnsEmpoweredBonus()
        {
            var so = CreateSO(glyphsNeeded: 2, empoweredCaptures: 3, empoweredBonus: 80);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(80));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_EmpoweredExhausted_EndsEmpowerment()
        {
            var so = CreateSO(glyphsNeeded: 2, empoweredCaptures: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsEmpowered, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_NormalState_DrainsGlyphCount()
        {
            var so = CreateSO(glyphsNeeded: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.GlyphCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhileEmpowered_EndsEmpowerment()
        {
            var so = CreateSO(glyphsNeeded: 2, empoweredCaptures: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsEmpowered, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGlyphInscribed_FiresEvent()
        {
            var so    = CreateSO(glyphsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGlyphSO)
                .GetField("_onGlyphInscribed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_OnEmpowermentEnded_BotCapture_FiresEvent()
        {
            var so    = CreateSO(glyphsNeeded: 2, empoweredCaptures: 3);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGlyphSO)
                .GetField("_onEmpowermentEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(glyphsNeeded: 2, empoweredCaptures: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.GlyphCount,         Is.EqualTo(0));
            Assert.That(so.InscriptionCount,   Is.EqualTo(0));
            Assert.That(so.IsEmpowered,        Is.False);
            Assert.That(so.EmpoweredRemaining, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GlyphSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GlyphSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureGlyphController)
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

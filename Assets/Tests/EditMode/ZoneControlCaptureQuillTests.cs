using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureQuillTests
    {
        private static ZoneControlCaptureQuillSO CreateSO(
            int strokesNeeded      = 5,
            int blotPerBot         = 1,
            int bonusPerInscription = 820)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureQuillSO>();
            typeof(ZoneControlCaptureQuillSO)
                .GetField("_strokesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, strokesNeeded);
            typeof(ZoneControlCaptureQuillSO)
                .GetField("_blotPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, blotPerBot);
            typeof(ZoneControlCaptureQuillSO)
                .GetField("_bonusPerInscription", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInscription);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureQuillController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureQuillController>();
        }

        [Test]
        public void SO_FreshInstance_Strokes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Strokes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InscriptionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InscriptionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStrokes()
        {
            var so = CreateSO(strokesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Strokes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_InscribesAtThreshold()
        {
            var so    = CreateSO(strokesNeeded: 3, bonusPerInscription: 820);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(820));
            Assert.That(so.InscriptionCount, Is.EqualTo(1));
            Assert.That(so.Strokes,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(strokesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BlotsStrokes()
        {
            var so = CreateSO(strokesNeeded: 5, blotPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Strokes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(strokesNeeded: 5, blotPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Strokes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StrokeProgress_Clamped()
        {
            var so = CreateSO(strokesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StrokeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnQuillInscribed_FiresEvent()
        {
            var so    = CreateSO(strokesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureQuillSO)
                .GetField("_onQuillInscribed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(strokesNeeded: 2, bonusPerInscription: 820);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Strokes,           Is.EqualTo(0));
            Assert.That(so.InscriptionCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInscriptions_Accumulate()
        {
            var so = CreateSO(strokesNeeded: 2, bonusPerInscription: 820);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InscriptionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1640));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_QuillSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.QuillSO, Is.Null);
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
            typeof(ZoneControlCaptureQuillController)
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

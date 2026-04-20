using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureForgeTests
    {
        private static ZoneControlCaptureForgeSO CreateSO(
            int capturesPerIngot = 3, int ingotsForForge = 3, int bonusPerForge = 450)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureForgeSO>();
            typeof(ZoneControlCaptureForgeSO)
                .GetField("_capturesPerIngot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesPerIngot);
            typeof(ZoneControlCaptureForgeSO)
                .GetField("_ingotsForForge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ingotsForForge);
            typeof(ZoneControlCaptureForgeSO)
                .GetField("_bonusPerForge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerForge);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureForgeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureForgeController>();
        }

        [Test]
        public void SO_FreshInstance_ForgeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ForgeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IngotCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IngotCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRaw()
        {
            var so = CreateSO(capturesPerIngot: 3, ingotsForForge: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.RawCaptures, Is.EqualTo(2));
            Assert.That(so.IngotCount,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ConvertsRawToIngot()
        {
            var so = CreateSO(capturesPerIngot: 3, ingotsForForge: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.RawCaptures, Is.EqualTo(0));
            Assert.That(so.IngotCount,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IngotsForForge_TriggersForge()
        {
            var so = CreateSO(capturesPerIngot: 1, ingotsForForge: 3, bonusPerForge: 450);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ForgeCount,  Is.EqualTo(1));
            Assert.That(so.IngotCount,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Forge_AwardsBonusAndUpdatesTotal()
        {
            var so = CreateSO(capturesPerIngot: 1, ingotsForForge: 1, bonusPerForge: 450);
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(450));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Forge_FiresEvent()
        {
            var so    = CreateSO(capturesPerIngot: 1, ingotsForForge: 1, bonusPerForge: 450);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureForgeSO)
                .GetField("_onForge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ScattersRawAndIngots()
        {
            var so = CreateSO(capturesPerIngot: 3, ingotsForForge: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RawCaptures, Is.EqualTo(0));
            Assert.That(so.IngotCount,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesPerIngot: 1, ingotsForForge: 1, bonusPerForge: 450);
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RawCaptures,       Is.EqualTo(0));
            Assert.That(so.IngotCount,        Is.EqualTo(0));
            Assert.That(so.ForgeCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IngotProgress_CorrectFraction()
        {
            var so = CreateSO(capturesPerIngot: 1, ingotsForForge: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IngotProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ForgeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ForgeSO, Is.Null);
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
            typeof(ZoneControlCaptureForgeController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(true);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();
            typeof(ZoneControlCaptureForgeController)
                .GetField("_forgeSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureForgeController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}

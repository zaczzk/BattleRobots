using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSearchTests
    {
        private static ZoneControlCaptureSearchSO CreateSO(
            int probesNeeded = 7,
            int missPerBot   = 2,
            int bonusPerFind = 2095)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSearchSO>();
            typeof(ZoneControlCaptureSearchSO)
                .GetField("_probesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, probesNeeded);
            typeof(ZoneControlCaptureSearchSO)
                .GetField("_missPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, missPerBot);
            typeof(ZoneControlCaptureSearchSO)
                .GetField("_bonusPerFind", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFind);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSearchController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSearchController>();
        }

        [Test]
        public void SO_FreshInstance_Probes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Probes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FindCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FindCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesProbes()
        {
            var so = CreateSO(probesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Probes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(probesNeeded: 3, bonusPerFind: 2095);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(2095));
            Assert.That(so.FindCount,  Is.EqualTo(1));
            Assert.That(so.Probes,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(probesNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_MissesProbes()
        {
            var so = CreateSO(probesNeeded: 7, missPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Probes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(probesNeeded: 7, missPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Probes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ProbeProgress_Clamped()
        {
            var so = CreateSO(probesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.ProbeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTargetFound_FiresEvent()
        {
            var so    = CreateSO(probesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSearchSO)
                .GetField("_onTargetFound", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(probesNeeded: 2, bonusPerFind: 2095);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Probes,            Is.EqualTo(0));
            Assert.That(so.FindCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFinds_Accumulate()
        {
            var so = CreateSO(probesNeeded: 2, bonusPerFind: 2095);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FindCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4190));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SearchSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SearchSO, Is.Null);
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
            typeof(ZoneControlCaptureSearchController)
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

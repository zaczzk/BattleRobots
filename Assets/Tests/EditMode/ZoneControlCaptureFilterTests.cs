using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFilterTests
    {
        private static ZoneControlCaptureFilterSO CreateSO(
            int bandsNeeded    = 5,
            int noisePerBot    = 1,
            int bonusPerFilter = 1690)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFilterSO>();
            typeof(ZoneControlCaptureFilterSO)
                .GetField("_bandsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bandsNeeded);
            typeof(ZoneControlCaptureFilterSO)
                .GetField("_noisePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, noisePerBot);
            typeof(ZoneControlCaptureFilterSO)
                .GetField("_bonusPerFilter", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFilter);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFilterController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFilterController>();
        }

        [Test]
        public void SO_FreshInstance_Bands_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bands, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FilterCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FilterCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBands()
        {
            var so = CreateSO(bandsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Bands, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bandsNeeded: 3, bonusPerFilter: 1690);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1690));
            Assert.That(so.FilterCount, Is.EqualTo(1));
            Assert.That(so.Bands,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bandsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBands()
        {
            var so = CreateSO(bandsNeeded: 5, noisePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bands, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bandsNeeded: 5, noisePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bands, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BandProgress_Clamped()
        {
            var so = CreateSO(bandsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BandProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFilterApplied_FiresEvent()
        {
            var so    = CreateSO(bandsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFilterSO)
                .GetField("_onFilterApplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bandsNeeded: 2, bonusPerFilter: 1690);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bands,             Is.EqualTo(0));
            Assert.That(so.FilterCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFilters_Accumulate()
        {
            var so = CreateSO(bandsNeeded: 2, bonusPerFilter: 1690);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FilterCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3380));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FilterSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FilterSO, Is.Null);
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
            typeof(ZoneControlCaptureFilterController)
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

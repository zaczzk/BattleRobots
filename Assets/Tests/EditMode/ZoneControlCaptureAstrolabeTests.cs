using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAstrolabeTests
    {
        private static ZoneControlCaptureAstrolabeSO CreateSO(
            int chartingsNeeded   = 5,
            int driftPerBot       = 1,
            int bonusPerAlignment = 985)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAstrolabeSO>();
            typeof(ZoneControlCaptureAstrolabeSO)
                .GetField("_chartingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chartingsNeeded);
            typeof(ZoneControlCaptureAstrolabeSO)
                .GetField("_driftPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, driftPerBot);
            typeof(ZoneControlCaptureAstrolabeSO)
                .GetField("_bonusPerAlignment", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAlignment);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAstrolabeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAstrolabeController>();
        }

        [Test]
        public void SO_FreshInstance_Chartings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Chartings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AlignmentCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AlignmentCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesChartings()
        {
            var so = CreateSO(chartingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Chartings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ChartingsAtThreshold()
        {
            var so    = CreateSO(chartingsNeeded: 3, bonusPerAlignment: 985);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(985));
            Assert.That(so.AlignmentCount,   Is.EqualTo(1));
            Assert.That(so.Chartings,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(chartingsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesChartings()
        {
            var so = CreateSO(chartingsNeeded: 5, driftPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Chartings, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chartingsNeeded: 5, driftPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Chartings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChartingProgress_Clamped()
        {
            var so = CreateSO(chartingsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ChartingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAstrolabeAligned_FiresEvent()
        {
            var so    = CreateSO(chartingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAstrolabeSO)
                .GetField("_onAstrolabeAligned", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chartingsNeeded: 2, bonusPerAlignment: 985);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Chartings,         Is.EqualTo(0));
            Assert.That(so.AlignmentCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAlignments_Accumulate()
        {
            var so = CreateSO(chartingsNeeded: 2, bonusPerAlignment: 985);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AlignmentCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1970));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AstrolabeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AstrolabeSO, Is.Null);
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
            typeof(ZoneControlCaptureAstrolabeController)
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

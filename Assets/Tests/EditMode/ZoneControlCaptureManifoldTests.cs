using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureManifoldTests
    {
        private static ZoneControlCaptureManifoldSO CreateSO(
            int chartsNeeded  = 6,
            int wrinklePerBot = 1,
            int bonusPerAtlas = 3520)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureManifoldSO>();
            typeof(ZoneControlCaptureManifoldSO)
                .GetField("_chartsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chartsNeeded);
            typeof(ZoneControlCaptureManifoldSO)
                .GetField("_wrinklePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, wrinklePerBot);
            typeof(ZoneControlCaptureManifoldSO)
                .GetField("_bonusPerAtlas", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAtlas);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureManifoldController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureManifoldController>();
        }

        [Test]
        public void SO_FreshInstance_Charts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Charts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AtlasCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AtlasCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCharts()
        {
            var so = CreateSO(chartsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Charts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(chartsNeeded: 3, bonusPerAtlas: 3520);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(3520));
            Assert.That(so.AtlasCount, Is.EqualTo(1));
            Assert.That(so.Charts,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(chartsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WrinklesCharts()
        {
            var so = CreateSO(chartsNeeded: 6, wrinklePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charts, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chartsNeeded: 6, wrinklePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ManifoldProgress_Clamped()
        {
            var so = CreateSO(chartsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ManifoldProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAtlasFormed_FiresEvent()
        {
            var so    = CreateSO(chartsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureManifoldSO)
                .GetField("_onAtlasFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chartsNeeded: 2, bonusPerAtlas: 3520);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Charts,            Is.EqualTo(0));
            Assert.That(so.AtlasCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAtlases_Accumulate()
        {
            var so = CreateSO(chartsNeeded: 2, bonusPerAtlas: 3520);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AtlasCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7040));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ManifoldSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ManifoldSO, Is.Null);
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
            typeof(ZoneControlCaptureManifoldController)
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

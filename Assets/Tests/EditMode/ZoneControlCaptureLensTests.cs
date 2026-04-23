using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLensTests
    {
        private static ZoneControlCaptureLensSO CreateSO(
            int facetsNeeded  = 5,
            int scatterPerBot = 1,
            int bonusPerView  = 2365)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLensSO>();
            typeof(ZoneControlCaptureLensSO)
                .GetField("_facetsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, facetsNeeded);
            typeof(ZoneControlCaptureLensSO)
                .GetField("_scatterPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, scatterPerBot);
            typeof(ZoneControlCaptureLensSO)
                .GetField("_bonusPerView", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerView);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLensController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLensController>();
        }

        [Test]
        public void SO_FreshInstance_Facets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Facets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ViewCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ViewCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFacets()
        {
            var so = CreateSO(facetsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Facets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(facetsNeeded: 3, bonusPerView: 2365);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(2365));
            Assert.That(so.ViewCount,  Is.EqualTo(1));
            Assert.That(so.Facets,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(facetsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFacets()
        {
            var so = CreateSO(facetsNeeded: 5, scatterPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Facets, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(facetsNeeded: 5, scatterPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Facets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FacetProgress_Clamped()
        {
            var so = CreateSO(facetsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FacetProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLensViewed_FiresEvent()
        {
            var so    = CreateSO(facetsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLensSO)
                .GetField("_onLensViewed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(facetsNeeded: 2, bonusPerView: 2365);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Facets,            Is.EqualTo(0));
            Assert.That(so.ViewCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleViews_Accumulate()
        {
            var so = CreateSO(facetsNeeded: 2, bonusPerView: 2365);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ViewCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4730));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LensSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LensSO, Is.Null);
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
            typeof(ZoneControlCaptureLensController)
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

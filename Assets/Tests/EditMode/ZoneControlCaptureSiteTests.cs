using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSiteTests
    {
        private static ZoneControlCaptureSiteSO CreateSO(
            int coveringsNeeded  = 6,
            int sievePerBot      = 1,
            int bonusPerCovering = 3460)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSiteSO>();
            typeof(ZoneControlCaptureSiteSO)
                .GetField("_coveringsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coveringsNeeded);
            typeof(ZoneControlCaptureSiteSO)
                .GetField("_sievePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, sievePerBot);
            typeof(ZoneControlCaptureSiteSO)
                .GetField("_bonusPerCovering", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCovering);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSiteController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSiteController>();
        }

        [Test]
        public void SO_FreshInstance_Coverings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Coverings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CoveringCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CoveringCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCoverings()
        {
            var so = CreateSO(coveringsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Coverings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(coveringsNeeded: 3, bonusPerCovering: 3460);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(3460));
            Assert.That(so.CoveringCount, Is.EqualTo(1));
            Assert.That(so.Coverings,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(coveringsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SievesCoverings()
        {
            var so = CreateSO(coveringsNeeded: 6, sievePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Coverings, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(coveringsNeeded: 6, sievePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Coverings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SiteProgress_Clamped()
        {
            var so = CreateSO(coveringsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SiteProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSiteCovered_FiresEvent()
        {
            var so    = CreateSO(coveringsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSiteSO)
                .GetField("_onSiteCovered", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(coveringsNeeded: 2, bonusPerCovering: 3460);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Coverings,         Is.EqualTo(0));
            Assert.That(so.CoveringCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCoverings_Accumulate()
        {
            var so = CreateSO(coveringsNeeded: 2, bonusPerCovering: 3460);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CoveringCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6920));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SiteSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SiteSO, Is.Null);
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
            typeof(ZoneControlCaptureSiteController)
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

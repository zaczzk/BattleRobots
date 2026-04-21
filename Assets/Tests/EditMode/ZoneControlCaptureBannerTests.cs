using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBannerTests
    {
        private static ZoneControlCaptureBannerSO CreateSO(
            int emblemsNeeded  = 5,
            int tearPerBot     = 1,
            int bonusPerBanner = 655)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBannerSO>();
            typeof(ZoneControlCaptureBannerSO)
                .GetField("_emblemsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, emblemsNeeded);
            typeof(ZoneControlCaptureBannerSO)
                .GetField("_tearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tearPerBot);
            typeof(ZoneControlCaptureBannerSO)
                .GetField("_bonusPerBanner", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBanner);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBannerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBannerController>();
        }

        [Test]
        public void SO_FreshInstance_Emblems_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Emblems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BannerCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BannerCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesEmblems()
        {
            var so = CreateSO(emblemsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Emblems, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RaisesAtThreshold()
        {
            var so    = CreateSO(emblemsNeeded: 3, bonusPerBanner: 655);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(655));
            Assert.That(so.BannerCount, Is.EqualTo(1));
            Assert.That(so.Emblems,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(emblemsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_TearsEmblems()
        {
            var so = CreateSO(emblemsNeeded: 5, tearPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Emblems, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(emblemsNeeded: 5, tearPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Emblems, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EmblemProgress_Clamped()
        {
            var so = CreateSO(emblemsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.EmblemProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnBannerRaised_FiresEvent()
        {
            var so    = CreateSO(emblemsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBannerSO)
                .GetField("_onBannerRaised", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(emblemsNeeded: 2, bonusPerBanner: 655);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Emblems,           Is.EqualTo(0));
            Assert.That(so.BannerCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleBanners_Accumulate()
        {
            var so = CreateSO(emblemsNeeded: 2, bonusPerBanner: 655);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.BannerCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1310));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BannerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BannerSO, Is.Null);
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
            typeof(ZoneControlCaptureBannerController)
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

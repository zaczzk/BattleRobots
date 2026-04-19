using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureStormTests
    {
        private static ZoneControlCaptureStormSO CreateSO(int chargesRequired = 6, float duration = 15f, int bonusPerCapture = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureStormSO>();
            typeof(ZoneControlCaptureStormSO)
                .GetField("_chargesRequired", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargesRequired);
            typeof(ZoneControlCaptureStormSO)
                .GetField("_stormDurationSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, duration);
            typeof(ZoneControlCaptureStormSO)
                .GetField("_bonusPerStormCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureStormController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureStormController>();
        }

        [Test]
        public void SO_FreshInstance_IsStormActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsStormActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_StormCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StormCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncrementsCharges()
        {
            var so = CreateSO(chargesRequired: 6);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.StormCharges, Is.EqualTo(2));
            Assert.That(so.IsStormActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_ActivatesStorm_WhenChargesMet()
        {
            var so = CreateSO(chargesRequired: 3);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.IsStormActive, Is.True);
            Assert.That(so.StormCount, Is.EqualTo(1));
            Assert.That(so.StormCharges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_DuringStorm_ReturnsBonus()
        {
            var so = CreateSO(chargesRequired: 1, bonusPerCapture: 50);
            so.RecordCapture();
            Assert.That(so.IsStormActive, Is.True);
            int bonus = so.RecordCapture();
            Assert.That(bonus, Is.EqualTo(50));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_DuringCharging_ReturnsZero()
        {
            var so    = CreateSO(chargesRequired: 6);
            int bonus = so.RecordCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_EndsStorm_WhenDurationExceeded()
        {
            var so = CreateSO(chargesRequired: 1, duration: 10f);
            so.RecordCapture();
            Assert.That(so.IsStormActive, Is.True);
            so.Tick(11f);
            Assert.That(so.IsStormActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_ZeroAtStart()
        {
            var so = CreateSO();
            Assert.That(so.ChargeProgress, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_IncreasesWithCaptures()
        {
            var so = CreateSO(chargesRequired: 4);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.ChargeProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(chargesRequired: 1);
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.IsStormActive,     Is.False);
            Assert.That(so.StormCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_StormSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.StormSO, Is.Null);
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
            typeof(ZoneControlCaptureStormController)
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

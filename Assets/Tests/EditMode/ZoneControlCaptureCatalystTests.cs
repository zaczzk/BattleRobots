using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCatalystTests
    {
        private static ZoneControlCaptureCatalystSO CreateSO(int activationCaptures = 3, int catalystDuration = 4, int bonus = 150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCatalystSO>();
            typeof(ZoneControlCaptureCatalystSO)
                .GetField("_capturesForActivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, activationCaptures);
            typeof(ZoneControlCaptureCatalystSO)
                .GetField("_catalystDurationCaptures", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, catalystDuration);
            typeof(ZoneControlCaptureCatalystSO)
                .GetField("_bonusPerCatalystCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCatalystController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCatalystController>();
        }

        [Test]
        public void SO_FreshInstance_IsCatalystActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsCatalystActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CatalystCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CatalystCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowActivation_ReturnsZero()
        {
            var so = CreateSO(activationCaptures: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsCatalystActive, Is.False);
            Assert.That(so.RecordPlayerCapture(), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesActivation_ActivatesCatalyst()
        {
            var so = CreateSO(activationCaptures: 3, catalystDuration: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsCatalystActive, Is.True);
            Assert.That(so.CatalystCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DuringCatalyst_ReturnsBonus()
        {
            var so = CreateSO(activationCaptures: 2, catalystDuration: 3, bonus: 200);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsCatalystActive, Is.True);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CatalystExpires_AfterDurationCaptures()
        {
            var so = CreateSO(activationCaptures: 2, catalystDuration: 2, bonus: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsCatalystActive, Is.True);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsCatalystActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsCharge()
        {
            var so = CreateSO(activationCaptures: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsCatalystActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Activation_FiresActivatedEvent()
        {
            var so    = CreateSO(activationCaptures: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCatalystSO)
                .GetField("_onCatalystActivated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_TotalBonusAwarded_Accumulates()
        {
            var so = CreateSO(activationCaptures: 1, catalystDuration: 3, bonus: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(activationCaptures: 2, catalystDuration: 4, bonus: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsCatalystActive,   Is.False);
            Assert.That(so.CatalystCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CatalystSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CatalystSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureCatalystController)
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

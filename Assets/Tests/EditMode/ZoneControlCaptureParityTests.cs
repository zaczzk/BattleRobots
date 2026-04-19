using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureParityTests
    {
        private static ZoneControlCaptureParitySO CreateSO(int bonus = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureParitySO>();
            typeof(ZoneControlCaptureParitySO)
                .GetField("_bonusPerParity", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureParityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureParityController>();
        }

        [Test]
        public void SO_FreshInstance_ParityCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ParityCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_NoBot_NoParity()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.ParityCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerAndBotEqual_FiresParity()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ParityCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Parity_Idempotent_SameCount()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            // Both equal at 1 — no second parity without a change
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ParityCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Parity_AccumulatesBonus()
        {
            var so = CreateSO(bonus: 150);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_EqualCount_FiresParity()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ParityCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AreTied_TrueWhenEqual()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.AreTied, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.ParityCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.PlayerCaptures,    Is.EqualTo(0));
            Assert.That(so.BotCaptures,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ParitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ParitySO, Is.Null);
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
            typeof(ZoneControlCaptureParityController)
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

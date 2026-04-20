using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureVaultTests
    {
        private static ZoneControlCaptureVaultSO CreateSO(
            int deposit = 50, float multiplier = 2f, int maxVault = 600)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureVaultSO>();
            typeof(ZoneControlCaptureVaultSO)
                .GetField("_depositPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, deposit);
            typeof(ZoneControlCaptureVaultSO)
                .GetField("_payoutMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, multiplier);
            typeof(ZoneControlCaptureVaultSO)
                .GetField("_maxVault", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxVault);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureVaultController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureVaultController>();
        }

        [Test]
        public void SO_FreshInstance_VaultBalance_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VaultBalance, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalPayouts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalPayouts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_VaultProgress_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VaultProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsVault()
        {
            var so = CreateSO(deposit: 50, maxVault: 600);
            so.RecordPlayerCapture();
            Assert.That(so.VaultBalance, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClampsToMaxVault()
        {
            var so = CreateSO(deposit: 100, maxVault: 150);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.VaultBalance, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_EmptyVault_ReturnsZero()
        {
            var so = CreateSO();
            Assert.That(so.RecordBotCapture(), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WithBalance_ReturnsScaledPayout()
        {
            var so = CreateSO(deposit: 100, multiplier: 3f, maxVault: 600);
            so.RecordPlayerCapture();
            int payout = so.RecordBotCapture();
            Assert.That(payout, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsVault()
        {
            var so = CreateSO(deposit: 50, maxVault: 600);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.VaultBalance, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresEvent()
        {
            var so    = CreateSO(deposit: 50, maxVault: 600);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureVaultSO)
                .GetField("_onVaultPayout", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsTotalPayouts()
        {
            var so = CreateSO(deposit: 50, maxVault: 600);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalPayouts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(deposit: 50, multiplier: 2f, maxVault: 600);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.VaultBalance,     Is.EqualTo(0));
            Assert.That(so.TotalPayouts,     Is.EqualTo(0));
            Assert.That(so.LastPayoutAmount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_VaultSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.VaultSO, Is.Null);
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
            typeof(ZoneControlCaptureVaultController)
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

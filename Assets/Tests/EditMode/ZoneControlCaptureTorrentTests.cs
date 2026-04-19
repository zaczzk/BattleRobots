using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTorrentTests
    {
        private static ZoneControlCaptureTorrentSO CreateSO(int buildPerCapture = 50, float payoutFraction = 0.75f, int maxPool = 1000)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTorrentSO>();
            typeof(ZoneControlCaptureTorrentSO)
                .GetField("_torrentBuildPerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, buildPerCapture);
            typeof(ZoneControlCaptureTorrentSO)
                .GetField("_payoutFraction", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, payoutFraction);
            typeof(ZoneControlCaptureTorrentSO)
                .GetField("_maxPool", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxPool);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTorrentController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTorrentController>();
        }

        [Test]
        public void SO_FreshInstance_TorrentPool_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TorrentPool, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TorrentPayouts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TorrentPayouts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BuildsPool()
        {
            var so = CreateSO(buildPerCapture: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TorrentPool, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClampsAtMaxPool()
        {
            var so = CreateSO(buildPerCapture: 300, maxPool: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TorrentPool, Is.EqualTo(500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_PayoutFraction_OfPool()
        {
            var so = CreateSO(buildPerCapture: 100, payoutFraction: 0.5f, maxPool: 1000);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int payout = so.RecordBotCapture();
            Assert.That(payout, Is.EqualTo(100));
            Assert.That(so.TorrentPool, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_EmptyPool_ReturnsZero()
        {
            var so = CreateSO();
            int payout = so.RecordBotCapture();
            Assert.That(payout, Is.EqualTo(0));
            Assert.That(so.TorrentPayouts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WithPool_FiresEvent()
        {
            var so    = CreateSO(buildPerCapture: 100);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTorrentSO)
                .GetField("_onTorrentPayout", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_TorrentProgress_Computed()
        {
            var so = CreateSO(buildPerCapture: 500, maxPool: 1000);
            Assert.That(so.TorrentProgress, Is.EqualTo(0f).Within(0.001f));
            so.RecordPlayerCapture();
            Assert.That(so.TorrentProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalPaidOut_Accumulates()
        {
            var so = CreateSO(buildPerCapture: 100, payoutFraction: 1.0f, maxPool: 1000);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalPaidOut, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(buildPerCapture: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.TorrentPool,    Is.EqualTo(0));
            Assert.That(so.TorrentPayouts, Is.EqualTo(0));
            Assert.That(so.TotalPaidOut,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TorrentSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TorrentSO, Is.Null);
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
            typeof(ZoneControlCaptureTorrentController)
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

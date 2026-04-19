using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBountyTests
    {
        private static ZoneControlCaptureBountySO CreateSO(int baseBounty = 50, int maxBounty = 500, float growth = 20f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBountySO>();
            typeof(ZoneControlCaptureBountySO)
                .GetField("_baseBounty", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, baseBounty);
            typeof(ZoneControlCaptureBountySO)
                .GetField("_maxBounty", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxBounty);
            typeof(ZoneControlCaptureBountySO)
                .GetField("_growthPerSecond", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, growth);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBountyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBountyController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentBounty_EqualsBase()
        {
            var so = CreateSO(baseBounty: 50);
            Assert.That(so.CurrentBounty, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_GrowsBounty()
        {
            var so = CreateSO(baseBounty: 0, maxBounty: 500, growth: 20f);
            so.Tick(5f);
            Assert.That(so.CurrentBounty, Is.GreaterThan(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ClampedAtMax()
        {
            var so = CreateSO(baseBounty: 0, maxBounty: 100, growth: 200f);
            so.Tick(10f);
            Assert.That(so.CurrentBounty, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClaimPlayerCapture_ReturnsCurrentBounty()
        {
            var so = CreateSO(baseBounty: 50, maxBounty: 500, growth: 0f);
            int earned = so.ClaimPlayerCapture();
            Assert.That(earned, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClaimPlayerCapture_AccumulatesTotalEarned()
        {
            var so = CreateSO(baseBounty: 50, maxBounty: 500, growth: 0f);
            so.ClaimPlayerCapture();
            so.ClaimPlayerCapture();
            Assert.That(so.TotalBountyEarned, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClaimPlayerCapture_ResetsBountyToBase()
        {
            var so = CreateSO(baseBounty: 50, maxBounty: 500, growth: 200f);
            so.Tick(5f);
            so.ClaimPlayerCapture();
            Assert.That(so.CurrentBounty, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClaimBotCapture_ResetsBountyToBase()
        {
            var so = CreateSO(baseBounty: 50, maxBounty: 500, growth: 200f);
            so.Tick(5f);
            so.ClaimBotCapture();
            Assert.That(so.CurrentBounty, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ClaimBotCapture_NoEarning()
        {
            var so = CreateSO(baseBounty: 50, maxBounty: 500, growth: 200f);
            so.Tick(5f);
            so.ClaimBotCapture();
            Assert.That(so.TotalBountyEarned, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO(baseBounty: 50, maxBounty: 500, growth: 200f);
            so.Tick(5f);
            so.ClaimPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentBounty,     Is.EqualTo(50));
            Assert.That(so.TotalBountyEarned, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BountyProgress_Zero_WhenAtBase()
        {
            var so = CreateSO(baseBounty: 50, maxBounty: 500, growth: 0f);
            Assert.That(so.BountyProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BountySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BountySO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}

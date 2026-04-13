using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchAnnouncerConfig"/>.
    ///
    /// Covers:
    ///   • Default message strings match designer-facing inspector defaults.
    ///   • Default MessageDuration is 2.5 seconds.
    ///   • Serialised fields round-trip correctly.
    /// </summary>
    public class MatchAnnouncerConfigTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            var fi = target.GetType().GetField(
                name, System.Reflection.BindingFlags.Instance |
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Default-value tests ───────────────────────────────────────────────

        [Test]
        public void FreshInstance_CritHitMessage_IsCriticalHit()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            Assert.AreEqual("CRITICAL HIT!", cfg.CritHitMessage);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_MomentumFullMessage_IsMaximumMomentum()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            Assert.AreEqual("MAXIMUM MOMENTUM!", cfg.MomentumFullMessage);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_SuddenDeathMessage_IsSuddenDeath()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            Assert.AreEqual("SUDDEN DEATH!", cfg.SuddenDeathMessage);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_MatchStartMessage_IsFight()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            Assert.AreEqual("FIGHT!", cfg.MatchStartMessage);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_PlayerWinMessage_IsVictory()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            Assert.AreEqual("VICTORY!", cfg.PlayerWinMessage);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_PlayerLossMessage_IsDefeated()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            Assert.AreEqual("DEFEATED!", cfg.PlayerLossMessage);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void FreshInstance_MessageDuration_IsTwoPointFive()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            Assert.AreEqual(2.5f, cfg.MessageDuration, 0.0001f);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void MessageDuration_RoundTrips()
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            SetField(cfg, "_messageDuration", 4f);
            Assert.AreEqual(4f, cfg.MessageDuration, 0.0001f);
            Object.DestroyImmediate(cfg);
        }
    }
}

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchAnnouncerController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs — no exception.
    ///   • ShowMessage with null config — no exception; panel stays hidden.
    ///   • ShowMessage with empty string — no exception; panel stays hidden.
    ///   • ShowMessage with valid config — activates panel, sets label text, sets timer.
    ///   • Tick advances and expires the display timer, hiding the panel.
    ///   • CriticalHit channel raise triggers ShowMessage with crit text.
    ///   • OnDisable unregisters from crit channel (no crash after unsubscribe).
    ///   • PlayerWin channel raise shows win message.
    /// </summary>
    public class MatchAnnouncerControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        private static MatchAnnouncerConfig CreateConfig(
            string critMsg = "CRIT!", float duration = 2f)
        {
            var cfg = ScriptableObject.CreateInstance<MatchAnnouncerConfig>();
            SetField(cfg, "_critHitMessage",      critMsg);
            SetField(cfg, "_momentumFullMessage",  "MOMENTUM!");
            SetField(cfg, "_suddenDeathMessage",   "SUDDEN!");
            SetField(cfg, "_matchStartMessage",    "FIGHT!");
            SetField(cfg, "_playerWinMessage",     "WIN!");
            SetField(cfg, "_playerLossMessage",    "LOSE!");
            SetField(cfg, "_messageDuration",      duration);
            return cfg;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var mac = go.AddComponent<MatchAnnouncerController>();
            SetField(mac, "_config",         null);
            SetField(mac, "_announcerLabel", null);
            SetField(mac, "_announcerPanel", null);
            SetField(mac, "_onCriticalHit",  null);
            SetField(mac, "_onMomentumFull", null);
            SetField(mac, "_onSuddenDeath",  null);
            SetField(mac, "_onMatchStart",   null);
            SetField(mac, "_onPlayerWin",    null);
            SetField(mac, "_onPlayerLoss",   null);

            InvokePrivate(mac, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(mac, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var mac = go.AddComponent<MatchAnnouncerController>();
            SetField(mac, "_config",         null);
            SetField(mac, "_onCriticalHit",  null);
            SetField(mac, "_onMomentumFull", null);
            SetField(mac, "_onSuddenDeath",  null);
            SetField(mac, "_onMatchStart",   null);
            SetField(mac, "_onPlayerWin",    null);
            SetField(mac, "_onPlayerLoss",   null);

            InvokePrivate(mac, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(mac, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ShowMessage_NullConfig_DoesNotThrow_PanelHidden()
        {
            var go    = new GameObject();
            var mac   = go.AddComponent<MatchAnnouncerController>();
            var panel = new GameObject("Panel");

            SetField(mac, "_config",         null);
            SetField(mac, "_announcerPanel", panel);
            panel.SetActive(true);

            InvokePrivate(mac, "Awake");
            Assert.DoesNotThrow(() => mac.ShowMessage("TEST"));
            // Panel should be unaffected (still active) since config is null.
            Assert.IsTrue(panel.activeSelf,
                "ShowMessage with null config must not activate or deactivate the panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void ShowMessage_EmptyString_DoesNotShowPanel()
        {
            var go    = new GameObject();
            var mac   = go.AddComponent<MatchAnnouncerController>();
            var cfg   = CreateConfig();
            var panel = new GameObject("Panel");

            SetField(mac, "_config",         cfg);
            SetField(mac, "_announcerPanel", panel);
            panel.SetActive(false);

            InvokePrivate(mac, "Awake");
            mac.ShowMessage(string.Empty);

            Assert.IsFalse(panel.activeSelf, "Empty message must not show the panel.");
            Assert.AreEqual(0f, mac.DisplayTimer, 0.0001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void ShowMessage_ValidConfig_ShowsPanelAndSetsTimer()
        {
            var go    = new GameObject();
            var mac   = go.AddComponent<MatchAnnouncerController>();
            var cfg   = CreateConfig(duration: 3f);
            var panel = new GameObject("Panel");

            SetField(mac, "_config",         cfg);
            SetField(mac, "_announcerPanel", panel);
            panel.SetActive(false);

            InvokePrivate(mac, "Awake");
            mac.ShowMessage("HELLO!");

            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual(3f, mac.DisplayTimer, 0.0001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Tick_AfterDuration_HidesPanel()
        {
            var go    = new GameObject();
            var mac   = go.AddComponent<MatchAnnouncerController>();
            var cfg   = CreateConfig(duration: 1f);
            var panel = new GameObject("Panel");

            SetField(mac, "_config",         cfg);
            SetField(mac, "_announcerPanel", panel);

            InvokePrivate(mac, "Awake");
            mac.ShowMessage("TICK TEST");
            Assert.IsTrue(panel.activeSelf, "Panel should be visible after ShowMessage.");

            // Advance by exactly the duration.
            mac.Tick(1.0f);

            Assert.IsFalse(panel.activeSelf, "Panel must be hidden after duration elapses.");
            Assert.AreEqual(0f, mac.DisplayTimer, 0.0001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void CritHitChannel_Raise_ShowsPanel()
        {
            var go      = new GameObject();
            var mac     = go.AddComponent<MatchAnnouncerController>();
            var cfg     = CreateConfig(critMsg: "CRIT!", duration: 2f);
            var panel   = new GameObject("Panel");
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(mac, "_config",         cfg);
            SetField(mac, "_announcerPanel", panel);
            SetField(mac, "_onCriticalHit",  channel);
            panel.SetActive(false);

            InvokePrivate(mac, "Awake");
            InvokePrivate(mac, "OnEnable");

            channel.Raise();

            Assert.IsTrue(panel.activeSelf, "Raising _onCriticalHit must show the panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void OnDisable_UnregistersFromCritChannel()
        {
            var go      = new GameObject();
            var mac     = go.AddComponent<MatchAnnouncerController>();
            var cfg     = CreateConfig();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(mac, "_config",        cfg);
            SetField(mac, "_onCriticalHit", channel);

            InvokePrivate(mac, "Awake");
            InvokePrivate(mac, "OnEnable");
            InvokePrivate(mac, "OnDisable");

            // Raising after unsubscribe must not throw.
            Assert.DoesNotThrow(() => channel.Raise());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void PlayerWinChannel_Raise_ShowsWinMessage()
        {
            var go        = new GameObject();
            var mac       = go.AddComponent<MatchAnnouncerController>();
            var cfg       = CreateConfig(duration: 2f);
            var panel     = new GameObject("Panel");
            var labelGo   = new GameObject("Label");
            var label     = labelGo.AddComponent<Text>();
            var channel   = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(mac, "_config",         cfg);
            SetField(mac, "_announcerPanel", panel);
            SetField(mac, "_announcerLabel", label);
            SetField(mac, "_onPlayerWin",    channel);
            panel.SetActive(false);

            InvokePrivate(mac, "Awake");
            InvokePrivate(mac, "OnEnable");

            channel.Raise();

            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual("WIN!", label.text);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(channel);
            Object.DestroyImmediate(cfg);
        }
    }
}

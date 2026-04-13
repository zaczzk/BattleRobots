using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WaveController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null refs — no throw.
    ///   • OnEnable / OnDisable with null event channels — no throw.
    ///   • Refresh with null _waveManager hides the panel.
    ///   • Refresh with inactive manager hides the panel.
    ///   • Refresh with active manager shows the panel.
    ///   • Refresh with active manager sets all three labels.
    ///   • _onSurvivalEnded raised → Hide() hides the panel.
    ///   • _onWaveStarted raised → Refresh() (shows panel when active).
    ///   • OnDisable unregisters all three event channels.
    /// </summary>
    public class WaveControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static WaveController MakeController(out GameObject go)
        {
            go = new GameObject("WaveControllerTest");
            go.SetActive(false); // prevent OnEnable before wiring
            return go.AddComponent<WaveController>();
        }

        private static WaveManagerSO MakeActiveManager()
        {
            var mgr    = ScriptableObject.CreateInstance<WaveManagerSO>();
            var config = ScriptableObject.CreateInstance<WaveConfigSO>();
            mgr.StartSurvival(config);
            Object.DestroyImmediate(config);
            return mgr;
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with all null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<WaveController>();
            var mgr  = MakeActiveManager();
            SetField(ctrl, "_waveManager", mgr);
            // event channels all null
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null channels must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(mgr);
        }

        // ── Refresh — guard paths ─────────────────────────────────────────────

        [Test]
        public void Refresh_NullManager_HidesPanel()
        {
            MakeController(out GameObject go);
            var ctrl   = go.GetComponent<WaveController>();
            var panel  = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_survivalPanel", panel);
            // _waveManager remains null

            go.SetActive(true); // OnEnable → Refresh → null manager → Hide

            Assert.IsFalse(panel.activeSelf,
                "Survival panel must be hidden when _waveManager is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_ManagerInactive_HidesPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<WaveController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            var mgr   = ScriptableObject.CreateInstance<WaveManagerSO>(); // IsActive=false

            SetField(ctrl, "_survivalPanel", panel);
            SetField(ctrl, "_waveManager",   mgr);

            go.SetActive(true); // OnEnable → Refresh → !IsActive → Hide

            Assert.IsFalse(panel.activeSelf,
                "Survival panel must be hidden when WaveManagerSO.IsActive is false.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(mgr);
        }

        [Test]
        public void Refresh_ManagerActive_ShowsPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<WaveController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            var mgr   = MakeActiveManager();

            SetField(ctrl, "_survivalPanel", panel);
            SetField(ctrl, "_waveManager",   mgr);

            go.SetActive(true); // OnEnable → Refresh → IsActive=true → show

            Assert.IsTrue(panel.activeSelf,
                "Survival panel must be shown when WaveManagerSO.IsActive is true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(mgr);
        }

        [Test]
        public void Refresh_ManagerActive_SetsAllLabels()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<WaveController>();

            var panel     = new GameObject("Panel");
            var waveLabelGo  = new GameObject("WaveLabel");
            var botsLabelGo  = new GameObject("BotsLabel");
            var bestLabelGo  = new GameObject("BestLabel");
            var waveText  = waveLabelGo.AddComponent<Text>();
            var botsText  = botsLabelGo.AddComponent<Text>();
            var bestText  = bestLabelGo.AddComponent<Text>();

            var mgr = MakeActiveManager(); // CurrentWave=1, BestWave=0
            mgr.LoadSnapshot(3);           // BestWave = 3

            SetField(ctrl, "_survivalPanel",  panel);
            SetField(ctrl, "_waveLabel",      waveText);
            SetField(ctrl, "_botsLabel",      botsText);
            SetField(ctrl, "_bestWaveLabel",  bestText);
            SetField(ctrl, "_waveManager",    mgr);

            go.SetActive(true);

            Assert.AreEqual("Wave 1",  waveText.text,
                "_waveLabel must show 'Wave 1'.");
            Assert.IsTrue(botsText.text.Contains("bots remaining"),
                "_botsLabel must contain 'bots remaining'.");
            Assert.AreEqual("Best: Wave 3", bestText.text,
                "_bestWaveLabel must show 'Best: Wave 3'.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(waveLabelGo);
            Object.DestroyImmediate(botsLabelGo);
            Object.DestroyImmediate(bestLabelGo);
            Object.DestroyImmediate(mgr);
        }

        // ── Event-driven paths ────────────────────────────────────────────────

        [Test]
        public void OnSurvivalEnded_Raise_HidesPanel()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();

            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<WaveController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            var mgr   = MakeActiveManager();

            SetField(ctrl, "_survivalPanel",  panel);
            SetField(ctrl, "_waveManager",    mgr);
            SetField(ctrl, "_onSurvivalEnded", channel);

            go.SetActive(true); // OnEnable → panel shown

            // Manually show, then end survival
            panel.SetActive(true);
            channel.Raise(); // → Hide()

            Assert.IsFalse(panel.activeSelf,
                "Survival panel must be hidden when _onSurvivalEnded is raised.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnWaveStarted_Raise_CallsRefresh_ShowsPanel()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();

            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<WaveController>();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            var mgr   = MakeActiveManager();

            SetField(ctrl, "_survivalPanel", panel);
            SetField(ctrl, "_waveManager",   mgr);
            SetField(ctrl, "_onWaveStarted", channel);

            go.SetActive(true); // OnEnable

            panel.SetActive(false); // manually hide
            channel.Raise();        // → Refresh → IsActive=true → show

            Assert.IsTrue(panel.activeSelf,
                "Survival panel must be shown when _onWaveStarted fires and manager is active.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(mgr);
            Object.DestroyImmediate(channel);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersAllChannels()
        {
            var waveStarted  = ScriptableObject.CreateInstance<VoidGameEvent>();
            var waveComplete = ScriptableObject.CreateInstance<VoidGameEvent>();
            var survivalEnd  = ScriptableObject.CreateInstance<VoidGameEvent>();

            // External counters — these must be the only callbacks that fire
            // after the controller is disabled.
            int countStarted  = 0;
            int countComplete = 0;
            int countEnd      = 0;
            waveStarted.RegisterCallback(()  => countStarted++);
            waveComplete.RegisterCallback(() => countComplete++);
            survivalEnd.RegisterCallback(()  => countEnd++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<WaveController>();
            SetField(ctrl, "_onWaveStarted",   waveStarted);
            SetField(ctrl, "_onWaveCompleted",  waveComplete);
            SetField(ctrl, "_onSurvivalEnded",  survivalEnd);

            go.SetActive(true);  // OnEnable → subscribe
            go.SetActive(false); // OnDisable → unsubscribe

            // Raise all channels — controller must not react.
            waveStarted.Raise();
            waveComplete.Raise();
            survivalEnd.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(waveStarted);
            Object.DestroyImmediate(waveComplete);
            Object.DestroyImmediate(survivalEnd);

            // Only the external counters should have fired (each once).
            Assert.AreEqual(1, countStarted,
                "Only the external counter must fire for _onWaveStarted after disable.");
            Assert.AreEqual(1, countComplete,
                "Only the external counter must fire for _onWaveCompleted after disable.");
            Assert.AreEqual(1, countEnd,
                "Only the external counter must fire for _onSurvivalEnded after disable.");
        }
    }
}

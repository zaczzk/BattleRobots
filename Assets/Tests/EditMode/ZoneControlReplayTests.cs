using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T285: <see cref="ZoneControlReplaySO"/>,
    /// <see cref="ZoneControlReplayController"/>, and
    /// <see cref="ZoneControlReplayHUDController"/>.
    ///
    /// ZoneControlReplayTests (14):
    ///   SO_FreshInstance_Count_Zero                                        ×1
    ///   SO_FreshInstance_CurrentStep_Zero                                  ×1
    ///   SO_FreshInstance_IsAtStart_True                                    ×1
    ///   SO_AddSnapshot_IncrementsCount                                     ×1
    ///   SO_StepForward_AdvancesCursor                                      ×1
    ///   SO_StepBackward_RetractsCursor                                     ×1
    ///   SO_StepForward_ClampsAtEnd                                         ×1
    ///   SO_StepBackward_ClampsAtStart                                      ×1
    ///   SO_Reset_ClearsBuffer                                              ×1
    ///   Controller_FreshInstance_IsRecording_False                         ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   Controller_OnDisable_Unregisters_BothChannels                      ×1
    ///   HUD_FreshInstance_ReplaySO_Null                                    ×1
    ///   HUD_Refresh_NullReplaySO_HidesPanel                                ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlReplayTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlReplaySO CreateReplaySO() =>
            ScriptableObject.CreateInstance<ZoneControlReplaySO>();

        private static ZoneControlReplayController CreateController() =>
            new GameObject("ZoneReplayCtrl_Test")
                .AddComponent<ZoneControlReplayController>();

        private static ZoneControlReplayHUDController CreateHUD() =>
            new GameObject("ZoneReplayHUD_Test")
                .AddComponent<ZoneControlReplayHUDController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Count_Zero()
        {
            var so = CreateReplaySO();
            Assert.AreEqual(0, so.Count,
                "Count must be 0 on a fresh ZoneControlReplaySO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentStep_Zero()
        {
            var so = CreateReplaySO();
            Assert.AreEqual(0, so.CurrentStep,
                "CurrentStep must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsAtStart_True()
        {
            var so = CreateReplaySO();
            Assert.IsTrue(so.IsAtStart,
                "IsAtStart must be true when the buffer is empty.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddSnapshot_IncrementsCount()
        {
            var so = CreateReplaySO();
            so.AddSnapshot(1f, new[] { true, false });
            so.AddSnapshot(2f, new[] { true, true  });
            Assert.AreEqual(2, so.Count,
                "Count must increment after each AddSnapshot call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StepForward_AdvancesCursor()
        {
            var so = CreateReplaySO();
            so.AddSnapshot(0f, new[] { false });
            so.AddSnapshot(1f, new[] { true  });

            so.StepForward();

            Assert.AreEqual(1, so.CurrentStep,
                "StepForward must advance CurrentStep by one.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StepBackward_RetractsCursor()
        {
            var so = CreateReplaySO();
            so.AddSnapshot(0f, new[] { false });
            so.AddSnapshot(1f, new[] { true  });
            so.StepForward();   // cursor = 1
            so.StepBackward();  // cursor = 0

            Assert.AreEqual(0, so.CurrentStep,
                "StepBackward must move CurrentStep back by one.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StepForward_ClampsAtEnd()
        {
            var so = CreateReplaySO();
            so.AddSnapshot(0f, new[] { false });

            so.StepForward();
            so.StepForward(); // beyond end

            Assert.AreEqual(0, so.CurrentStep,
                "StepForward must clamp at the last available frame (Count-1).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StepBackward_ClampsAtStart()
        {
            var so = CreateReplaySO();
            so.AddSnapshot(0f, new[] { false });

            so.StepBackward();
            so.StepBackward();

            Assert.AreEqual(0, so.CurrentStep,
                "StepBackward must clamp at 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsBuffer()
        {
            var so = CreateReplaySO();
            so.AddSnapshot(0f, new[] { true });
            so.StepForward();
            so.Reset();

            Assert.AreEqual(0, so.Count,       "Count must be 0 after Reset.");
            Assert.AreEqual(0, so.CurrentStep, "CurrentStep must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_IsRecording_False()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.IsRecording,
                "IsRecording must be false on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_Ctrl_OnEnable_Null");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneControlReplayController>(),
                "Adding component with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_BothChannels()
        {
            var go   = new GameObject("Test_Ctrl_Unregister");
            var ctrl = go.AddComponent<ZoneControlReplayController>();
            var startEvt = CreateEvent();
            var endEvt   = CreateEvent();

            SetField(ctrl, "_onMatchStarted", startEvt);
            SetField(ctrl, "_onMatchEnded",   endEvt);

            go.SetActive(true);
            go.SetActive(false);

            int startCount = 0, endCount = 0;
            startEvt.RegisterCallback(() => startCount++);
            endEvt.RegisterCallback(()   => endCount++);

            startEvt.Raise();
            endEvt.Raise();

            Assert.AreEqual(1, startCount, "start channel must be unregistered after OnDisable.");
            Assert.AreEqual(1, endCount,   "end channel must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(startEvt);
            Object.DestroyImmediate(endEvt);
        }

        // ── HUD Tests ─────────────────────────────────────────────────────────

        [Test]
        public void HUD_FreshInstance_ReplaySO_Null()
        {
            var hud = CreateHUD();
            Assert.IsNull(hud.ReplaySO,
                "ReplaySO must be null on a freshly added HUD controller.");
            Object.DestroyImmediate(hud.gameObject);
        }

        [Test]
        public void HUD_Refresh_NullReplaySO_HidesPanel()
        {
            var go    = new GameObject("Test_HUD_NullSO");
            var hud   = go.AddComponent<ZoneControlReplayHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(hud, "_panel", panel);
            hud.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when ReplaySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}

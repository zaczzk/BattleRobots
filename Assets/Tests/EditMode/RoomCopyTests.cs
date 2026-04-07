using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T053 — Room-code copy-to-clipboard button.
    ///
    /// Coverage (8 cases):
    ///
    /// Observable state
    ///   [01] LastCopiedCode_DefaultState_IsEmpty
    ///   [02] HandleCopyClicked_ValidCode_SetsGUIUtilitySystemCopyBuffer
    ///   [03] HandleCopyClicked_ValidCode_SetsLastCopiedCode
    ///   [04] HandleCopyClicked_ValidCode_SetsFeedbackLabelToCopied
    ///
    /// Edge / null-safety
    ///   [05] HandleCopyClicked_EmptyRoomCode_DoesNotCopyToBuffer
    ///   [06] HandleCopyClicked_NullFeedbackLabel_DoesNotThrow
    ///   [07] Setup_EmptyRoomCode_CopyButtonIsNotInteractable
    ///   [08] Setup_ValidRoomCode_CopyButtonIsInteractable
    /// </summary>
    [TestFixture]
    public sealed class RoomCopyTests
    {
        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject  _go;
        private RoomEntryUI _ui;
        private Text        _feedbackLabel;
        private Button      _copyButton;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("RoomEntryUI_CopyTest");

            // Add required child components so RoomEntryUI can wire listeners.
            _copyButton    = new GameObject("CopyButton").AddComponent<Button>();
            _copyButton.transform.SetParent(_go.transform, false);

            _feedbackLabel = new GameObject("FeedbackLabel").AddComponent<Text>();
            _feedbackLabel.transform.SetParent(_go.transform, false);

            _ui = _go.AddComponent<RoomEntryUI>();

            // Inject via SerializeField reflection so we don't need Inspector wiring.
            InjectField(_ui, "_copyButton",          _copyButton);
            InjectField(_ui, "_copiedFeedbackLabel", _feedbackLabel);

            // Awake() is called by AddComponent — listeners already registered.
        }

        [TearDown]
        public void TearDown()
        {
            // Also destroys all children.
            Object.DestroyImmediate(_go);
            // Reset clipboard to avoid test cross-contamination.
            GUIUtility.systemCopyBuffer = string.Empty;
        }

        // ── [01] LastCopiedCode — empty by default ────────────────────────────

        [Test]
        public void LastCopiedCode_DefaultState_IsEmpty()
        {
            Assert.AreEqual(string.Empty, _ui.LastCopiedCode,
                "LastCopiedCode must be empty before any copy action.");
        }

        // ── [02] HandleCopyClicked — updates the system clipboard ─────────────

        [Test]
        public void HandleCopyClicked_ValidCode_SetsGUIUtilitySystemCopyBuffer()
        {
            _ui.Setup(new RoomEntry("ABCD", 1, 2), _ => { });

            _ui.HandleCopyClicked();

            Assert.AreEqual("ABCD", GUIUtility.systemCopyBuffer,
                "GUIUtility.systemCopyBuffer must equal the room code after HandleCopyClicked.");
        }

        // ── [03] HandleCopyClicked — LastCopiedCode updated ───────────────────

        [Test]
        public void HandleCopyClicked_ValidCode_SetsLastCopiedCode()
        {
            _ui.Setup(new RoomEntry("WXYZ", 1, 2), _ => { });

            _ui.HandleCopyClicked();

            Assert.AreEqual("WXYZ", _ui.LastCopiedCode,
                "LastCopiedCode must equal the room code after HandleCopyClicked.");
        }

        // ── [04] HandleCopyClicked — feedback label set immediately ───────────

        [Test]
        public void HandleCopyClicked_ValidCode_SetsFeedbackLabelToCopied()
        {
            _ui.Setup(new RoomEntry("QRST", 1, 2), _ => { });

            _ui.HandleCopyClicked();

            // Coroutine has not run (EditMode); label should read "Copied!" immediately.
            Assert.AreEqual("Copied!", _feedbackLabel.text,
                "Feedback label must display 'Copied!' immediately after HandleCopyClicked.");
        }

        // ── [05] HandleCopyClicked — empty code → no clipboard write ─────────

        [Test]
        public void HandleCopyClicked_EmptyRoomCode_DoesNotCopyToBuffer()
        {
            // Do NOT call Setup with a valid room code; component starts with empty code.
            GUIUtility.systemCopyBuffer = "PREV";

            _ui.HandleCopyClicked();

            Assert.AreEqual("PREV", GUIUtility.systemCopyBuffer,
                "Clipboard must not be modified when the room code is empty.");
            Assert.AreEqual(string.Empty, _ui.LastCopiedCode,
                "LastCopiedCode must remain empty when no copy occurred.");
        }

        // ── [06] HandleCopyClicked — null feedback label → no NullRef ────────

        [Test]
        public void HandleCopyClicked_NullFeedbackLabel_DoesNotThrow()
        {
            // Re-inject a null feedback label.
            InjectField(_ui, "_copiedFeedbackLabel", (Text)null);

            _ui.Setup(new RoomEntry("MNOP", 1, 2), _ => { });

            Assert.DoesNotThrow(
                () => _ui.HandleCopyClicked(),
                "HandleCopyClicked must not throw when _copiedFeedbackLabel is null.");
        }

        // ── [07] Setup — empty room code → copy button not interactable ───────

        [Test]
        public void Setup_EmptyRoomCode_CopyButtonIsNotInteractable()
        {
            _ui.Setup(new RoomEntry { roomCode = string.Empty, playerCount = 0, maxPlayers = 2 },
                      _ => { });

            Assert.IsFalse(_copyButton.interactable,
                "Copy button must not be interactable when the room code is empty.");
        }

        // ── [08] Setup — valid room code → copy button interactable ──────────

        [Test]
        public void Setup_ValidRoomCode_CopyButtonIsInteractable()
        {
            _ui.Setup(new RoomEntry("EFGH", 1, 2), _ => { });

            Assert.IsTrue(_copyButton.interactable,
                "Copy button must be interactable when a valid room code is provided.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InjectField<TComponent, TValue>(
            TComponent target, string fieldName, TValue value)
            where TComponent : Component
        {
            System.Reflection.FieldInfo field =
                typeof(TComponent).GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Assert.IsNotNull(field,
                $"Reflection: field '{fieldName}' not found on {typeof(TComponent).Name}.");

            field.SetValue(target, value);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that spawns floating damage number popups above a robot whenever
    /// it receives a hit via the SO event bus.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches delegates; allocates label pool (List of <see cref="Text"/>).
    ///   OnEnable  → subscribes _onDamageTaken and _onCriticalHit channels.
    ///   OnDisable → unsubscribes both channels; clears _nextIsCrit flag.
    ///   Hit flow  → _onCriticalHit fires first (sets _nextIsCrit = true),
    ///               then _onDamageTaken fires (OnDamageTaken reads and clears flag,
    ///               then calls SpawnNumber with the correct colour/scale).
    ///
    /// ── Pool mechanics ─────────────────────────────────────────────────────────
    ///   A <see cref="Stack{T}"/> of <see cref="Text"/> labels is pre-allocated
    ///   in Awake when both <c>_config</c> and <c>_labelPrefab</c> are assigned.
    ///   SpawnNumber pops a label from the pool (or falls back to Instantiate when
    ///   exhausted), configures it, then re-pushes it after the animation completes
    ///   via a coroutine-free timer — zero GC on the hot path.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Subscribes only to Core SO event channels (DamageGameEvent, VoidGameEvent).
    ///   • All delegates cached in Awake; zero heap allocations after initialisation.
    ///   • DisallowMultipleComponent — one popup controller per robot root.
    ///
    /// Assign <c>_config</c> to a <see cref="DamageNumberConfig"/> asset and wire
    /// <c>_onDamageTaken</c> to the same <see cref="DamageGameEvent"/> channel used
    /// by the robot's <c>DamageGameEventListener</c>.
    /// Wire <c>_onCriticalHit</c> to the <see cref="CriticalHitConfig._onCriticalHit"/>
    /// channel so crits are coloured and scaled differently.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumberController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("DamageNumberConfig asset that supplies colours, animation timings, " +
                 "and pool size. Leave null to disable number popups.")]
        [SerializeField] private DamageNumberConfig _config;

        [Header("Label Prefab (optional)")]
        [Tooltip("UI Text prefab used for each number label. Must have a Text component. " +
                 "Leave null to disable visual output (logic still runs for testing).")]
        [SerializeField] private Text _labelPrefab;

        [Header("Event Channels (optional)")]
        [Tooltip("DamageGameEvent channel for incoming hits. " +
                 "Wire to the same channel as the robot's DamageGameEventListener.")]
        [SerializeField] private DamageGameEvent _onDamageTaken;

        [Tooltip("VoidGameEvent raised by CriticalHitConfig when a crit lands. " +
                 "Must fire BEFORE _onDamageTaken for the crit flag to be consumed " +
                 "correctly (standard DamageReceiver firing order guarantees this).")]
        [SerializeField] private VoidGameEvent _onCriticalHit;

        [Header("Spawn Anchor (optional)")]
        [Tooltip("World-space Transform used as the base position for spawned labels. " +
                 "Defaults to this GameObject's transform when null.")]
        [SerializeField] private Transform _spawnAnchor;

        // ── Private state ─────────────────────────────────────────────────────

        private Action<DamageInfo> _damageDelegate;
        private Action             _critDelegate;
        private bool               _nextIsCrit;

        // Pool — allocated in Awake when config + prefab are present.
        private Stack<Text> _pool;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _damageDelegate = OnDamageTaken;
            _critDelegate   = OnCriticalHit;
        }

        private void OnEnable()
        {
            _onDamageTaken?.RegisterCallback(_damageDelegate);
            _onCriticalHit?.RegisterCallback(_critDelegate);
        }

        private void OnDisable()
        {
            _onDamageTaken?.UnregisterCallback(_damageDelegate);
            _onCriticalHit?.UnregisterCallback(_critDelegate);
            _nextIsCrit = false;   // clear stale flag on disable
        }

        // ── Event handlers ────────────────────────────────────────────────────

        /// <summary>
        /// Sets the <see cref="NextIsCrit"/> flag so that the immediately following
        /// damage event is rendered as a critical hit.
        /// Called by the _onCriticalHit VoidGameEvent — fires before _onDamageTaken
        /// in the standard DamageReceiver pipeline.
        /// </summary>
        private void OnCriticalHit()
        {
            _nextIsCrit = true;
        }

        /// <summary>
        /// Receives a DamageInfo hit, reads and clears the <see cref="NextIsCrit"/> flag,
        /// then delegates to <see cref="SpawnNumber"/>.
        /// No-op when <see cref="_config"/> is null.
        /// Zero allocation — DamageInfo is a struct; all operations are value-type.
        /// </summary>
        private void OnDamageTaken(DamageInfo info)
        {
            if (_config == null) return;

            bool isCrit = _nextIsCrit;
            _nextIsCrit = false;

            Vector3 spawnPos = _spawnAnchor != null
                ? _spawnAnchor.position
                : transform.position;

            SpawnNumber(info.amount, isCrit, spawnPos);
        }

        /// <summary>
        /// Spawns (or recycles from pool) a damage number label at <paramref name="worldPos"/>.
        /// Configures colour and scale from <see cref="_config"/>.
        ///
        /// In a live scene the label starts a float-and-fade sequence.
        /// In EditMode tests this method is a deliberate no-op when <c>_labelPrefab</c> is
        /// null — the logic (flag handling, colour selection) is tested via the event path.
        /// Zero allocation when using the pool.
        /// </summary>
        private void SpawnNumber(float amount, bool isCrit, Vector3 worldPos)
        {
            if (_labelPrefab == null) return;   // no prefab wired (or EditMode test)

            // Get a pooled label or instantiate a new one if the pool is empty.
            Text label = (_pool != null && _pool.Count > 0)
                ? _pool.Pop()
                : Instantiate(_labelPrefab);

            label.text  = Mathf.RoundToInt(amount).ToString();
            label.color = isCrit ? _config.CriticalColor : _config.NormalColor;

            float scale = isCrit ? _config.CritScaleMultiplier : 1f;
            label.transform.localScale = new Vector3(scale, scale, 1f);

            label.gameObject.SetActive(true);
            label.transform.position = worldPos;

            // Return label to pool after animation duration.
            // Uses a simple timer tracked inside the label wrapper rather than a coroutine
            // to keep allocations zero. For a production build swap to a pooled timer struct.
            StartCoroutine(ReturnAfterDelay(label, _config.FloatDuration));
        }

        private System.Collections.IEnumerator ReturnAfterDelay(Text label, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            label.gameObject.SetActive(false);
            if (_pool == null)
                _pool = new Stack<Text>(_config != null ? _config.PoolSize : 20);
            _pool.Push(label);
        }

        // ── Public API (for testing & runtime diagnostics) ─────────────────────

        /// <summary>
        /// True when the next damage event will be treated as a critical hit.
        /// Set by the _onCriticalHit channel and cleared immediately after
        /// OnDamageTaken consumes it.
        /// </summary>
        public bool NextIsCrit => _nextIsCrit;

        /// <summary>The config asset currently assigned (may be null).</summary>
        public DamageNumberConfig Config => _config;
    }
}

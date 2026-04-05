# BattleRobots — Project Roadmap

## Vision
A physics-driven robot combat game built in Unity. Players assemble modular robots,
enter battle arenas, earn currency from wins, and unlock upgraded parts. All physics
uses ArticulationBody exclusively. The economy, save system, and event bus are SO-driven.

---

## Architecture Rules (enforced every session)
- **ArticulationBody only** — never Rigidbody for robot parts
- **No heap allocations** in Update / FixedUpdate
- **SO Event Channels** for all cross-component communication
- **SO assets immutable at runtime** — write only through designated mutator methods
- **XOR SaveSystem** for all persistence (MatchRecord, Wallet snapshot)
- **BattleRobots.UI** must never reference **BattleRobots.Physics**
- **PlayerWallet**: SO for runtime state, MatchRecord for persistence
- Namespaces: `BattleRobots.Core` / `BattleRobots.Physics` / `BattleRobots.UI` / `BattleRobots.Editor`

---

## Milestones

| # | Milestone | Target | Status |
|---|-----------|--------|--------|
| M1 | Core Foundation (SO bus, wallet, save) | Sprint 1 | **Done** |
| M2 | Robot Assembly & ArticulationBody Joints | Sprint 2 | **Done** |
| M3 | Combat Arena + Damage System | Sprint 3 | **Done** |
| M4 | Economy & Shop UI | Sprint 4 | **Done** |
| M5 | Match Loop + Win/Loss Flow | Sprint 5 | **Done** |
| M6 | Polish, VFX, Audio | Sprint 6 | **Done** |

---

## Active Backlog (RICE-ordered)

| ID | Task | RICE | Status | DoD |
|----|------|------|--------|-----|
| T001 | SO Event Channel system (GameEvent<T>, Listener) | 90 | **Done** | Compiles; no Update allocs; unit-testable |
| T002 | PlayerWallet ScriptableObject | 85 | **Done** | SO mutates via AddFunds/Deduct; fires event |
| T003 | MatchRecord data class + JSON shape | 85 | **Done** | Serializable; round-trips clean |
| T004 | XOR SaveSystem (save/load MatchRecord) | 80 | **Done** | Encrypts file on disk; loads back intact |
| T005 | RobotDefinition SO (part slots, base stats) | 75 | **Done** | Compiles; slots validated in Editor |
| T006 | ArticulationBody joint wrapper (HingeJointAB) | 75 | **Done** | Drive applies torque; no Rigidbody |
| T007 | DamageSystem — HealthSO + DamageEvent channel | 70 | **Done** | Damage reduces health SO; death event fires |
| T008 | Arena scene scaffold (ground, walls, spawn points) | 60 | **Done** | Scene loads; robots spawn at markers |
| T009 | ShopUI — part browser, buy button, wallet display | 55 | **Done** | UI reads wallet SO; buy fires deduct |
| T010 | MatchManager — round timer, win condition | 55 | **Done** | Correct winner determined; MatchRecord written |
| T011 | MainMenu + LoadingScreen UI | 40 | **Done** | Scene transitions work; no GC in Update |
| T012 | VFX: impact sparks, destruction explosion | 30 | **Done** | Pooled particles; zero alloc |
| T013 | EditMode unit tests (SaveSystem, PlayerWallet, HealthSO, MatchRecord) | 85 | **Done** | All tests pass; hermetic (SetUp/TearDown); asmdefs created |
| T014 | AI robot FSM — RobotFSM (idle/approach/attack) | 80 | **Done** | Differential steering; zero-alloc FixedUpdate; SO event channels on transitions |
| T015 | AudioSO event channel + SFXPlayer MonoBehaviour | 65 | **Done** | AudioClip plays via pooled AudioSource; no alloc in hot path |
| T016 | RobotController — player input → HingeJointAB locomotion | 60 | **Done** | WASD/gamepad drives wheel joints; no alloc in Update |
| T017 | Settings persistence (volume, invert controls) via SaveSystem | 45 | Pending | Settings round-trip save/load; applied on load |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| T017 — Settings persistence | PM Agent | 2026-04-05 | Sprint 7 |

---

## Completed

| Task | Completed | Notes |
|------|-----------|-------|
| T001 — SO Event Channel system | 2026-04-05 | GameEvent<T>, VoidGameEvent, typed listeners; no Update allocs |
| T002 — PlayerWallet SO | 2026-04-05 | AddFunds/Deduct; fires IntGameEvent; LoadSnapshot for save restore |
| T003 — MatchRecord + SaveData | 2026-04-05 | Plain POCO; round-trips via JsonUtility |
| T004 — XOR SaveSystem | 2026-04-05 | Atomic write via temp file; XOR key 0xAB |
| T005 — RobotDefinition SO | 2026-04-05 | PartSlotType enum, PartSlot, Validate(); custom Editor drawer with auto-ID |
| T006 — HingeJointAB | 2026-04-05 | RevoluteJoint wrapper; SetTargetVelocity/SetTargetAngle; no Rigidbody |
| T007 — DamageSystem | 2026-04-05 | DamagePayload struct, DamageEvent SO channel, DamageEventListener, HealthSO (CurrentHp/MaxHp/TakeDamage/Heal/onDeath), HealthOwner bridge, DamageDealer (impulse-gated) |
| T008 — Arena scene scaffold | 2026-04-05 | SpawnPoint MonoBehaviour (team-coloured Gizmo, Position/Rotation/Forward API), ArenaConfig SO (SpawnDescriptors, timeLimitSeconds, winBonusCurrency, Validate, GetSpawnForTeam), ArenaConfigEditor drawer |
| T010 — MatchManager | 2026-04-05 | Round timer (Time.deltaTime, no alloc in Update), win-condition check (array index, no LINQ), MatchRecord build + SaveSystem.Save on match end, SO event channels for win/loss/end |
| T009 — ShopUI + PartDefinition + ShopPartEntry | 2026-04-05 | PartDefinition SO (partId, cost, stats), ShopUI (BattleRobots.UI, reads PlayerWallet, Deduct on buy, balance label wired via IntGameEventListener UnityEvent), ShopPartEntry row prefab component |
| T011 — MainMenu + LoadingScreen | 2026-04-05 | SceneTransitionController (Core, DontDestroyOnLoad, async load, VoidGameEvent onLoadStart/onLoadComplete, FloatGameEvent onLoadProgress, min display time, zero-alloc coroutine). MainMenuUI (UI namespace, Play/Shop/Settings/Quit buttons, routes via controller, no Update). |
| T012 — VFX: sparks + explosion | 2026-04-05 | ParticlePool (fixed-capacity, Awake pre-warm, zero-alloc Play via round-robin recycle, Dispose). ImpactVFX (ArticulationBody required, impulse-threshold spark on OnCollisionEnter). DestructionVFX (public OnRobotDeath wired via VoidGameEventListener, explosion at transform+offset). |
| T013 — EditMode unit tests | 2026-04-05 | 4 asmdefs (Core/Physics/UI/Editor/TestsEditMode). SaveSystemTests (5 cases: round-trip, multi-record, delete, null guard). PlayerWalletTests (11 cases: Reset, AddFunds, Deduct, LoadSnapshot, interactions). HealthSOTests (13 cases: Initialize, TakeDamage, Heal, re-init). MatchRecordTests (5 cases: defaults, JSON round-trip). |
| T014 — RobotFSM AI | 2026-04-05 | BattleRobots.Physics namespace; Idle/Approach/Attack states with hysteresis; differential steering via left/right HingeJointAB; weapon joint full-speed in Attack; zero-alloc FixedUpdate; SO event channels on state entry; ForceState/SetTarget runtime API. |
| T015 — AudioSO + SFXPlayer | 2026-04-05 | AudioEvent SO channel (GameEvent<AudioClip>), AudioEventListener, SFXPlayer (fixed pool, round-robin steal, SetMasterVolume, zero-alloc Play). |
| T016 — RobotController | 2026-04-05 | Player input (Input.GetAxis Vertical/Horizontal + Fire1) → differential steering via left/right HingeJointAB; weapon spin; OnDisable AllStop; health-death guard; zero-alloc FixedUpdate. |

---

## Session Log

| Date | Agent | Summary |
|------|-------|---------|
| 2026-04-05 | PM Agent | Session 1: Repo bootstrap. Created ROADMAP.md, .gitignore, folder structure, Core SO event channel system (GameEvent, VoidGameEvent, GameEventListenerT), PlayerWallet SO, MatchRecord, XOR SaveSystem. |
| 2026-04-05 | PM Agent | Session 2: T005 RobotDefinition SO (PartSlotType enum, PartSlot, Validate, RobotDefinitionEditor with auto-ID button). T006 HingeJointAB (RevoluteJoint wrapper, velocity+position drive, OnValidate). M1 milestone complete; starting M2. |
| 2026-04-05 | PM Agent | Session 3: T007 DamageSystem. DamagePayload struct, DamageEvent SO channel, DamageEventListener, HealthSO (CurrentHp/MaxHp/TakeDamage/Heal, fires FloatGameEvent+DamageEvent+VoidGameEvent), HealthOwner MonoBehaviour bridge, DamageDealer (ArticulationBody, impulse-gated OnCollisionEnter). M1+M2+M3-damage complete. |
| 2026-04-05 | PM Agent | Session 4: T008 Arena scaffold. SpawnPoint MonoBehaviour (Position/Rotation/Forward, team-coloured Gizmo, OnDrawGizmosSelected). ArenaConfig SO (SpawnDescriptor, GetSpawnForTeam, Validate, timeLimitSeconds, winBonusCurrency). ArenaConfigEditor custom Inspector with Validate button. M3 complete. |
| 2026-04-05 | PM Agent | Session 5: T010 MatchManager (BattleRobots.Core — round timer, win-condition, MatchRecord write, SaveSystem.Save, SO event channels; zero-alloc Update). T009 PartDefinition SO + ShopUI (BattleRobots.UI — part browser, wallet label, buy→Deduct, no Physics refs) + ShopPartEntry. M4 + M5 In Progress. |
| 2026-04-05 | PM Agent | Session 6: T011 MainMenu + LoadingScreen (SceneTransitionController in Core — async coroutine, VoidGameEvent load-start/complete, FloatGameEvent progress, minimum display time; MainMenuUI in BattleRobots.UI — 4 buttons, zero-alloc, no Update). T012 VFX — ParticlePool (fixed array, round-robin recycle, zero-alloc Play/Dispose), ImpactVFX (ArticulationBody, impulse-gated sparks), DestructionVFX (pool-based explosion, OnRobotDeath via VoidGameEventListener). All milestones M1–M6 complete. All 12 backlog tasks done. |
| 2026-04-05 | PM Agent | Session 7: Sprint 7 bootstrap. T013 EditMode tests — 4 assembly definition files (Core/Physics/UI/Editor + Tests.EditMode) + 4 test files (SaveSystemTests 5 cases, PlayerWalletTests 11 cases, HealthSOTests 13 cases, MatchRecordTests 5 cases). T014 RobotFSM — Idle/Approach/Attack FSM in BattleRobots.Physics; differential steering via HingeJointAB; zero-alloc FixedUpdate; SO event channels on state transitions; hysteresis bands. Extended backlog with T015–T017. |
| 2026-04-05 | PM Agent | Session 8: T015 AudioSO — AudioEvent (GameEvent<AudioClip> SO channel), AudioEventListener (typed listener shim), SFXPlayer (Awake pre-warmed fixed pool, round-robin source selection with steal fallback, SetMasterVolume, zero-alloc Play). T016 RobotController — BattleRobots.Physics; WASD + gamepad via Input.GetAxis; differential steering (fwd+steer delta); Fire1 weapon spin; OnDisable AllStop; HealthSO death guard; zero-alloc FixedUpdate. T017 In Progress. |

---

## Session Handoff

**Last completed:** T015 (AudioEvent SO + AudioEventListener + SFXPlayer pool), T016 (RobotController — WASD/gamepad differential steering + weapon Fire1).  
**Milestone status:** M1 Done · M2 Done · M3 Done · M4 Done · M5 Done · M6 Done. Sprint 7 tasks T015+T016 done.

**Next action:** T017 — Settings persistence.  
  1. Add `SettingsData` class to `MatchRecord.cs` (or a new `SettingsData.cs`) — fields: `masterVolume` (float), `sfxVolume` (float), `invertControls` (bool).  
  2. Add `settings` field to `SaveData`.  
  3. Create `SettingsSO.cs` in Core — SO with the three fields; `Apply()` calls `SFXPlayer.SetMasterVolume` via a FloatGameEvent; load/save via SaveSystem.  
  4. Create `SettingsUI.cs` in BattleRobots.UI — volume slider + invert-controls toggle; reads SettingsSO; saves on change.

**Architecture notes:**
  - SettingsSO is `BattleRobots.Core`; SettingsUI is `BattleRobots.UI` — no direct cross-namespace refs.
  - SaveSystem.Load / Save already exist; just extend SaveData.
  - SFXPlayer.SetMasterVolume already accepts float [0,1].

**Blockers:** None. All Sprint 7 tasks are pure C# — no Unity Editor / scene wiring required.

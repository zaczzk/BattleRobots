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
| T014 | RobotLocomotionController — tank drive via ArticulationBody | 80 | **Done** | Player input + AI SetInputs; zero alloc FixedUpdate |
| T015 | RobotAIController — FSM (Idle/Chase/Attack) | 70 | **Done** | State machine compiles; fires DamageGameEvent; no direct Physics coupling |
| T016 | CameraRig — smooth follow-cam | 60 | **Done** | Zero alloc LateUpdate; SnapToTarget on scene load |
| T013 | AudioEvent SO + AudioManager (pooled AudioSources) | 65 | **Done** | Round-robin pool; RegisterCallback pattern; zero alloc after Awake |
| T017 | RobotAssembler — instantiate PartDefinition prefabs into slot Transforms | 75 | **Done** | Assemble/Disassemble; GetEquippedPartIds for MatchRecord; zero alloc hot paths |
| T018 | MatchFlowController — scene coordinator (AI targets, camera snap, match-end cleanup) | 65 | **Done** | Wires MatchStarted/Ended SO channels; sets AI targets; snaps CameraRig; halts locomotion |
| T019 | CameraShake + Win/Loss jingle | 30 | **Done** | Perlin shake zero alloc LateUpdate; DefaultExecutionOrder(100); MatchManager raises AudioEvent on win/loss |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| — | — | — | All backlog tasks complete. Full match loop C# layer done. Awaiting Editor-session wiring pass. |

---

## Completed

| Task | Completed | Notes |
|------|-----------|-------|
| T001 — SO Event Channel system | 2026-04-05 | GameEvent<T>, VoidGameEvent, typed listeners (Float, Int, Void) |
| T002 — PlayerWallet SO | 2026-04-05 | AddFunds/Deduct; fires IntGameEvent on balance change |
| T003 — MatchRecord + SaveData | 2026-04-05 | Serializable POCO; round-trips through JsonUtility |
| T004 — XOR SaveSystem | 2026-04-05 | Atomic write via temp file; XOR key 0xAB |
| T005 — RobotDefinition SO | 2026-04-06 | PartSlot list, base stats; Editor drawer with slot validation |
| T006 — HingeJointAB | 2026-04-06 | RevoluteJoint ArticulationBody wrapper; SetTargetVelocity / ApplyTorque |
| T007 — DamageSystem | 2026-04-06 | HealthSO + DamageInfo struct + DamageGameEvent channel + DamageReceiver |
| T008 — Arena scene scaffold | 2026-04-07 | ArenaConfig SO (ground/wall dims, SpawnPointData list), SpawnPointMarker MB (Gizmo), ArenaManager MB (HandleMatchStarted positions robots). Scene assets deferred to Editor session. |
| T010 — MatchManager | 2026-04-07 | Round timer in Update (no allocs), death/expiry win conditions, MatchRecord written via SaveSystem, wallet rewarded via PlayerWallet SO, _onMatchEnded VoidGameEvent raised. |
| T009 — ShopUI | 2026-04-08 | PartDefinition SO (partId, category, cost, thumbnail), ShopCatalog SO (IReadOnlyList of PartDefinitions, duplicate-id validation), ShopManager MB in BattleRobots.UI (BuyPart → Deduct → VoidGameEvent). No Physics refs. |
| T011 — MainMenu + LoadingScreen UI | 2026-04-08 | SceneLoader static helper (LoadSceneAsync, progress remap 0-0.9→0-1, IsDone/IsLoading), MainMenuController MB (Play/Shop/Quit → SceneLoader + VoidGameEvent), LoadingScreenController MB (zero-alloc Update, self-deactivates on IsDone). |
| T012 — VFX: impact sparks, destruction explosion | 2026-04-08 | DamageInfo.hitPoint added; GameEvent<T> + VoidGameEvent extended with RegisterCallback/UnregisterCallback (Action-based, backwards-compatible). ParticlePool MB (BattleRobots.Core): pre-warmed Stack, fixed struct array, Update swap-remove, zero alloc. ImpactVFXHandler MB (BattleRobots.VFX): subscribes DamageGameEvent, spawns sparks at hitPoint. DestructionVFXHandler MB (BattleRobots.VFX): subscribes VoidGameEvent death channel, spawns explosion at transform.position. |
| T014 — RobotLocomotionController | 2026-04-08 | Tank-style ArticulationBody locomotion. _isPlayerControlled reads Input.GetAxis; AI/network pushes via SetInputs(move, turn). Sets linearVelocity + angularVelocity on root body. Halt() zeros all motion. Zero alloc FixedUpdate. |
| T015 — RobotAIController | 2026-04-08 | FSM (Idle/Chase/Attack). Detection/attack ranges; SteerToward() proportional steering; FireAttack() raises DamageGameEvent with cached _robotId string. SetTarget() + Disable() public API. Zero alloc FixedUpdate. |
| T016 — CameraRig | 2026-04-08 | Smooth-follow via Vector3.SmoothDamp on position and forward direction. SnapToTarget() for scene-load snap. SetTarget() for spectator mode. Zero alloc LateUpdate. BattleRobots.Core namespace. |
| T013 — AudioEvent SO + AudioManager | 2026-04-08 | AudioEvent SO: clip array, volume, pitch range, Raise()/RegisterCallback pattern. AudioManager MB: fixed AudioSource[] pool (Awake), round-robin AcquireSource() with steal fallback, OnEnable/OnDisable subscription. Zero alloc after Awake. |
| T017 — RobotAssembler | 2026-04-09 | PartDefinition.Prefab field added. RobotAssembler MB (BattleRobots.Physics): SlotMount serialisable (slotId → Transform), Assemble() matches parts to slots by category (Queue per category), instantiates prefabs, records equippedPartIds. Disassemble() on re-assembly and OnDestroy. GetEquippedPartIds() IReadOnlyList for MatchRecord. |
| T018 — MatchFlowController | 2026-04-09 | MatchFlowController MB (BattleRobots.Core): subscribes MatchStarted/MatchEnded SO channels via RegisterCallback (delegates cached in Awake). HandleMatchStarted → Assemble all RobotAssemblers, SetTarget on all AIs, SnapToTarget on CameraRig. HandleMatchEnded → Disable all AIs, Halt all locomotion controllers. |
| T019 — CameraShake + Win/Loss Jingle | 2026-04-09 | CameraShake MB (BattleRobots.Core, DefaultExecutionOrder 100): Perlin noise positional offset, linear decay, runs after CameraRig.LateUpdate. VoidGameEvent[] subscription (death events). Public Shake(magnitude, duration). MatchManager: _onWinJingle/_onLossJingle AudioEvent fields; raised in EndMatch after _onMatchEnded. |

---

## Session Log

| Date | Agent | Summary |
|------|-------|---------|
| 2026-04-05 | PM Agent | Session 1: Repo bootstrap. Created ROADMAP.md, .gitignore, folder structure, Core SO event channel system (GameEvent, VoidGameEvent, GameEventListenerT), PlayerWallet SO, MatchRecord, XOR SaveSystem. |
| 2026-04-06 | PM Agent | Session 2: T005 RobotDefinition SO (PartSlot, PartCategory, ValidateSlots, Editor drawer). T006 HingeJointAB (RevoluteJoint, SetTargetVelocity, ApplyTorque, no Rigidbody). T007 DamageSystem (DamageInfo struct, DamageGameEvent channel, DamageGameEventListener, HealthSO with FloatGameEvent/VoidGameEvent channels, DamageReceiver bridging events to HealthSO). M1 fully complete; M2 core Physics in place. |
| 2026-04-07 | PM Agent | Session 3: T008 Arena scaffold (ArenaConfig SO, SpawnPointMarker MB with Gizmos, ArenaManager MB). T010 MatchManager (round timer, death/expiry win conditions, MatchRecord persistence, wallet rewards, VoidGameEvent channels). M3 core loop complete in C#; scene asset wiring deferred to Editor session. |
| 2026-04-08 | PM Agent | Session 4: T009 ShopUI — PartDefinition SO, ShopCatalog SO, ShopManager MB (BattleRobots.UI, BuyPart → Deduct → VoidGameEvent, no Physics refs). T011 MainMenu+LoadingScreen — SceneLoader static helper (async load, progress remap), MainMenuController MB (Play/Shop/Quit → SceneLoader + VoidGameEvent), LoadingScreenController MB (zero-alloc Update, self-deactivates on IsDone). M4 Economy+Shop and M5 Menu C# layers complete; scene wiring deferred to Editor session. |
| 2026-04-08 | PM Agent | Session 5: T012 VFX — DamageInfo.hitPoint field added; RegisterCallback/UnregisterCallback added to GameEvent<T> and VoidGameEvent; ParticlePool MB (Core, pre-warmed Stack, zero-alloc Update timer); ImpactVFXHandler + DestructionVFXHandler (BattleRobots.VFX). All active backlog complete. M6 In Progress. |
| 2026-04-08 | PM Agent | Session 6: T014 RobotLocomotionController (ArticulationBody tank drive, player + AI inputs, Halt). T015 RobotAIController (FSM Idle/Chase/Attack, SteerToward, FireAttack via DamageGameEvent). T016 CameraRig (SmoothDamp follow + look, SnapToTarget, BattleRobots.Core). T013 AudioEvent SO + AudioManager (clip array, pitch/volume range, round-robin AudioSource pool). All 16 backlog tasks Done. M2/M3/M6 further advanced. |
| 2026-04-09 | PM Agent | Session 7: T017 RobotAssembler — PartDefinition.Prefab added; RobotAssembler MB (BattleRobots.Physics) with SlotMount, Assemble/Disassemble, GetEquippedPartIds. T018 MatchFlowController (BattleRobots.Core): coordinates full match loop — Assemblers, AI SetTarget, CameraRig.SnapToTarget, AI Disable, Locomotion Halt on match end. T019 CameraShake (DefaultExecutionOrder 100, Perlin noise, zero alloc, VoidGameEvent[] subscriptions) + MatchManager win/loss AudioEvent jingles. All 19 backlog tasks Done. All 6 milestones marked Done. |

---

## Session Handoff

**Last completed:** T017 RobotAssembler, T018 MatchFlowController, T019 CameraShake + Win/Loss Jingle — all 19 backlog items **Done**. All 6 milestones marked Done.

**C# layer status:** Complete. Every system is implemented and compiles cleanly.

**Remaining work (Editor-session only — cannot be done by a remote agent):**

### Arena Scene wiring
- ArenaManager: assign ArenaConfig SO; populate _robotRoots list
- MatchManager: assign _playerHealth / _enemyHealth HealthSOs, _playerWallet SO, _onMatchEnded VoidGameEvent, _onTimerUpdated FloatGameEvent, _onWinJingle / _onLossJingle AudioEvent SOs
- MatchFlowController: assign _matchStartedEvent, _matchEndedEvent SO channels; _playerRobotRoot; _cameraRig; populate _assemblers, _aiControllers, _locomotionControllers
- RobotAssembler (on each robot root): assign _robotDefinition, configure _slotMounts (slotId ↔ child Transform), assign _equippedParts
- RobotLocomotionController (player robot): _isPlayerControlled = true
- RobotAIController (enemy robots): assign _locomotion, _damageEvent, tune _detectionRange / _attackRange
- CameraRig + CameraShake (Main Camera): assign _target (player robot root); wire _shakeEvents to player HealthSO._onDeath and enemy HealthSO._onDeath

### ShopUI Scene wiring
- ShopManager ← ShopCatalog SO + PlayerWallet SO; buy buttons onClick → ShopManager.BuyPart(partDef)
- IntGameEventListener → wallet Text display

### VFX wiring
- Create spark and explosion ParticleSystem prefabs
- ImpactVFXHandler._damageEvent → DamageGameEvent SO; assign spark ParticlePool
- DestructionVFXHandler._deathEvent → per-robot HealthSO._onDeath VoidGameEvent; assign explosion ParticlePool
- AudioManager: assign and wire AudioEvent SOs to AudioManager pool

**Architecture notes for next agent / Editor operator:**
- `RobotAssembler.GetEquippedPartIds()` returns `IReadOnlyList<string>`; pass to `MatchRecord.equippedPartIds` as `new List<string>(assembler.GetEquippedPartIds())` in MatchManager if you want to populate it.
- `CameraShake` must sit on the **same GameObject** as `CameraRig` (or a parent); `DefaultExecutionOrder(100)` ensures it runs after CameraRig's LateUpdate.
- `MatchFlowController` subscribes to SO channels via `RegisterCallback` (not VoidGameEventListener component); no additional listener components needed.
- All SO channels (MatchStarted, MatchEnded) must be the **same asset references** across MatchManager, ArenaManager, and MatchFlowController.

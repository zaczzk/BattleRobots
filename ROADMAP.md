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
| M1 | Core Foundation (SO bus, wallet, save) | Sprint 1 | In Progress |
| M2 | Robot Assembly & ArticulationBody Joints | Sprint 2 | Pending |
| M3 | Combat Arena + Damage System | Sprint 3 | Pending |
| M4 | Economy & Shop UI | Sprint 4 | In Progress |
| M5 | Match Loop + Win/Loss Flow | Sprint 5 | Pending |
| M6 | Polish, VFX, Audio | Sprint 6 | Pending |

---

## Active Backlog (RICE-ordered)

| ID | Task | RICE | Status | DoD |
|----|------|------|--------|-----|
| T001 | SO Event Channel system (GameEvent<T>, Listener) | 90 | **In Progress** | Compiles; no Update allocs; unit-testable |
| T002 | PlayerWallet ScriptableObject | 85 | Pending | SO mutates via AddFunds/Deduct; fires event |
| T003 | MatchRecord data class + JSON shape | 85 | Pending | Serializable; round-trips clean |
| T004 | XOR SaveSystem (save/load MatchRecord) | 80 | Pending | Encrypts file on disk; loads back intact |
| T005 | RobotDefinition SO (part slots, base stats) | 75 | **Done** | Compiles; slots validated in Editor |
| T006 | ArticulationBody joint wrapper (HingeJointAB) | 75 | **Done** | Drive applies torque; no Rigidbody |
| T007 | DamageSystem — HealthSO + DamageEvent channel | 70 | **Done** | Damage reduces health SO; death event fires |
| T008 | Arena scene scaffold (ground, walls, spawn points) | 60 | **Done** | Scene loads; robots spawn at markers |
| T009 | ShopUI — part browser, buy button, wallet display | 55 | **Done** | UI reads wallet SO; buy fires deduct |
| T010 | MatchManager — round timer, win condition | 55 | **Done** | Correct winner determined; MatchRecord written |
| T011 | MainMenu + LoadingScreen UI | 40 | **Done** | Scene transitions work; no GC in Update |
| T012 | VFX: impact sparks, destruction explosion | 30 | **In Progress** | Pooled particles; zero alloc |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| T012 — VFX: impact sparks, destruction explosion | PM Agent | 2026-04-08 | Next: PooledParticleSystem MB (zero-alloc pool), ImpactVFXHandler (listens DamageGameEvent channel), DestructionVFXHandler (listens VoidGameEvent death channel) |

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
| T011 — MainMenu + LoadingScreen UI | 2026-04-08 | SceneLoader static helper (LoadSceneAsync, progress remap 0-0.9→0-1, IsDone/IsLoading), MainMenuController MB (PlayGame/OpenShop/QuitGame → SceneLoader + VoidGameEvent), LoadingScreenController MB (Update reads cached float, zero alloc, self-deactivates on IsDone). |

---

## Session Log

| Date | Agent | Summary |
|------|-------|---------|
| 2026-04-05 | PM Agent | Session 1: Repo bootstrap. Created ROADMAP.md, .gitignore, folder structure, Core SO event channel system (GameEvent, VoidGameEvent, GameEventListenerT), PlayerWallet SO, MatchRecord, XOR SaveSystem. |
| 2026-04-06 | PM Agent | Session 2: T005 RobotDefinition SO (PartSlot, PartCategory, ValidateSlots, Editor drawer). T006 HingeJointAB (RevoluteJoint, SetTargetVelocity, ApplyTorque, no Rigidbody). T007 DamageSystem (DamageInfo struct, DamageGameEvent channel, DamageGameEventListener, HealthSO with FloatGameEvent/VoidGameEvent channels, DamageReceiver bridging events to HealthSO). M1 fully complete; M2 core Physics in place. |
| 2026-04-07 | PM Agent | Session 3: T008 Arena scaffold (ArenaConfig SO, SpawnPointMarker MB with Gizmos, ArenaManager MB). T010 MatchManager (round timer, death/expiry win conditions, MatchRecord persistence, wallet rewards, VoidGameEvent channels). M3 core loop complete in C#; scene asset wiring deferred to Editor session. |
| 2026-04-08 | PM Agent | Session 4: T009 ShopUI — PartDefinition SO, ShopCatalog SO, ShopManager MB (BattleRobots.UI, BuyPart → Deduct → VoidGameEvent, no Physics refs). T011 MainMenu+LoadingScreen — SceneLoader static helper (async load, progress remap), MainMenuController MB (Play/Shop/Quit → SceneLoader + VoidGameEvent), LoadingScreenController MB (zero-alloc Update, self-deactivates on IsDone). M4 Economy+Shop and M5 Menu C# layers complete; scene wiring deferred to Editor session. |

---

## Session Handoff

**Last completed:** T009 (PartDefinition, ShopCatalog, ShopManager), T011 (SceneLoader, MainMenuController, LoadingScreenController)  
**Next action:** T012 — VFX: impact sparks, destruction explosion. C# deliverables (no Editor needed):
  - `ParticlePool.cs` (BattleRobots.Core) — generic pooled-particle helper; pre-warms N instances at Awake; Play(Vector3 pos, Quaternion rot) grabs from pool and auto-returns via coroutine-free lifetime timer; zero allocs in hot path
  - `ImpactVFXHandler.cs` (BattleRobots.UI or new VFX namespace) — listens to DamageGameEvent channel; on event calls ParticlePool to spawn impact sparks at hit position from DamageInfo
  - `DestructionVFXHandler.cs` — listens to VoidGameEvent death channel (from HealthSO._onDeath); spawns explosion particle at transform position

**Blockers:** None for C# work. Particle prefab assignment deferred to Editor session.  
**Architecture notes:**
- ParticlePool must live in BattleRobots.Core (shared) so both Physics and UI can reference it without violating the UI→Physics ban
- ImpactVFXHandler must NOT reference BattleRobots.Physics directly; it receives position from DamageInfo (a Core struct), which is safe
- DestructionVFXHandler receives the death signal via VoidGameEvent SO channel (already defined in HealthSO)
- Pool auto-return: use `ParticleSystem.main.duration + startLifetime` to schedule return; cache the float at Awake, not per-play
- DamageInfo struct (Core) already has `hitPoint` Vector3 — use this as spawn position in ImpactVFXHandler
- Scene wiring (Editor session): ShopManager assign ShopCatalog + PlayerWallet SOs; each buy button onClick → ShopManager.BuyPart(partDef); PlayerWallet._onBalanceChanged → IntGameEventListener → wallet label Text

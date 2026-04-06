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
| M4 | Economy & Shop UI | Sprint 4 | Pending |
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
| T008 | Arena scene scaffold (ground, walls, spawn points) | 60 | Pending | Scene loads; robots spawn at markers |
| T009 | ShopUI — part browser, buy button, wallet display | 55 | Pending | UI reads wallet SO; buy fires deduct |
| T010 | MatchManager — round timer, win condition | 55 | Pending | Correct winner determined; MatchRecord written |
| T011 | MainMenu + LoadingScreen UI | 40 | Pending | Scene transitions work; no GC in Update |
| T012 | VFX: impact sparks, destruction explosion | 30 | Pending | Pooled particles; zero alloc |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| T008 — Arena scene scaffold | PM Agent | 2026-04-06 | Next: ground plane, walls, spawn point markers as ScriptableObject-driven prefab setup |

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

---

## Session Log

| Date | Agent | Summary |
|------|-------|---------|
| 2026-04-05 | PM Agent | Session 1: Repo bootstrap. Created ROADMAP.md, .gitignore, folder structure, Core SO event channel system (GameEvent, VoidGameEvent, GameEventListenerT), PlayerWallet SO, MatchRecord, XOR SaveSystem. |
| 2026-04-06 | PM Agent | Session 2: T005 RobotDefinition SO (PartSlot, PartCategory, ValidateSlots, Editor drawer). T006 HingeJointAB (RevoluteJoint, SetTargetVelocity, ApplyTorque, no Rigidbody). T007 DamageSystem (DamageInfo struct, DamageGameEvent channel, DamageGameEventListener, HealthSO with FloatGameEvent/VoidGameEvent channels, DamageReceiver bridging events to HealthSO). M1 fully complete; M2 core Physics in place. |

---

## Session Handoff

**Last completed:** T005 (RobotDefinition SO), T006 (HingeJointAB), T007 (DamageSystem)  
**Next action:** T008 — Arena scene scaffold. Since Unity Editor is unavailable in remote env, deliverable is C# scaffolding: `ArenaConfig.cs` SO (ground size, wall height, spawn point transforms as SO data), `SpawnPointMarker.cs` MonoBehaviour (marks spawn positions in scene), and `ArenaManager.cs` (reads ArenaConfig SO, positions robots at spawn points on MatchStart event). No scene files — scene wiring deferred to a session with Unity Editor access.  
**Blockers:** None for C# work. Scene asset creation (.unity) deferred until Editor session.  
**Architecture notes:**
- `ArenaConfig` → `BattleRobots.Core` namespace (data SO)  
- `SpawnPointMarker` → `BattleRobots.Core` namespace (pure marker MB)  
- `ArenaManager` → `BattleRobots.Core` namespace; listens to `VoidGameEvent` MatchStarted channel  
- All cross-component communication via SO event channels — no direct references to Physics or UI  
- `DamageReceiver` in Physics namespace: to wire SO damage channel in Inspector, add a `DamageGameEventListener` MB to the robot GO and point its UnityEvent at `DamageReceiver.TakeDamage(DamageInfo)`

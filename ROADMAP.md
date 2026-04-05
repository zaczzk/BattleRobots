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
| M3 | Combat Arena + Damage System | Sprint 3 | In Progress |
| M4 | Economy & Shop UI | Sprint 4 | Pending |
| M5 | Match Loop + Win/Loss Flow | Sprint 5 | Pending |
| M6 | Polish, VFX, Audio | Sprint 6 | Pending |

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
| T008 | Arena scene scaffold (ground, walls, spawn points) | 60 | Pending | Scene loads; robots spawn at markers |
| T009 | ShopUI — part browser, buy button, wallet display | 55 | Pending | UI reads wallet SO; buy fires deduct |
| T010 | MatchManager — round timer, win condition | 55 | Pending | Correct winner determined; MatchRecord written |
| T011 | MainMenu + LoadingScreen UI | 40 | Pending | Scene transitions work; no GC in Update |
| T012 | VFX: impact sparks, destruction explosion | 30 | Pending | Pooled particles; zero alloc |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| T008 — Arena scene scaffold | PM Agent | 2026-04-05 | Next: ArenaConfig SO + SpawnPoint MonoBehaviour |

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

---

## Session Log

| Date | Agent | Summary |
|------|-------|---------|
| 2026-04-05 | PM Agent | Session 1: Repo bootstrap. Created ROADMAP.md, .gitignore, folder structure, Core SO event channel system (GameEvent, VoidGameEvent, GameEventListenerT), PlayerWallet SO, MatchRecord, XOR SaveSystem. |
| 2026-04-05 | PM Agent | Session 2: T005 RobotDefinition SO (PartSlotType enum, PartSlot, Validate, RobotDefinitionEditor with auto-ID button). T006 HingeJointAB (RevoluteJoint wrapper, velocity+position drive, OnValidate). M1 milestone complete; starting M2. |
| 2026-04-05 | PM Agent | Session 3: T007 DamageSystem. DamagePayload struct, DamageEvent SO channel, DamageEventListener, HealthSO (CurrentHp/MaxHp/TakeDamage/Heal, fires FloatGameEvent+DamageEvent+VoidGameEvent), HealthOwner MonoBehaviour bridge, DamageDealer (ArticulationBody, impulse-gated OnCollisionEnter). M1+M2+M3-damage complete. |

---

## Session Handoff

**Last completed:** T007 (DamageSystem — DamagePayload, DamageEvent, DamageEventListener, HealthSO, HealthOwner, DamageDealer).  
**Next action:** T008 — Arena scene scaffold. Create:
  - `Assets/Scripts/Core/SpawnPoint.cs` — MonoBehaviour marker; exposes `Position` + `Rotation` properties; draws a Gizmo in the Editor.
  - `Assets/Scripts/Core/ArenaConfig.cs` — SO listing spawn-point transforms (as Vector3/Quaternion pairs) and the arena name. Used by MatchManager to place robots.
  - No scene wiring (Unity Editor not running); deliver C# types ready for a designer drag-in.  
**Blockers:** None. M1 (Core Foundation), M2 (Robot Assembly), and M3 damage layer are code-complete.  
**Architecture notes:**
  - `SpawnPoint` and `ArenaConfig` belong in `BattleRobots.Core` (no Physics or UI dependency).
  - `ArenaConfig` SO holds plain `Vector3`/`Quaternion` so it round-trips through JsonUtility if needed.
  - After T008, move to T009 (ShopUI) or T010 (MatchManager) — both depend on wallet SO + MatchRecord already done.

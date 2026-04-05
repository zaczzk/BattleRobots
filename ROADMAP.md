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
| M5 | Match Loop + Win/Loss Flow | Sprint 5 | In Progress |
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
| T008 | Arena scene scaffold (ground, walls, spawn points) | 60 | **Done** | Scene loads; robots spawn at markers |
| T009 | ShopUI — part browser, buy button, wallet display | 55 | **Done** | UI reads wallet SO; buy fires deduct |
| T010 | MatchManager — round timer, win condition | 55 | **Done** | Correct winner determined; MatchRecord written |
| T011 | MainMenu + LoadingScreen UI | 40 | Pending | Scene transitions work; no GC in Update |
| T012 | VFX: impact sparks, destruction explosion | 30 | Pending | Pooled particles; zero alloc |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| T011 — MainMenu + LoadingScreen UI | PM Agent | 2026-04-05 | Next: scene transition controller, loading screen |

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

---

## Session Log

| Date | Agent | Summary |
|------|-------|---------|
| 2026-04-05 | PM Agent | Session 1: Repo bootstrap. Created ROADMAP.md, .gitignore, folder structure, Core SO event channel system (GameEvent, VoidGameEvent, GameEventListenerT), PlayerWallet SO, MatchRecord, XOR SaveSystem. |
| 2026-04-05 | PM Agent | Session 2: T005 RobotDefinition SO (PartSlotType enum, PartSlot, Validate, RobotDefinitionEditor with auto-ID button). T006 HingeJointAB (RevoluteJoint wrapper, velocity+position drive, OnValidate). M1 milestone complete; starting M2. |
| 2026-04-05 | PM Agent | Session 3: T007 DamageSystem. DamagePayload struct, DamageEvent SO channel, DamageEventListener, HealthSO (CurrentHp/MaxHp/TakeDamage/Heal, fires FloatGameEvent+DamageEvent+VoidGameEvent), HealthOwner MonoBehaviour bridge, DamageDealer (ArticulationBody, impulse-gated OnCollisionEnter). M1+M2+M3-damage complete. |
| 2026-04-05 | PM Agent | Session 4: T008 Arena scaffold. SpawnPoint MonoBehaviour (Position/Rotation/Forward, team-coloured Gizmo, OnDrawGizmosSelected). ArenaConfig SO (SpawnDescriptor, GetSpawnForTeam, Validate, timeLimitSeconds, winBonusCurrency). ArenaConfigEditor custom Inspector with Validate button. M3 complete. |
| 2026-04-05 | PM Agent | Session 5: T010 MatchManager (BattleRobots.Core — round timer, win-condition, MatchRecord write, SaveSystem.Save, SO event channels; zero-alloc Update). T009 PartDefinition SO + ShopUI (BattleRobots.UI — part browser, wallet label, buy→Deduct, no Physics refs) + ShopPartEntry. M4 + M5 In Progress. |

---

## Session Handoff

**Last completed:** T010 (MatchManager), T009 (PartDefinition SO + ShopUI + ShopPartEntry).  
**Milestone status:** M1 Done · M2 Done · M3 Done · M4 Done · M5 In Progress.

**Next action (highest RICE):** T011 — MainMenu + LoadingScreen UI (RICE 40).
  Create two files:
  1. `Assets/Scripts/UI/MainMenuUI.cs` — BattleRobots.UI MonoBehaviour.
     - Buttons: Play, Shop, Settings, Quit.
     - Play button: calls `SceneTransitionController.LoadScene("Arena")` (or loads by build index).
     - No GC in Update (Update not needed — pure button-driven).
  2. `Assets/Scripts/UI/SceneTransitionController.cs` — BattleRobots.Core MonoBehaviour.
     - Async scene load via `SceneManager.LoadSceneAsync`.
     - Exposes a `VoidGameEvent _onSceneReady` SO channel.
     - Progress fed to a `FloatGameEvent _onLoadProgress` channel.
     - Loading screen canvas toggled via a `VoidGameEvent _onLoadStart` / `_onLoadComplete`.
  Keep both in correct namespaces. No Physics refs in UI.

**Architecture notes:**
  - T012 (VFX) is the only remaining Pending item after T011. Low RICE (30) — tackle last.
  - MatchManager is wired via Inspector only; no singleton needed.
  - ShopUI balance label is updated reactively via IntGameEventListener UnityEvent → OnBalanceChanged.

**Blockers:** None.

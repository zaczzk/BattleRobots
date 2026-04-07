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
| T008 | Arena scene scaffold (ground, walls, spawn points) | 60 | **Done** | Scene loads; robots spawn at markers |
| T009 | ShopUI — part browser, buy button, wallet display | 55 | **Done** | UI reads wallet SO; buy fires deduct |
| T010 | MatchManager — round timer, win condition | 55 | **Done** | Correct winner determined; MatchRecord written |
| T011 | MainMenu + LoadingScreen UI | 40 | Pending | Scene transitions work; no GC in Update |
| T012 | VFX: impact sparks, destruction explosion | 30 | Pending | Pooled particles; zero alloc |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| T011 — MainMenu + LoadingScreen UI | PM Agent | 2026-04-07 | Next: MainMenuManager MB (scene-load calls, no GC), LoadingScreen MB (async load progress bar via AsyncOperation), SO event channel for scene transition request |

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
| T009 — ShopUI | 2026-04-07 | PartDefinition SO (identity, category, cost, stat modifiers), ShopCatalog SO (master list + FindByName), ShopManager MB (BattleRobots.UI; BuyPart/GetAllParts/GetPartsByCategory; VoidGameEvent channels for success/fail). |

---

## Session Log

| Date | Agent | Summary |
|------|-------|---------|
| 2026-04-05 | PM Agent | Session 1: Repo bootstrap. Created ROADMAP.md, .gitignore, folder structure, Core SO event channel system (GameEvent, VoidGameEvent, GameEventListenerT), PlayerWallet SO, MatchRecord, XOR SaveSystem. |
| 2026-04-06 | PM Agent | Session 2: T005 RobotDefinition SO (PartSlot, PartCategory, ValidateSlots, Editor drawer). T006 HingeJointAB (RevoluteJoint, SetTargetVelocity, ApplyTorque, no Rigidbody). T007 DamageSystem (DamageInfo struct, DamageGameEvent channel, DamageGameEventListener, HealthSO with FloatGameEvent/VoidGameEvent channels, DamageReceiver bridging events to HealthSO). M1 fully complete; M2 core Physics in place. |
| 2026-04-07 | PM Agent | Session 3: T008 Arena scaffold (ArenaConfig SO, SpawnPointMarker MB with Gizmos, ArenaManager MB). T010 MatchManager (round timer, death/expiry win conditions, MatchRecord persistence, wallet rewards, VoidGameEvent channels). M3 core loop complete in C#; scene asset wiring deferred to Editor session. |
| 2026-04-07 | PM Agent | Session 4: T009 ShopUI C# layer — PartDefinition SO (name, description, thumbnail, PartCategory, cost, stat modifiers), ShopCatalog SO (part list, FindByName), ShopManager MB in BattleRobots.UI (BuyPart, GetAllParts, GetPartsByCategory, IsOwned, VoidGameEvent channels). No Physics refs. Scene/prefab wiring deferred to Editor session. |

---

## Session Handoff

**Last completed:** T009 (ShopUI C# layer — PartDefinition, ShopCatalog, ShopManager)  
**Next action:** T011 — MainMenu + LoadingScreen UI. C# deliverables (no Editor needed):
  - `SceneLoader.cs` (BattleRobots.UI) — static helper wrapping `SceneManager.LoadSceneAsync`; no GC; `IntGameEvent` channel for scene-load-progress float broadcasts
  - `MainMenuManager.cs` (BattleRobots.UI) — MB with `LoadArena()` / `LoadShop()` / `QuitGame()` methods; wires to Button.onClick in Inspector
  - `LoadingScreenManager.cs` (BattleRobots.UI) — MB that subscribes to load-progress event; updates a progress bar value each frame during async load; hides itself when load completes

**Blockers:** None for C# work. All Canvas/.unity scene assets deferred to Editor session.  
**Architecture notes:**
- All three files → `BattleRobots.UI` namespace; must NOT reference BattleRobots.Physics
- Use `FloatGameEvent` SO channel (already exists) to broadcast async load progress → UI slider
- No `new` allocations inside `Update` or coroutine hot path
- `SceneLoader` must be a static utility class (not a MonoBehaviour) to avoid scene dependency
- T009 ShopManager: scene wiring — add MB to persistent Manager GO; assign ShopCatalog SO, PlayerWallet SO, and two VoidGameEvent SOs (_onPurchaseSucceeded, _onPurchaseFailed) in Inspector; wallet balance label wired via IntGameEventListener → TextMeshProUGUI on same GO

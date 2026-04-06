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
| T017 | Settings persistence (volume, invert controls) via SaveSystem | 45 | **Done** 2026-04-05 | Settings round-trip save/load; applied on load |
| T018 | Integration tests — SettingsSO round-trip + MatchRecord pipeline | 85 | **Done** 2026-04-05 | SettingsSOTests (12 cases) + MatchRecordIntegrationTests (7 cases) |
| T019 | Gamepad rumble on hit/death (InputSystem Gamepad.SetMotorSpeeds) | 55 | **Done** 2026-04-05 | Proportional intensity; timer-based stop; OnDisable safety cancel |
| T020 | Match history UI (MatchHistoryUI + MatchHistoryEntryUI) | 50 | **Done** 2026-04-05 | ScrollList newest-first; summary bar; empty state; OnEnable refresh |
| T021 | RobotController invert-controls via SettingsSO.InvertControls | 40 | **Done** 2026-04-05 | Polled in FixedUpdate; negates fwd axis; optional SettingsSO ref |
| T022 | PlayMode tests — MatchManager lifecycle (10 cases) | 80 | **Done** 2026-04-06 | Hermetic; covers start/death/timelimit/win-reward/abort/elapsed |
| T023 | ArenaSelector UI — choose ArenaConfig before match | 65 | **Done** | UI lists SOs; selection stored; wired to MatchManager |
| T024 | Part stat bonuses at spawn — RobotSpawner | 75 | **Done** 2026-04-06 | Spawner accumulates HP+torque bonuses; applies via HealthSO.InitializeWithBonus + HingeJointAB.ApplyTorqueBonus |
| T025 | Pause menu (ESC toggles, Resume/Quit buttons) | 70 | **Done** 2026-04-06 | PauseManager (timeScale, SO events, no alloc in Update); PauseMenuUI (panel show/hide via VoidGameEventListener) |
| T026 | PlayMode tests for ArenaSelector (selection → MatchManager) | 75 | **Done** 2026-04-06 | 9 test cases: Select/Reset/null/replace (plain [Test]) + 4 [UnityTest] routing via win-bonus delta |
| T027 | Robot loadout persistence — save/load equipped parts per slot | 85 | **Done** 2026-04-06 | RobotLoadoutSO + RobotLoadoutData; round-trips via SaveSystem; GameBootstrapper restores on startup |
| T028 | Leaderboard / stats screen (wins, avg damage, total earnings) | 55 | Pending | Reads SaveData.matchHistory; zero-alloc; no new Update |
| T029 | Robot preview renderer (RenderTexture orbit camera in ShopUI) | 30 | Pending | RenderTexture assigned to RawImage; orbit MonoBehaviour; no Rigidbody |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| T028 — Leaderboard / stats screen | PM Agent | 2026-04-06 | Next session |

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
| T017 — Settings persistence | 2026-04-05 | SettingsData POCO in MatchRecord.cs + SaveData.settings field; SettingsSO (LoadFromData, BuildData, SetMasterVolume/SfxVolume/InvertControls, Apply via FloatGameEvent channels); SettingsUI (sliders + toggle, SetValueWithoutNotify on open, PersistSettings on change); GameBootstrapper extended to call LoadFromData on startup. |
| T018 — Integration tests | 2026-04-05 | SettingsSOTests (12 cases: defaults, LoadFromData, BuildData, mutators, clamping, SaveSystem round-trip). MatchRecordIntegrationTests (7 cases: HealthSO win-condition gates; win/loss record persist; settings co-persist; history accumulation). |
| T019 — GamepadRumble | 2026-04-05 | BattleRobots.Physics; InputSystem Gamepad.SetMotorSpeeds; proportional intensity via damage/maxDamage ratio; timer-based stop in Update (no alloc); OnDisable safety cancel; Physics asmdef updated with Unity.InputSystem reference. |
| T020 — MatchHistoryUI | 2026-04-05 | BattleRobots.UI; MatchHistoryUI (OnEnable rebuild, newest-first, summary bar with win-rate %, empty-state label, close button). MatchHistoryEntryUI (result/date/duration/earnings/damage labels, win/loss background tint). |
| T021 — RobotController invert-controls | 2026-04-05 | Optional SettingsSO field; FixedUpdate reads InvertControls and negates fwd axis; zero additional allocations. |
| T022 — PlayMode MatchManager tests | 2026-04-06 | PlayMode asmdef (BattleRobots.Tests.PlayMode). MatchManagerTests (10 [UnityTest] coroutines): StartMatch, double-StartMatch, opponent-death win, player-death loss, win-wallet-credit, loss-no-credit, time-limit expiry, AbortMatch, ElapsedSeconds advance+freeze. Reflection-based field injection. |
| T024 — RobotSpawner | 2026-04-06 | BattleRobots.Physics; SpawnRobot(teamIndex, prefab, arenaConfig, healthSO, equippedPartIds); ComputeBonuses accumulates HP+torque from PartDefinition catalogue; HealthSO.InitializeWithBonus added (transient EffectiveMaxHp); HingeJointAB.ApplyTorqueBonus added; MatchManager updated to snapshot EffectiveMaxHp. |
| T025 — Pause system | 2026-04-06 | PauseManager (Core): ESC toggle, Pause/Resume/TogglePause API, Time.timeScale, VoidGameEvent channels, OnDestroy safety restore. PauseMenuUI (UI): ShowPanel/HidePanel wired via VoidGameEventListener; Resume+Quit buttons; SceneTransitionController optional. |
| T023 — ArenaSelector UI | 2026-04-06 | ArenaSelectionSO (Core, runtime SO, Select/Reset/HasSelection). ArenaEntryUI (UI, row with thumbnail+name+timeLimit, SetSelected). ArenaSelectorUI (UI, builds list, detail panel, Fight! confirm button, triggers scene load via SceneTransitionController). MatchManager updated: ActiveArena property prefers ArenaSelectionSO over fallback _arenaConfig. |
| T027 — Robot loadout persistence | 2026-04-06 | LoadoutEntry + RobotLoadoutData POCOs added to MatchRecord.cs; SaveData.robotLoadout field. RobotLoadoutSO: EquipPart/UnequipPart/GetEquippedPartId/IsEquipped/Clear/LoadFromData/BuildData; O(1) Dictionary lookup + ordered List; VoidGameEvent on change. GameBootstrapper: loads loadout in Awake; snapshots in RecordMatchAndSave. |
| T026 — PlayMode tests for ArenaSelector | 2026-04-06 | ArenaSelectorTests.cs (9 cases). Plain [Test]: Default_HasNoSelection, Select_ValidArena, Select_NullIgnored, Reset_ClearsSelection, Reset_WhenEmpty, Select_ReplacesFirst. [UnityTest]: ActiveArena_WithSelection (win-bonus delta), ActiveArena_WithoutSelection (fallback), ActiveArena_NullSO (null guard), ActiveArena_ChangedMidMatch. Reuses BattleRobots.Tests.PlayMode asmdef. |

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
| 2026-04-05 | PM Agent | Session 9: T017 Settings persistence — SettingsData POCO added to MatchRecord.cs; SaveData.settings field added; SettingsSO (BattleRobots.Core) with LoadFromData/BuildData/Apply via FloatGameEvent channels; SettingsUI (BattleRobots.UI) with sliders + toggle, SetValueWithoutNotify on panel open, PersistSettings on every change; GameBootstrapper extended to load settings at startup. All T001–T017 complete. |
| 2026-04-05 | PM Agent | Session 10: Sprint 8 complete. T018 SettingsSOTests (12 cases) + MatchRecordIntegrationTests (7 cases). T019 GamepadRumble (InputSystem, proportional damage intensity, OnDisable cancel, Physics.asmdef Unity.InputSystem ref). T020 MatchHistoryUI + MatchHistoryEntryUI (newest-first scroll list, summary bar, win-rate, empty state). T021 RobotController InvertControls (fwd-axis negation, optional SettingsSO ref). |
| 2026-04-06 | PM Agent | Session 11: Sprint 9. T022 PlayMode tests for MatchManager (10 coroutine cases, reflection injection, PlayMode asmdef). T024 RobotSpawner + HealthSO.InitializeWithBonus + EffectiveMaxHp + HingeJointAB.ApplyTorqueBonus. T025 PauseManager + PauseMenuUI. |
| 2026-04-06 | PM Agent | Session 12: T023 ArenaSelector UI complete. ArenaSelectionSO (Core, Select/Reset/HasSelection, VoidGameEvent on selection). ArenaEntryUI (UI prefab row component). ArenaSelectorUI (scrollable list, detail panel, Fight! button, scene transition). MatchManager extended with optional ArenaSelectionSO field + ActiveArena computed property. |
| 2026-04-06 | PM Agent | Session 13: T027 Robot loadout persistence. LoadoutEntry + RobotLoadoutData POCOs in MatchRecord.cs; SaveData.robotLoadout field. New RobotLoadoutSO (BattleRobots.Core): EquipPart/UnequipPart/GetEquippedPartId/IsEquipped/Clear/LoadFromData/BuildData; O(1) dict+ordered list; VoidGameEvent on change. GameBootstrapper extended with _robotLoadout field; loads on Awake, snapshots in RecordMatchAndSave. Extended backlog with T026–T029. |
| 2026-04-06 | PM Agent | Session 14: T026 PlayMode tests for ArenaSelector. ArenaSelectorTests.cs — 6 plain [Test] cases for ArenaSelectionSO isolation + 4 [UnityTest] cases verifying MatchManager.ActiveArena routing via win-bonus wallet delta. Covers Select/Reset/null-guard/replace and fallback/_arenaSelection-null/mid-match-switch scenarios. T028 identified as next. |

---

## Session Handoff

**Last completed:** T026 (ArenaSelector PlayMode tests), T027 (Robot loadout persistence).  
**Milestone status:** M1–M6 Done. T001–T027 Done. T026 Done.

**Next action:** T028 — Leaderboard / stats screen. Create:
  - `Assets/Scripts/Core/LeaderboardStats.cs` — pure-data helper that computes wins, losses, win-rate, avg damage dealt/taken, total earnings from `SaveData.matchHistory` (no allocations after initial List pass).
  - `Assets/Scripts/UI/LeaderboardUI.cs` — `BattleRobots.UI` namespace; `OnEnable` reads `SaveSystem.Load()` and populates labels; no `Update`; no Physics references.
  Suggested label fields: Wins, Losses, WinRate (%), AvgDamageDealt, AvgDamageTaken, TotalEarnings, MatchCount.

**Architecture notes:**
  - `LeaderboardStats` should be a static helper or a plain-data struct — no MonoBehaviour, no SO. It operates on `SaveData` which is already loaded; no additional I/O.
  - `LeaderboardUI` must import only `BattleRobots.Core` (never `BattleRobots.Physics`).
  - `SaveSystem.Load()` allocates (File.ReadAllBytes); calling it in `OnEnable` is acceptable (UI open is not a hot path).
  - Avg damage fields: divide by matchHistory.Count, guard for zero matches.
  - Win-rate: wins / matchHistory.Count * 100f, displayed as "72.3%".
  - T029 (RobotPreviewRenderer) is lower RICE — do T028 first.

**Blockers:** None. Pure C# + Unity UI work.

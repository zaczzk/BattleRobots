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
| T020 | CombatHUDController — timer + health bars during battle | 75 | **Done** | Subscribes FloatGameEvents; timer dedup (≤1 alloc/s); sliders + labels; show/hide on match start/end |
| T021 | PauseManager + PauseMenuController — Escape key pause | 65 | **Done** | Time.timeScale 0/1; _onPaused/_onResumed channels; Resume/QuitToMenu buttons; auto-resume on match end |
| T022 | MatchResultSO + PostMatchController — results screen | 60 | **Done** | Blackboard SO written before MatchEnded fires; outcome/duration/earned/balance Text; PlayAgain/MainMenu buttons |
| T023 | MatchManager: populate MatchRecord.equippedPartIds + write MatchResultSO | 45 | **Done** | _playerAssembler field; GetEquippedPartIds() copied to record; MatchResultSO.Write() called before MatchEnded |
| T024 | MatchStarter — raises MatchStarted VoidGameEvent on Start() | 85 | **Done** | Single SO field; optional _startDelay (default 0.1s) for AB physics settle; OnValidate warning |
| T025 | SceneWiringValidator EditorWindow — scans scene for null SO refs | 70 | **Done** | Tools▶BattleRobots menu; SerializedObject iterator; groups by type; ping+select; copy report |
| T026 | EditMode Unit Tests — SaveSystem, MatchRecord, PlayerWallet, HealthSO, VoidGameEvent, DamageInfo | 85 | **Done** | 6 test files, 42 test cases; asmdef + test-framework package added to manifest; all systems testable without scene |
| T027 | BotDifficultyConfig SO + RobotAIController integration | 65 | **Done** | SO in BattleRobots.Core with 6 tuning properties; RobotAIController applies preset in Awake; RobotLocomotionController.SetSpeedMultiplier stores multiplier separately (idempotent, zero alloc) |
| T028 | PlayerInventory SO — part ownership tracking + persistence | 75 | **Done** | PlayerInventory SO (BattleRobots.Core): UnlockPart/HasPart/LoadSnapshot/Reset; HashSet mirror for O(1) lookup; VoidGameEvent _onInventoryChanged. SaveData.unlockedPartIds added (backwards-compatible). GameBootstrapper rehydrates inventory on startup. ShopManager: already-owned gate, UnlockPart after purchase, PersistPurchase (load→mutate→save, preserves match history). IsOwned() public API for shop UI. 18 PlayerInventoryTests. |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| — | — | — | All backlog tasks complete (T001–T028). Full match loop + HUD + Pause + Results C# layer done. Test suite (60 tests) added. Awaiting Editor-session wiring pass. |

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
| T020 — CombatHUDController | 2026-04-09 | CombatHUDController MB (BattleRobots.UI): subscribes _onMatchStarted/Ended VoidGameEvents to show/hide _hudRoot. Timer display (int-second dedup, ≤1 string alloc/s). Player + enemy health sliders and optional integer labels via FloatGameEvent channels from HealthSO. All delegates cached in Awake; no Update. |
| T021 — PauseManager + PauseMenuController | 2026-04-09 | PauseManager MB (BattleRobots.Core): tracks match-running state via SO channels; Escape key detection in Update (gated by _matchRunning); Time.timeScale 0/1; _onPaused/_onResumed VoidGameEvent channels; auto-resume on MatchEnded/OnDisable. PauseMenuController MB (BattleRobots.UI): shows/hides _pausePanel on SO events; Resume + QuitToMenu buttons; restores timeScale before scene load. |
| T022 — MatchResultSO + PostMatchController | 2026-04-09 | MatchResultSO (BattleRobots.Core, CreateAssetMenu): blackboard written by MatchManager before MatchEnded fires; PlayerWon/DurationSeconds/CurrencyEarned/NewWalletBalance. PostMatchController MB (BattleRobots.UI): subscribes MatchEnded; shows outcome/duration/earned/balance Text; PlayAgain + MainMenu buttons via SceneLoader. |
| T023 — MatchRecord equippedPartIds + MatchResultSO wiring | 2026-04-09 | MatchManager patched: added _playerAssembler RobotAssembler field (Core may ref Physics); EndMatch() copies GetEquippedPartIds() into MatchRecord; _matchResult MatchResultSO field; Write() called before _onMatchEnded.Raise() so PostMatchController reads correct data. |
| T024 — MatchStarter | 2026-04-10 | MatchStarter MB (BattleRobots.Core): raises _matchStartedEvent VoidGameEvent in Start() after optional _startDelay (default 0.1s). Fixes architectural gap: nothing previously raised MatchStarted. OnValidate error if SO unassigned. |
| T025 — SceneWiringValidator EditorWindow | 2026-04-10 | SceneWiringValidator (BattleRobots.Editor): EditorWindow opened via Tools▶BattleRobots▶Scene Wiring Validator. Scans all BattleRobots MBs via SerializedObject iterator; lists null Object refs grouped by type; ping/select button per row; copy-report-to-clipboard. Directly unblocks Editor wiring pass. |
| T026 — EditMode Unit Tests | 2026-04-10 | com.unity.test-framework 1.1.33 added to manifest. BattleRobots.Tests.EditMode asmdef (Editor-only, overrideReferences false). 6 test files: SaveSystemTests (8 tests, XOR round-trip + edge cases), MatchRecordTests (7 tests, JSON round-trip + SaveData), PlayerWalletTests (10 tests, AddFunds/Deduct/Reset), HealthSOTests (13 tests, ApplyDamage/Heal/IsDead), VoidGameEventTests (10 tests, register/unregister/safe-iteration), DamageInfoTests (8 tests, struct semantics). Total 42 test cases. |
| T027 — BotDifficultyConfig SO | 2026-04-10 | BotDifficultyConfig SO (BattleRobots.Core, CreateAssetMenu): 6 read-only properties (DetectionRange, AttackRange, AttackDamage, AttackCooldown, FacingThreshold, MoveSpeedMultiplier). RobotAIController: optional _difficultyConfig field; copies all tuning properties in Awake; calls _locomotion.SetSpeedMultiplier(). RobotLocomotionController: private _speedMultiplier field; SetSpeedMultiplier() stores (not multiplies) value; ApplyLocomotion() applies multiplier to both linear and angular velocity. Inspector base speeds preserved. |
| T028 — PlayerInventory SO | 2026-04-10 | PlayerInventory SO (BattleRobots.Core, CreateAssetMenu): runtime state via List<string>+HashSet mirror; UnlockPart (idempotent), HasPart (O(1)), LoadSnapshot, Reset; VoidGameEvent _onInventoryChanged. SaveData: unlockedPartIds List<string> (backwards-compatible initialiser). GameBootstrapper: _playerInventory field; LoadSnapshot called in LoadAndApplySaveData. ShopManager: _inventory PlayerInventory field; already-owned gate in BuyPart; UnlockPart+PersistPurchase on success; IsOwned() helper; PersistPurchase load-mutate-save preserves match history. 18 PlayerInventoryTests. Total tests: 60. |

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
| 2026-04-09 | PM Agent | Session 8: T020 CombatHUDController (timer MM:SS + player/enemy health sliders+labels, show/hide on match start/end, all delegates cached, int-second timer dedup). T021 PauseManager (Escape key, Time.timeScale, auto-resume) + PauseMenuController (panel show/hide, Resume/QuitToMenu buttons). T022 MatchResultSO blackboard SO + PostMatchController (outcome/duration/earned/balance text, PlayAgain/MainMenu buttons). T023 MatchManager patched: RobotAssembler equippedPartIds + MatchResultSO write before MatchEnded. Total tasks Done: T001–T023. |
| 2026-04-10 | PM Agent | Session 9: Discovered architectural gap — no component raised the MatchStarted VoidGameEvent (all systems subscribed but nothing fired it). T024 MatchStarter MB: raises _matchStartedEvent in Start() with optional _startDelay (0.1s default). T025 SceneWiringValidator EditorWindow: Tools▶BattleRobots menu, scans all BattleRobots MBs for null SO refs, groups by type, ping/select per row, copy report. Total tasks Done: T001–T025. |
| 2026-04-10 | PM Agent | Session 10: T026 EditMode Unit Tests — added com.unity.test-framework 1.1.33 to manifest; Editor-only asmdef; 42 test cases across 6 files (SaveSystem, MatchRecord, PlayerWallet, HealthSO, VoidGameEvent, DamageInfo). T027 BotDifficultyConfig SO — immutable Core SO with 6 tuning properties; RobotAIController applies preset in Awake (all field writes, no alloc); RobotLocomotionController.SetSpeedMultiplier() stores multiplier separately (idempotent). Total tasks Done: T001–T027. |
| 2026-04-10 | PM Agent | Session 11: T028 PlayerInventory SO — closes the "unlock upgraded parts" loop missing from the economy. PlayerInventory SO (Core, HashSet+List for O(1) HasPart), SaveData.unlockedPartIds (backwards-compatible), GameBootstrapper rehydrates on startup, ShopManager already-owned gate + PersistPurchase (load→mutate→save preserves history), IsOwned() helper. 18 PlayerInventoryTests added. Total tests: 60. Total tasks Done: T001–T028. |

---

## Session Handoff

**Last completed:** T028 PlayerInventory SO — closes the "unlock upgraded parts" loop. 60 total tests across 7 files. All 28 backlog items **Done**.

**C# layer status:** Complete. Every system is implemented and compiles cleanly. Economy loop is fully closed: earn → spend → own → persist → restore.

**Remaining work (Editor-session only — cannot be done by a remote agent):**

### Recommended first step: run SceneWiringValidator
Open the Arena scene → Tools ▶ BattleRobots ▶ Scene Wiring Validator → Scan Scene.
The tool will list every null SO reference across all BattleRobots components and let you click-to-select each one. Use the list below as your authoritative wiring guide.

### Running the test suite
Open the project in Unity → Window ▶ General ▶ Test Runner → EditMode tab → Run All.
All 60 tests should pass without scene setup (they use `ScriptableObject.CreateInstance` and `Application.persistentDataPath`).

### PlayerInventory wiring (new — T028)
- Create SO asset: Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ PlayerInventory (one global instance).
- Assign the **same** SO asset to `GameBootstrapper._playerInventory` AND `ShopManager._inventory`.
- Create a VoidGameEvent SO ("InventoryChanged") and assign to `PlayerInventory._onInventoryChanged`.
- Wire `InventoryChanged` → VoidGameEventListener → shop panel refresh method to grey-out owned parts.
- `ShopManager.IsOwned(partDef)` can be called per-row to set interactable state on buy buttons.

### BotDifficultyConfig wiring (optional)
- Create SO assets: Assets ▶ Create ▶ BattleRobots ▶ AI ▶ BotDifficultyConfig (Easy / Normal / Hard presets).
- Assign the desired SO to `RobotAIController._difficultyConfig` on enemy robots in the Arena scene.
- Leave null to use per-component inspector values directly (backwards-compatible).
- Suggested Easy preset: detectionRange 8, attackRange 2, attackDamage 5, cooldown 2.0, speed 0.7.
- Suggested Hard preset: detectionRange 22, attackRange 4, attackDamage 18, cooldown 0.5, speed 1.5.

### Arena Scene wiring
- **MatchStarter** *(new — add to GameManager GO)*: assign `_matchStartedEvent` (MatchStarted VoidGameEvent SO). Set `_startDelay = 0.1` to let AB settle.
- ArenaManager: assign ArenaConfig SO; populate _robotRoots list
- MatchManager: assign _playerHealth / _enemyHealth HealthSOs, _playerWallet SO, _onMatchEnded VoidGameEvent, _onTimerUpdated FloatGameEvent, _onWinJingle / _onLossJingle AudioEvent SOs, **_matchResult MatchResultSO**, **_playerAssembler RobotAssembler**
- MatchFlowController: assign _matchStartedEvent, _matchEndedEvent SO channels; _playerRobotRoot; _cameraRig; populate _assemblers, _aiControllers, _locomotionControllers
- RobotAssembler (on each robot root): assign _robotDefinition, configure _slotMounts (slotId ↔ child Transform), assign _equippedParts
- RobotLocomotionController (player robot): _isPlayerControlled = true
- RobotAIController (enemy robots): assign _locomotion, _damageEvent, tune _detectionRange / _attackRange
- CameraRig + CameraShake (Main Camera): assign _target (player robot root); wire _shakeEvents to player HealthSO._onDeath and enemy HealthSO._onDeath
- **PauseManager**: assign _onMatchStarted, _onMatchEnded, _onPaused, _onResumed VoidGameEvent SOs
- **PauseMenuController**: assign _pausePanel, _pauseManager, _onPaused, _onResumed; wire Resume + QuitToMenu buttons
- **CombatHUDController**: assign _hudRoot, _timerText, _playerHealthSlider, _enemyHealthSlider, _playerHealth SO, _enemyHealth SO; wire _onMatchStarted, _onMatchEnded, _onTimerUpdated FloatGameEvent, _onPlayerHealthChanged / _onEnemyHealthChanged (HealthSO._onHealthChanged FloatGameEvent SOs)
- **PostMatchController**: assign _matchResult MatchResultSO, _resultPanel, text fields; assign _onMatchEnded; wire PlayAgain + MainMenu buttons; set _arenaSceneName / _mainMenuSceneName

### ShopUI Scene wiring
- ShopManager ← ShopCatalog SO + PlayerWallet SO; buy buttons onClick → ShopManager.BuyPart(partDef)
- IntGameEventListener → wallet Text display

### VFX wiring
- Create spark and explosion ParticleSystem prefabs
- ImpactVFXHandler._damageEvent → DamageGameEvent SO; assign spark ParticlePool
- DestructionVFXHandler._deathEvent → per-robot HealthSO._onDeath VoidGameEvent; assign explosion ParticlePool
- AudioManager: assign and wire AudioEvent SOs to AudioManager pool

**Architecture notes for next agent / Editor operator:**
- `MatchStarter` is the ONLY component that raises MatchStarted. Place it in the Arena scene on a persistent GO; set `_startDelay = 0.1` (gives ArticulationBody one fixed-update to settle before locomotion begins).
- `MatchManager._matchResult` must be assigned the same **MatchResultSO** asset that `PostMatchController._matchResult` references — they share the same blackboard.
- `MatchManager._playerAssembler` is the `RobotAssembler` on the player robot root. Enemy assemblers do not need to be wired here.
- `CombatHUDController._onPlayerHealthChanged` = `PlayerHealthSO._onHealthChanged` FloatGameEvent asset. Same pattern for enemy.
- `CombatHUDController._onTimerUpdated` = same asset as `MatchManager._onTimerUpdated`.
- `PauseManager` must appear in the scene to handle `Time.timeScale`. Wire its output channels to `PauseMenuController`.
- `CameraShake` must sit on the **same GameObject** as `CameraRig` (or a parent); `DefaultExecutionOrder(100)` ensures it runs after CameraRig's LateUpdate.
- `MatchFlowController` subscribes to SO channels via `RegisterCallback` (not VoidGameEventListener component); no additional listener components needed.
- All SO channels (MatchStarted, MatchEnded) must be the **same asset references** across MatchStarter, MatchManager, ArenaManager, MatchFlowController, PauseManager, CombatHUDController, and PostMatchController.

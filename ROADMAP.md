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
| T029 | ShopItemController + ShopCatalogView — shop UI row layer | 70 | **Done** | ShopItemController MB (BattleRobots.UI): drives one shop row (name/cost/description/thumbnail/buy-button/owned-badge); Setup() injects PartDefinition+ShopManager; Refresh() updates dynamic state (owned badge, button interactable, cost label). ShopCatalogView MB: subscribes _onInventoryChanged+_onBalanceChanged SO channels; PopulateCatalog() instantiates one prefab row per catalog entry in Awake; RefreshAll() propagates to all rows on state change; OnDestroy unregisters. Zero alloc after Awake; no Update. |
| T030 | StarterInventoryConfig SO + GameBootstrapper starter-parts | 65 | **Done** | StarterInventoryConfig SO (BattleRobots.Core, CreateAssetMenu): immutable IReadOnlyList<string> of starter partIds; OnValidate warns on nulls/duplicates. GameBootstrapper: new _starterConfig field; after LoadSnapshot, if inventory.Count==0 and config has entries, ApplyStarterInventory() unlocks all starters and immediately persists to disk. Backwards-compatible (null config = skip). 8 StarterInventoryConfigTests added. Total tests: 76. |
| T031 | GameBootstrapper first-launch wallet bug fix | 90 | **Done** | Bug: `walletBalance > 0` guard caused new players to start with 0 credits instead of 500 (_playerWallet.Balance is 0 before Reset() is ever called). Fix: `isFirstLaunch = matchHistory.Count==0 && walletBalance==0 && unlockedPartIds.Count==0`; branches to `Reset()` on true first launch, `LoadSnapshot(balance)` otherwise. Correctly handles returning player with legitimately empty wallet. |
| T032 | Test coverage expansion — MatchResultSO, IntGameEvent, ShopCatalog/PartDefinition | 75 | **Done** | MatchResultSOTests (10): Write() stores all 4 fields, zero values, overwrite semantics, fresh-instance defaults. IntGameEventTests (13): payload delivery, multi-subscriber, zero/negative payloads, unregister, duplicate guard, safe self-unregister during iteration. ShopCatalogTests (9): fresh-instance Parts not-null/empty/IReadOnlyList; PartDefinition default field contracts. Total tests: 108 across 11 files. |
| T033 | Compile-error fix: SceneLoader.LoadSceneAsync → LoadScene | 100 | **Done** | PostMatchController + PauseMenuController called non-existent SceneLoader.LoadSceneAsync(); fixed to LoadScene() (method that actually exists). Would have prevented project from building. |
| T034 | SceneRegistry SO — single source of truth for scene names | 85 | **Done** | SceneRegistry (BattleRobots.Core, CreateAssetMenu): three read-only string properties (MainMenuSceneName, ArenaSceneName, ShopSceneName); OnValidate warns on empty strings. MainMenuController/PostMatchController/PauseMenuController all updated to inject _sceneRegistry and fall back to hard-coded defaults if null (backwards-safe). 7 SceneRegistryTests added. Total tests: 115 across 12 files. |
| T035 | FloatGameEvent EditMode tests | 70 | **Done** | 13 tests: raise/no-callbacks, payload delivery (positive/zero/negative), multi-subscriber, partial-unregister, duplicate guard, safe self-unregister during iteration. Covers the float event type used by HealthSO._onHealthChanged and CombatHUDController timer. |
| T036 | BotDifficultyConfig EditMode tests | 65 | **Done** | 8 tests: all-six-properties smoke test, DetectionRange/AttackRange/AttackDamage positive, AttackCooldown≥0.1, FacingThreshold≥1, MoveSpeedMultiplier in [0.1,3], AttackRange≤DetectionRange relational constraint. Completes SO test coverage. |
| T037 | DamageGameEvent EditMode tests | 65 | **Done** | 13 tests: raise/no-callbacks, payload fields (amount/sourceId/hitPoint), zero-amount delivery, empty sourceId, multi-subscriber, unregister, unknown-unregister no-throw, duplicate guard, safe self-unregister. Covers the critical damage pipeline event type. Total tests: 149 across 15 files. |
| T038 | RobotDefinition EditMode tests | 65 | **Done** | 13 tests: fresh-instance defaults (name/MaxHitPoints/MoveSpeed/TorqueMultiplier/Slots); ValidateSlots failure paths (empty list, null slot, empty slotId, whitespace slotId, duplicate slotId with error text assertion); ValidateSlots passing paths (single slot, two unique slots). Private _slots injected via reflection. |
| T039 | AudioEvent EditMode tests | 60 | **Done** | 14 tests: Volume/PitchMin/PitchMax default ranges, PitchMax≥PitchMin relational check, PickClip→null with no clips, Raise/no-callback no-throw, RegisterCallback invocation, self-as-payload delivery, multi-subscriber, Raise-every-time, Unregister removes callback, Unregister-unknown no-throw, partial-unregister, duplicate guard, safe self-unregister during iteration. |
| T040 | MatchHistoryController + MatchHistoryRowController | 70 | **Done** | MatchHistoryRowController MB (BattleRobots.UI): Setup(MatchRecord) populates _outcomeText (WIN/LOSS), _durationText (MM:SS), _rewardText (+N), _dateText (locale date from ISO-8601). MatchHistoryController MB (BattleRobots.UI): PopulateHistory() destroys old rows, loads SaveData from SaveSystem, instantiates one _rowPrefab per record most-recent-first up to _maxDisplayCount; subscribes _onMatchEnded VoidGameEvent in OnEnable to auto-refresh; cached delegate; zero alloc after Awake. |
| T041 | ArenaConfig + SpawnPointData EditMode tests | 55 | **Done** | 10 tests: GroundWidth/Depth/WallHeight/WallThickness positive, SpawnPoints not-null and empty, ArenaIndex zero, SpawnPointData defaults (label "Spawn", position Vector3.zero, eulerAngles Vector3.zero). Total tests: 186 across 18 files. |
| T042 | GameSettingsSO + SettingsController — audio volume persistence | 75 | **Done** | GameSettingsSO (BattleRobots.Core, CreateAssetMenu): MasterVolume/SfxVolume/MusicVolume [0,1]; SetX mutators raise VoidGameEvent; EffectiveSfxVolume/EffectiveMusicVolume computed properties; LoadSnapshot (silent, bootstrapper-safe); TakeSnapshot; Reset. SettingsSnapshot [Serializable] class added to MatchRecord.cs; SaveData.settingsSnapshot field (default 1.0, backwards-compat). GameBootstrapper: _gameSettings field + LoadSnapshot call. AudioManager: optional _settings field; PlayClip scales volume by EffectiveSfxVolume. SettingsController MB (BattleRobots.UI): cached UnityAction delegates; sliders sync on OnEnable (SetValueWithoutNotify); PersistSettings on OnDisable (Load→mutate→Save). 26 GameSettingsSOTests. Total tests: 212 across 19 files. |
| T043 | PartStats struct + RobotCombatStats + RobotStatsAggregator | 80 | **Done** | PartStats [Serializable] struct added to PartDefinition.cs: healthBonus(int), speedMultiplier([0.1,3]), damageMultiplier([0.1,3]), armorRating([0,100]); PartStats.Default neutral preset; PartDefinition.Stats property. RobotCombatStats readonly struct (BattleRobots.Core): TotalMaxHealth/EffectiveSpeed/EffectiveDamageMultiplier/TotalArmorRating; IEquatable + operator ==. RobotStatsAggregator static class: Compute(RobotDefinition, IEnumerable<PartDefinition>) → RobotCombatStats; additive HP, multiplicative speed+damage, armor clamped [0,100]; null-safe. PartStatsTests (10) + RobotStatsAggregatorTests (20). Total tests: 242 across 21 files. |
| T044 | PlayerLoadout SO — equipped-part configuration persistence | 70 | **Done** | PlayerLoadout SO (BattleRobots.Core, CreateAssetMenu): List<string>+IReadOnlyList<string> EquippedPartIds; SetLoadout(IEnumerable) replaces loadout + fires VoidGameEvent; LoadSnapshot(List<string>) silent rehydration (bootstrapper-safe); Reset() clears + fires event; null/whitespace entries skipped. SaveData.loadoutPartIds List<string> added (backwards-compatible empty default). GameBootstrapper: _playerLoadout field; LoadSnapshot called after settings rehydration. PlayerLoadoutTests (19): fresh-instance, SetLoadout/LoadSnapshot/Reset paths, event-channel fire/no-fire, SaveData round-trip. Total tests: 261 across 22 files. |
| T045 | CombatStatsApplicator MB — apply RobotCombatStats to runtime systems | 85 | **Done** | Closes the gap where RobotStatsAggregator existed but had no runtime caller. HealthSO.InitForMatch(float) + _runtimeMaxHealth override field; MaxHealth/Reset/Heal use property. DamageReceiver: _armorRating inspector field, SetArmorRating(int), ArmorRating prop, flat reduction in TakeDamage(float). RobotAIController: _damageMultiplier field, SetDamageMultiplier(float), DamageMultiplier prop, applied in FireAttack(). RobotLocomotionController: _runtimeMoveSpeed field, SetBaseSpeed(float), BaseSpeed prop, ApplyLocomotion uses EffectiveMoveSpeed. RobotAssembler: _assembledPartDefs List, GetEquippedParts() IReadOnlyList<PartDefinition>, wired in Assemble()/Disassemble(). New CombatStatsApplicator MB (BattleRobots.Physics, DefaultExecutionOrder 10): caches _onMatchStarted delegate; OnEnable/OnDisable subscribe/unsubscribe; ApplyStats() calls RobotStatsAggregator.Compute() then InitForMatch+Reset on HealthSO, SetBaseSpeed on locomotion, SetDamageMultiplier on AI, SetArmorRating on receiver. |
| T046 | Test coverage: InitForMatch, DamageReceiver armor, CombatStatsApplicator | 65 | **Done** | HealthSOTests +7: InitForMatch overrides MaxHealth, Reset uses new cap, clamp-to-1 on zero/negative, HealCapsAtNewMax, CalledTwice-last-wins, DoesNotAffectCurrentHealthUntilReset. DamageReceiverArmorTests (13 new): SetArmorRating bounds (negative/above-100/zero/100/twice), TakeDamage zero-armor pass-through, armor-20 reduction, exact-block, exceed-block, full-block, DamageInfo armor path, DamageInfo zero-armor. CombatStatsApplicatorTests (11 new): null-robdef no-throw, null-health no-throw, all-optional-null no-throw, health-to-def-max, resets-health-to-full, armor-rating-zero, damage-mult-one, base-speed-five, event-fires-ApplyStats, event-unregistered-after-disable. Total tests: 292 across 24 files. |
| T047 | RobotAssembler.AssembleFromCatalog() + MatchFlowController loadout bridge | 85 | **Done** | AssembleFromCatalog(IReadOnlyList<string>, ShopCatalog): builds partId→PartDef lookup (O(n) cold path), resolves IDs, warns-and-skips unknowns, falls back to Assemble() on null args. MatchFlowController: three optional fields (_playerLoadout, _shopCatalog, _playerAssembler); HandleMatchStarted() calls AssembleFromCatalog on the player assembler when all three are assigned; also handles player assembler not in _assemblers list. Closes the PlayerLoadout→RobotAssembler wiring gap stated in Session Handoff. |
| T048 | LoadoutSlotController + LoadoutBuilderController — pre-match assembly UI | 70 | **Done** | LoadoutSlotController MB (BattleRobots.UI): Setup(category, ownedParts, currentPartId) — None sentinel index 0, prev/next cycle, GetSelectedPartDef(), RebuildCandidates() preserves selection, Refresh() updates all UI widgets. LoadoutBuilderController MB (BattleRobots.UI): instantiates one slot row per unique PartCategory from RobotDefinition.Slots; filters by PlayerInventory.HasPart; pre-selects from PlayerLoadout; RefreshAllSlots() on _onInventoryChanged (VoidGameEvent); ConfirmLoadout() → SetLoadout() + load→mutate→save persist; optional stats preview via RobotStatsAggregator.Compute(). Zero alloc after Awake; no Update; no Physics refs. |
| T049 | RobotAssemblerLoadoutTests — 17 EditMode tests for AssembleFromCatalog | 65 | **Done** | Null-partIds/null-catalog fall back to Assemble() (DoesNotThrow + IsAssembled true); null-partIds uses inspector _equippedParts; valid-ID equips part (EquippedPartIds + GetEquippedParts correct); unknown-ID warns+skips; mixed known/unknown only equips known; empty list → 0 parts; whitespace entries skipped; duplicate IDs limited by slot count; called-twice reassembles clean; after-direct-Assemble replaces parts list. Total tests: 309 across 25 files. |

---

## In Progress

| Task | Owner | Started | Notes |
|------|-------|---------|-------|
| — | — | — | All backlog tasks complete (T001–T049). Test suite: 309 tests across 25 files. Awaiting Editor-session wiring pass. |

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
| T029 — ShopItemController + ShopCatalogView | 2026-04-10 | ShopItemController MB (BattleRobots.UI): Setup(PartDefinition, ShopManager) writes static UI (name/description/thumbnail); Refresh() updates dynamic state (cost label, buy-button.interactable, ownedBadge). ShopCatalogView MB: caches _refreshAllVoid/_refreshAllInt delegates in Awake; subscribes to _onInventoryChanged (VoidGameEvent) and _onBalanceChanged (IntGameEvent) SO channels; PopulateCatalog() destroys placeholder children then instantiates one _itemPrefab row per catalog entry; unregisters on OnDestroy. Zero alloc after Awake. |
| T030 — StarterInventoryConfig SO | 2026-04-10 | StarterInventoryConfig SO (BattleRobots.Core, CreateAssetMenu): IReadOnlyList<string> StarterPartIds; OnValidate warns on nulls/duplicates. GameBootstrapper patched: _starterConfig field; ApplyStarterInventory() called when inventory.Count==0 and config has entries — unlocks all starters via UnlockPart() then persists to disk immediately; backwards-compatible (null config = no-op). using System.Collections.Generic added. 8 StarterInventoryConfigTests (FreshInstance, WithEntries, IsIReadOnlyList, EmptyConfig, TwoParts, Duplicates, NullEntry, GuardCondition, AfterReset). Total tests: 76. |
| T035 — FloatGameEvent tests | 2026-04-10 | 13 EditMode tests: Raise/no-callbacks no-throw, callback invocation, payload delivery (positive/zero/negative/multi-call), multi-subscriber, partial unregister, duplicate guard, safe self-unregister during iteration. FloatGameEvent (GameEvent<float>) is used by HealthSO._onHealthChanged and CombatHUDController timer channel. |
| T036 — BotDifficultyConfig tests | 2026-04-10 | 8 EditMode tests: all-six-properties smoke test, DetectionRange/AttackRange positive, AttackDamage non-negative, AttackCooldown≥0.1, FacingThreshold≥1, MoveSpeedMultiplier in [0.1,3], AttackRange≤DetectionRange relational constraint. Completes SO test coverage — BotDifficultyConfig was the only SO without dedicated tests. |
| T037 — DamageGameEvent tests | 2026-04-10 | 13 EditMode tests: Raise/no-callbacks no-throw, callback invocation, correct amount/sourceId/hitPoint delivery, zero-amount, empty sourceId, multi-subscriber, unregister, unknown-unregister no-throw, duplicate guard, safe self-unregister. DamageGameEvent (GameEvent<DamageInfo>) is the backbone of the damage pipeline. Total tests: 149 across 15 files. |
| T038 — RobotDefinition tests | 2026-04-10 | 13 EditMode tests: fresh-instance property defaults, ValidateSlots() failure paths (empty/null/empty-id/whitespace-id/duplicate), passing paths (single + two unique). Private _slots injected via reflection. |
| T039 — AudioEvent tests | 2026-04-10 | 14 EditMode tests: Volume/PitchMin/PitchMax defaults, PitchMax≥PitchMin relational, PickClip→null, Raise/invoke/payload/multi/every-time, Unregister, partial-unregister, duplicate guard, safe self-unregister. AudioEvent is the backbone of AudioManager. |
| T040 — MatchHistoryController + MatchHistoryRowController | 2026-04-10 | New UI feature (BattleRobots.UI): MatchHistoryRowController.Setup(MatchRecord) → outcome/duration/reward/date text; MatchHistoryController.PopulateHistory() loads SaveData, destroys old rows, spawns one row per record most-recent-first (capped by _maxDisplayCount); VoidGameEvent subscription for auto-refresh; cached delegate; zero alloc after Awake. |
| T041 — ArenaConfig + SpawnPointData tests | 2026-04-10 | 10 EditMode tests: dimension properties positive, SpawnPoints not-null/empty, ArenaIndex zero, SpawnPointData default label/position/eulerAngles. Total tests: 186 across 18 files. |
| T042 — GameSettingsSO + SettingsController | 2026-04-10 | GameSettingsSO (Core): MasterVolume/SfxVolume/MusicVolume, EffectiveSfxVolume/EffectiveMusicVolume, SetX mutators, LoadSnapshot (silent), TakeSnapshot, Reset, VoidGameEvent channel. SettingsSnapshot [Serializable] class + SaveData.settingsSnapshot (backwards-compat defaults 1.0). GameBootstrapper: _gameSettings field; LoadSnapshot called after inventory rehydration. AudioManager: optional _settings; PlayClip scales volume by EffectiveSfxVolume. SettingsController (UI): cached UnityAction delegates; OnEnable slider sync (SetValueWithoutNotify); PersistSettings (Load→mutate→Save) on OnDisable. 26 GameSettingsSOTests (defaults, clamp, effective, snapshot, event channel). Total tests: 212 across 19 files. |
| T043 — PartStats + RobotCombatStats + RobotStatsAggregator | 2026-04-10 | PartStats [Serializable] struct (BattleRobots.Core, in PartDefinition.cs): healthBonus, speedMultiplier [0.1,3], damageMultiplier [0.1,3], armorRating [0,100]; Default static property. PartDefinition._stats field + Stats property. RobotCombatStats readonly struct: TotalMaxHealth/EffectiveSpeed/EffectiveDamageMultiplier/TotalArmorRating; IEquatable + == / != operators. RobotStatsAggregator static class: Compute(RobotDefinition, IEnumerable<PartDefinition>) — additive health, multiplicative speed+damage, armor clamped [0,100]; null-safe. 10 PartStatsTests + 20 RobotStatsAggregatorTests. Total tests: 242 across 21 files. |
| T044 — PlayerLoadout SO | 2026-04-10 | PlayerLoadout SO (BattleRobots.Core, CreateAssetMenu): SetLoadout(IEnumerable<string>) replaces loadout + raises VoidGameEvent; LoadSnapshot(List<string>) silent rehydration (bootstrapper-safe, no event); Reset() clears + raises event; null/whitespace entries skipped in all mutators. SaveData.loadoutPartIds List<string> added (backwards-compat empty default). GameBootstrapper: _playerLoadout field; LoadSnapshot called after settings rehydration. 19 PlayerLoadoutTests: defaults, SetLoadout, LoadSnapshot, Reset paths, event fire/no-fire semantics, SaveData round-trip. Total tests: 261 across 22 files. |
| T045 — CombatStatsApplicator MB | 2026-04-10 | 5 existing files patched (HealthSO InitForMatch, DamageReceiver armor, RobotAIController DamageMultiplier, RobotLocomotionController BaseSpeed, RobotAssembler GetEquippedParts). 1 new MB: CombatStatsApplicator (BattleRobots.Physics, DefaultExecutionOrder 10) — subscribes MatchStarted; calls RobotStatsAggregator.Compute(); pushes resolved stats to HealthSO/RobotLocomotionController/RobotAIController/DamageReceiver. ApplyStats() public for direct calls. Closes the T043 wiring gap. |
| T046 — Tests: InitForMatch, DamageReceiver armor, CombatStatsApplicator | 2026-04-10 | +7 HealthSOTests (InitForMatch paths). DamageReceiverArmorTests (13 tests): SetArmorRating bounds, flat-reduction, full-block, DamageInfo path. CombatStatsApplicatorTests (11 tests): null-safety, health/armor/damage-mult/base-speed application, event subscribe/unsubscribe. Total tests: 292 across 24 files. |
| T047 — RobotAssembler.AssembleFromCatalog() + MatchFlowController loadout bridge | 2026-04-10 | AssembleFromCatalog(partIds, catalog): O(n) lookup build, resolve IDs to PartDefinitions, warn-and-skip unknowns, null-fallback to Assemble(). MatchFlowController: _playerLoadout + _shopCatalog + _playerAssembler optional fields; HandleMatchStarted() routes player assembler through AssembleFromCatalog when all three assigned. Closes the PlayerLoadout→RobotAssembler wiring gap. |
| T048 — LoadoutSlotController + LoadoutBuilderController | 2026-04-10 | LoadoutSlotController (BattleRobots.UI): category slot row with None sentinel, prev/next cycle, GetSelectedPartDef(), RebuildCandidates() on inventory change. LoadoutBuilderController (BattleRobots.UI): one row per unique PartCategory from RobotDefinition.Slots; owned-parts filter via PlayerInventory.HasPart; pre-selects from PlayerLoadout; ConfirmLoadout() persists via load→mutate→save; live stats preview via RobotStatsAggregator. No Physics refs. |
| T049 — RobotAssemblerLoadoutTests | 2026-04-10 | 17 EditMode tests for AssembleFromCatalog: null safety, ID resolution, unknown-ID skip, whitespace skip, slot-count limit, re-assembly, fallback to inspector list. Total tests: 309 across 25 files. |

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
| 2026-04-10 | PM Agent | Session 12: T029 ShopItemController + ShopCatalogView — bridges data layer to shop UI. ShopItemController (UI): Setup injects PartDefinition+ShopManager, Refresh updates owned badge/cost/button; ShopCatalogView (UI): subscribes to inventory+wallet SO channels, PopulateCatalog spawns one row per catalog entry, RefreshAll propagates on change; zero alloc after Awake. T030 StarterInventoryConfig SO — new player UX: immutable SO lists starter partIds; GameBootstrapper applies starters when inventory empty after load, persists immediately; backwards-compatible. 8 StarterInventoryConfigTests added. Total tests: 76. Total tasks Done: T001–T030. |
| 2026-04-10 | PM Agent | Session 13: T031 Bug fix — GameBootstrapper first-launch wallet: `walletBalance > 0` guard was broken (Balance is 0 before Reset()), new players got 0 credits. Fix uses `isFirstLaunch` flag (all three SaveData fields == 0/empty); branches to Reset() vs LoadSnapshot(). T032 Test expansion — MatchResultSOTests (10), IntGameEventTests (13), ShopCatalogTests (9) added; 3 new test files. Total tests: 108 across 11 files. All 32 backlog items Done. |
| 2026-04-10 | PM Agent | Session 14: T033 Critical compile-error fix — PostMatchController + PauseMenuController called SceneLoader.LoadSceneAsync() which does not exist; corrected to LoadScene(). T034 SceneRegistry SO (BattleRobots.Core): eliminates scene-name magic strings duplicated across 3 UI controllers; OnValidate warns on empty names; all 3 controllers updated to inject _sceneRegistry with null-safe fallback. 7 SceneRegistryTests added. Total tests: 115 across 12 files. Total tasks Done: T001–T034. |
| 2026-04-10 | PM Agent | Session 15: T035 FloatGameEvent tests (13 tests) — closes coverage gap for GameEvent<float>, mirrors IntGameEventTests pattern. T036 BotDifficultyConfig tests (8 tests) — last SO without dedicated test coverage; verifies all 6 property constraints and relational AttackRange≤DetectionRange invariant. T037 DamageGameEvent tests (13 tests) — tests GameEvent<DamageInfo> struct payload delivery across all three fields plus safe iteration. Total tests: 149 across 15 files. Total tasks Done: T001–T037. |
| 2026-04-10 | PM Agent | Session 16: T038 RobotDefinition tests (13 tests) — ValidateSlots() failure/passing paths via reflection; fresh-instance property defaults. T039 AudioEvent tests (14 tests) — RegisterCallback/Raise/PickClip/duplicate-guard/safe-iteration mirroring other event channel test files. T040 MatchHistoryController + MatchHistoryRowController — new BattleRobots.UI feature bridging SaveData.matchHistory to a scrollable match-record list; auto-refreshes on VoidGameEvent (MatchEnded); zero alloc after Awake. T041 ArenaConfig + SpawnPointData tests (10 tests) — dimension constraints, SpawnPoints list, ArenaIndex, SpawnPointData defaults. Total tests: 186 across 18 files. Total tasks Done: T001–T041. |
| 2026-04-10 | PM Agent | Session 17: T042 GameSettingsSO + SettingsController — closes the settings persistence gap. GameSettingsSO (Core): master/sfx/music volumes [0,1]; EffectiveSfxVolume/EffectiveMusicVolume computed props; SetX mutators raise VoidGameEvent; LoadSnapshot silent (bootstrapper-safe); TakeSnapshot; Reset. SettingsSnapshot [Serializable] class added to MatchRecord.cs; SaveData.settingsSnapshot field with default 1.0 (backwards-compat). GameBootstrapper: _gameSettings field; LoadSnapshot called after inventory rehydration. AudioManager: optional _settings; PlayClip scales volume by EffectiveSfxVolume. SettingsController (BattleRobots.UI): cached UnityAction delegates; OnEnable sync sliders (SetValueWithoutNotify); PersistSettings (Load→mutate→Save) on OnDisable. 26 GameSettingsSOTests. Total tests: 212 across 19 files. Total tasks Done: T001–T042. |
| 2026-04-10 | PM Agent | Session 18: T043 PartStats+RobotCombatStats+RobotStatsAggregator — closes the gameplay-stats gap where PartDefinitions had no stat contribution. PartStats [Serializable] struct added to PartDefinition.cs (healthBonus, speedMultiplier, damageMultiplier, armorRating; PartStats.Default). RobotCombatStats readonly struct (IEquatable, ==). RobotStatsAggregator static class: null-safe Compute() with additive health, multiplicative speed+damage, clamped armor. 10 PartStatsTests + 20 RobotStatsAggregatorTests. T044 PlayerLoadout SO — closes the save-your-build gap. PlayerLoadout SO (Core): SetLoadout/LoadSnapshot/Reset; VoidGameEvent _onLoadoutChanged; bootstrapper-safe. SaveData.loadoutPartIds (backwards-compat). GameBootstrapper wired. 19 PlayerLoadoutTests. Total tests: 261 across 22 files. Total tasks Done: T001–T044. |
| 2026-04-10 | PM Agent | Session 19: T045 CombatStatsApplicator MB — closes the wiring gap where RobotStatsAggregator.Compute() existed but was never called at runtime. Patched 5 existing files to add the necessary public APIs: HealthSO.InitForMatch() + _runtimeMaxHealth + MaxHealth property (Reset/Heal use property); DamageReceiver._armorRating + SetArmorRating(int) + ArmorRating prop + flat reduction in TakeDamage(); RobotAIController._damageMultiplier + SetDamageMultiplier(float) + DamageMultiplier prop + applied in FireAttack(); RobotLocomotionController._runtimeMoveSpeed + SetBaseSpeed(float) + BaseSpeed prop + EffectiveMoveSpeed in ApplyLocomotion; RobotAssembler._assembledPartDefs + GetEquippedParts() IReadOnlyList<PartDefinition>. New CombatStatsApplicator MB (DefaultExecutionOrder 10): caches delegate; subscribes/unsubscribes VoidGameEvent; ApplyStats() calls Compute() then InitForMatch+Reset/SetBaseSpeed/SetDamageMultiplier/SetArmorRating. T046 Tests: +7 HealthSOTests (InitForMatch), 13 DamageReceiverArmorTests, 11 CombatStatsApplicatorTests. Total tests: 292 across 24 files. Total tasks Done: T001–T046. |
| 2026-04-10 | PM Agent | Session 20: T047 RobotAssembler.AssembleFromCatalog() — closes the PlayerLoadout→RobotAssembler wiring gap (Session Handoff said "pass EquippedPartIds to Assemble()" but no API existed). New method builds partId→PartDef lookup, resolves IDs, warns-and-skips unknowns, null-fallback to Assemble(). MatchFlowController patched with three optional fields (_playerLoadout, _shopCatalog, _playerAssembler); HandleMatchStarted routes player assembler through AssembleFromCatalog when all three assigned. T048 LoadoutSlotController + LoadoutBuilderController — complete pre-match assembly UI layer: slot rows with None/prev/next cycling; parent controller groups by PartCategory, filters by PlayerInventory.HasPart, pre-selects from PlayerLoadout, ConfirmLoadout() persists, live stats preview via RobotStatsAggregator. No Physics refs (BattleRobots.UI namespace). T049 RobotAssemblerLoadoutTests (17 tests): null safety, resolution, unknown-ID skip, whitespace, slot-count limit, re-assembly, fallback-to-inspector-list. Total tasks Done: T001–T049. Total tests: 309 across 25 files. |

---

## Session Handoff

**Last completed:** T049 (RobotAssemblerLoadoutTests). **309 total tests across 25 files.** All 49 backlog items **Done**.

**C# layer status:** Complete and compiles clean. All event channel types tested. Every ScriptableObject in BattleRobots.Core has at least one test file. Newest additions (Session 20): AssembleFromCatalog on RobotAssembler (bridges PlayerLoadout string IDs to PartDefinition objects), MatchFlowController loadout-override fields, LoadoutSlotController + LoadoutBuilderController (complete pre-match assembly UI — wire in Editor).

**Remaining work (Editor-session only — cannot be done by a remote agent):**

### Recommended first step: run SceneWiringValidator
Open the Arena scene → Tools ▶ BattleRobots ▶ Scene Wiring Validator → Scan Scene.
The tool will list every null SO reference across all BattleRobots components and let you click-to-select each one. Use the list below as your authoritative wiring guide.

### Running the test suite
Open the project in Unity → Window ▶ General ▶ Test Runner → EditMode tab → Run All.
All 309 tests should pass without scene setup (they use `ScriptableObject.CreateInstance` and `Application.persistentDataPath`).

### LoadoutBuilder wiring (new — T048) ← next priority after CombatStatsApplicator
This is the pre-match assembly screen. Create a new scene or panel in the Main Menu / pre-arena flow:
1. Create a "LoadoutSlot" prefab:
   - Attach `LoadoutSlotController` MB.
   - Assign optional UI refs: `_categoryLabel` (Text), `_partNameLabel` (Text),
     `_partDescLabel` (Text), `_thumbnailImage` (Image), `_prevButton` (Button), `_nextButton` (Button).
   - Wire `_prevButton.onClick → LoadoutSlotController.PreviousPart` (or leave for Awake auto-wire).
   - Wire `_nextButton.onClick → LoadoutSlotController.NextPart`.
2. On the loadout panel root, add `LoadoutBuilderController` MB and assign:
   - `_playerInventory` → same PlayerInventory SO as GameBootstrapper.
   - `_playerLoadout` → same PlayerLoadout SO as GameBootstrapper + MatchFlowController.
   - `_shopCatalog` → same ShopCatalog SO as ShopManager.
   - `_robotDefinition` → the player robot's RobotDefinition SO.
   - `_onInventoryChanged` → same VoidGameEvent SO as PlayerInventory._onInventoryChanged.
   - `_slotRowPrefab` → the LoadoutSlot prefab from step 1.
   - `_slotContainer` → a ScrollRect Content Transform (VerticalLayoutGroup recommended).
   - `_confirmButton` → a "Confirm Build" Button (or wire onClick → ConfirmLoadout() in Inspector).
   - Optionally: `_healthText`, `_speedText`, `_damageText`, `_armorText` for live stats preview.
3. `ConfirmLoadout()` writes to `PlayerLoadout` **and** persists to disk automatically.
   Call it when the player presses "Enter Arena" or "Confirm Build".

### MatchFlowController loadout bridge wiring (new — T047)
In the Arena scene, open MatchFlowController and set the three optional loadout fields:
- `_playerLoadout` → same PlayerLoadout SO used by LoadoutBuilderController.
- `_shopCatalog` → same ShopCatalog SO.
- `_playerAssembler` → the RobotAssembler on the **player** robot root.
  (The player assembler may also appear in `_assemblers` — that is fine, the override check
   routes it through `AssembleFromCatalog` instead of `Assemble`.)
When all three are assigned, the player robot rebuilds its equipped parts from the saved
loadout on every `MatchStarted` event. Enemy assemblers always use `Assemble()`.

### CombatStatsApplicator wiring (new — T045) ← highest priority
Add `CombatStatsApplicator` MB to each robot root GameObject in the Arena scene.
Wire the following fields:
- `_matchStartedEvent` → same MatchStarted VoidGameEvent SO as MatchManager uses.
- `_robotDefinition` → the chassis RobotDefinition SO for this robot.
- `_assembler` → the RobotAssembler on the same GameObject.
- `_health` → the robot's HealthSO asset (must be unique per robot).
- `_locomotion` (optional) → the RobotLocomotionController on the same root.
- `_aiController` (optional, enemy robots only) → the RobotAIController.
- `_damageReceiver` (optional) → the DamageReceiver on the same root.

**Execution order:** `CombatStatsApplicator` has `DefaultExecutionOrder(10)`.
`MatchFlowController` (order 0) calls `Assemble()` first via its MatchStarted callback,
so `GetEquippedParts()` returns the live part list when `ApplyStats()` fires.

**PartDefinition Inspector:** Open each PartDefinition SO and set the "Combat Stats" fields
(`healthBonus`, `speedMultiplier`, `damageMultiplier`, `armorRating`) per part.

### PlayerLoadout wiring (new — T044)
- Create SO asset: Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ PlayerLoadout (one global instance).
- Create a VoidGameEvent SO ("LoadoutChanged") and assign to `PlayerLoadout._onLoadoutChanged`.
- Assign the same PlayerLoadout SO to `GameBootstrapper._playerLoadout`.
- Call `PlayerLoadout.SetLoadout(assembler.GetEquippedPartIds())` after the player confirms their build in the pre-match assembly UI to persist the loadout for the next session.
- On match start, pass `PlayerLoadout.EquippedPartIds` to `RobotAssembler.Assemble()` to restore the saved build automatically.

### GameSettings wiring (new — T042)
- Create SO asset: Assets ▶ Create ▶ BattleRobots ▶ Core ▶ GameSettings (one global instance).
- Create a VoidGameEvent SO ("SettingsChanged") and assign to `GameSettingsSO._onSettingsChanged`.
- Assign the **same** GameSettingsSO asset to:
  - `GameBootstrapper._gameSettings`
  - `AudioManager._settings`
  - `SettingsController._settings` (on any settings panel GO)
- On each settings panel, add `SettingsController` MB and assign the three optional Sliders
  (min 0, max 1). Settings persist automatically when the panel is hidden.
- `AudioManager` will scale all SFX volume by `EffectiveSfxVolume` (master × sfx) automatically.

### SceneRegistry wiring (new — T034)
- Create SO asset: Assets ▶ Create ▶ BattleRobots ▶ Core ▶ SceneRegistry (one global instance).
- Set `_mainMenuSceneName`, `_arenaSceneName`, `_shopSceneName` to match exact Build Settings scene names.
- Assign the **same** SceneRegistry SO to:
  - `MainMenuController._sceneRegistry`
  - `PostMatchController._sceneRegistry`
  - `PauseMenuController._sceneRegistry`
- If left null, each controller falls back to hard-coded defaults ("MainMenu", "Arena", "Shop") — safe for initial testing.

### StarterInventoryConfig wiring (new — T030)
- Create SO asset: Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ StarterInventoryConfig.
- Populate `_starterPartIds` with the IDs of parts new players should receive (must match PartDefinition.PartId exactly).
- Assign the SO to `GameBootstrapper._starterConfig`.
- On first launch (empty inventory), the bootstrapper unlocks and persists all starters automatically.
- Leave `_starterConfig` null to skip starter distribution (backwards-compatible).

### ShopCatalogView wiring (new — T029)
- Create a shop-row prefab with `ShopItemController` MB attached. Assign UI refs in the prefab Inspector:
  `_nameLabel` (Text), `_costLabel` (Text), `_descriptionLabel` (Text, optional),
  `_thumbnail` (Image, optional), `_buyButton` (Button), `_ownedBadge` (GameObject, optional).
- Wire `_buyButton.onClick → ShopItemController.OnBuyClicked` in the prefab Inspector.
- On the shop panel GameObject, add `ShopCatalogView` MB and assign:
  - `_shopManager` → the ShopManager in the scene
  - `_itemPrefab` → the shop-row prefab above
  - `_itemContainer` → the ScrollRect Content Transform (VerticalLayoutGroup recommended)
  - `_onInventoryChanged` → same VoidGameEvent SO as PlayerInventory._onInventoryChanged
  - `_onBalanceChanged` → same IntGameEvent SO as PlayerWallet._onBalanceChanged
- ShopCatalogView will instantiate one row per PartDefinition in the ShopCatalog and keep all rows in sync automatically.

### PlayerInventory wiring (new — T028)
- Create SO asset: Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ PlayerInventory (one global instance).
- Assign the **same** SO asset to `GameBootstrapper._playerInventory` AND `ShopManager._inventory`.
- Create a VoidGameEvent SO ("InventoryChanged") and assign to `PlayerInventory._onInventoryChanged`.
- Wire the same "InventoryChanged" SO to `ShopCatalogView._onInventoryChanged`.

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

### MatchHistoryController wiring (new — T040)
- Create a "HistoryRow" prefab. Attach `MatchHistoryRowController` MB.
  In the prefab Inspector assign optional Text refs:
  `_outcomeText` ("WIN"/"LOSS"), `_durationText` (MM:SS), `_rewardText` (+200),
  `_dateText` (e.g. "Apr 10, 2026").
- On the panel that should show match history (Main Menu or Post-Match screen):
  - Add `MatchHistoryController` MB.
  - Assign `_rowPrefab` → the HistoryRow prefab above.
  - Assign `_listContainer` → a ScrollRect Content Transform (VerticalLayoutGroup recommended).
  - Set `_maxDisplayCount` (default 10).
  - Optionally assign `_onMatchEnded` → same VoidGameEvent SO as MatchManager._onMatchEnded
    so the list auto-refreshes after each match without a scene reload.
- `PopulateHistory()` is also safe to call from a Button.onClick event.

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

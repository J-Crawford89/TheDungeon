# TheDungeon — Product Backlog

This is the living intake and prioritization document for requested features, observed bugs, UX problems, and unresolved design decisions. It records **what should change and why**; the [Game Design Document](./GAME_DESIGN_DOCUMENT.md) remains canonical for accepted player-facing rules, and the [Technical Design Document](./TECHNICAL_DESIGN_DOCUMENT.md) remains canonical for implementation decisions.

Items begin as `Proposed`. Promote them to the GDD/TDD when their design or implementation approach is accepted. Move completed work to a dated **Completed** section with its commit or pull request.

## Priority key

- **P0 — Blocking:** Prevents reliable play or continued development.
- **P1 — High:** Major pacing, comprehension, or core-loop problem.
- **P2 — Medium:** Meaningful improvement that does not block the core loop.
- **P3 — Future:** Important direction that still needs substantial design.

## Work-type key

- **Bug:** Existing behavior is incorrect relative to accepted intent.
- **Issue:** A known UX, pacing, or design problem whose final solution may still require a decision.
- **Improvement:** Working behavior that should be made measurably better without regressing the baseline.
- **Feature:** New player-facing or supporting capability.

Each type has its own independent number sequence. Identifiers are stable once assigned, even if priority or implementation order changes. Keep open items under their type heading in identifier order. Completed items belong only in **Completed**.

## Bugs

### Bug-004 — Define physical result semantics for tetrahedral d4 dice

- **Type:** Bug / physics-model investigation
- **Priority:** P2
- **Status:** Needs manual confirmation
- **Problem:** The shared predetermined-roll acceptance rule expects a labeled face normal to finish nearly parallel to world up. A tetrahedral d4 normally rests on one triangular face while its readable result is associated with the opposite vertex; it has no horizontal top face equivalent to a d6. A deterministic all-shape timing experiment could not produce a d4 trajectory satisfying the common `dot(up) >= 0.95` rule, even though other serialized shapes did.
- **Desired behavior:** Define the d4 result from its physically resting support face/opposite result vertex, preserve readable label orientation, and validate it without weakening the flat-face rule for other dice.
- **Acceptance notes:** First reproduce and visually confirm the issue in the dice test scene. Then add d4-specific physical-settle/result tests before changing simulation or calibration behavior. Do not fold this investigation into presentation-speed tuning.

## Issues

None open.

## Improvements

### Improvement-002 — Add dice-rolling sound effects

- **Type:** Improvement / audio feedback
- **Priority:** P2
- **Status:** Proposed
- **Depends on:** Improvement-001
- **Problem:** The physical dice presentation is visually readable but silent, reducing its weight and tactile believability.
- **Desired behavior:** Add synchronized audio for the throw, rolling/tumbling, wall or die impacts where appropriate, and the final settle. Sound should reflect the visible presentation without becoming noisy during rapid sequences or true multi-die rolls.
- **Scope notes:** Define whether sounds are driven by physics events, presentation phases, or a hybrid; support per-die variation in pitch, volume, and sample selection; prevent repetitive sample playback and excessive overlapping impacts; respect sound-effect volume and mute settings.
- **Acceptance notes:** Standard and rapid-sequence rolls sound responsive and materially grounded. Audio remains synchronized under time-compressed playback, multi-die rolls remain intelligible, and headless tests do not require audio resources.

### Improvement-003 — Coordinator owns HUD refresh (drop `UiRefreshFlags` plumbing)

- **Type:** Improvement / UI architecture
- **Priority:** P2
- **Status:** Proposed
- **Depends on:** None
- **Distinct from:** Feature-011 (AV juice / semantic presentation events). This item is about **who refreshes HUD panels**, not about hit flashes or screen shake.
- **Desired behavior:** `GameUiCoordinator` (or a dedicated HUD subscriber) owns refresh. Presenters and services stop passing `UiRefreshFlags` through call chains; they raise or rely on **state events**, and the coordinator subscribes.
- **Acceptance notes:** Combat, exploration, loot, and inventory still update the same panels; no presenter needs a `refreshHud` callback solely to push flags.

### Improvement-004 — Polish initiative overlay and strip presentation assets

- **Type:** Improvement / combat UI polish
- **Priority:** P2
- **Status:** Proposed
- **Depends on:** Feature-001, Issue-002
- **Distinct from:** Feature-011 (semantic AV juice for hits, damage, traps, victory). This item is about the **initiative overlay and strip** already in the main view: final art, motion, audio, and layout, not new combat-outcome events.
- **Problem:** Functional initiative chrome is in place (order overlay, persistent strip, current-combatant styling, fade in/out). Placeholder panels, typography, timing, and silence still read as prototype.
- **Desired behavior:** Replace or tighten graphics, animation, audio, and related UI assets so the order reveal and strip feel authored rather than temporary, without changing the Game presentation beats (`OrderRevealed`, `ActiveTurnChanged`, `CombatEnded`).
- **Acceptance notes:** Order and active combatant remain readable at a glance. Polish does not stall combat if an asset or animation is missing. Headless tests stay independent of Godot assets.

## Features

### Feature-002 — Default player and monster die colors

- **Type:** Feature / visual identity
- **Priority:** P2
- **Status:** Proposed
- **Depends on:** None
- **Desired behavior:** Player and monster dice use clearly distinct default colors. Preserve an extension point for future selectable or unlockable dice appearances.
- **Acceptance notes:** Ownership is immediately legible during initiative and combat rolls, including multi-die events; color choices remain distinguishable under supported accessibility settings.

### Feature-003 — UI scaling

- **Type:** Feature / accessibility
- **Priority:** P2
- **Status:** Proposed
- **Scope:** Define supported resolutions, scaling behavior, minimum readable sizes, and whether scaling is global or independently configurable by UI region.

### Feature-004 — Split SP and HP

- **Type:** Design decision / character resources
- **Priority:** P3
- **Status:** Proposed
- **Scope:** Define what SP represents, how damage and recovery interact with SP versus HP, and how the split changes combat pacing, death, resting, items, and UI.

### Feature-005 — Focus or strain resource

- **Type:** Feature / exploration economy
- **Priority:** P3
- **Status:** Proposed
- **Problem:** Repeated inspection, trap-disarm attempts, lock attempts, and similar actions currently have little opportunity cost, encouraging exhaustive button spam.
- **Design intent:** Introduce a rest-linked resource—depleting Focus or accumulating Strain—that turns repeated investigation and precision actions into a resource decision rather than an annoyance.
- **Open questions:** Choose Focus versus Strain framing; affected actions; costs; failure behavior; recovery/rest rules; maximum/minimum values; class/race/item interactions; and safeguards against soft-locking progress.

### Feature-006 — Multiple monsters in one room

- **Type:** Feature / combat architecture
- **Priority:** P3
- **Status:** Proposed
- **Depends on:** Issue-002, Feature-001
- **Scope:** Encounter composition, initiative order, targeting, monster turn sequencing, main-view layout, death/removal, rewards, and multi-monster tests.

### Feature-007 — Multiple attack options per weapon

- **Type:** Feature / combat content
- **Priority:** P3
- **Status:** Proposed
- **Scope:** Allow a weapon to expose multiple named attacks with distinct accuracy, damage, damage type/family, costs, and other effects. Define selection UI and AI usage.

### Feature-008 — Attacks granted by class, race, or other sources

- **Type:** Feature / character progression
- **Priority:** P3
- **Status:** Proposed
- **Depends on:** Feature-007
- **Scope:** Support attacks not owned exclusively by a weapon and define how multiple grant sources combine or conflict.
- **Presentation requirement:** Narrative and UI output must name the attack as well as the weapon used, if any.

### Feature-009 — Spellcasting

- **Type:** Feature / major system
- **Priority:** P3
- **Status:** Proposed
- **Scope:** Define spell acquisition, preparation/selection, resources, targeting, resolution rolls, effects, scaling, interruption, combat/exploration use, enemy casting, UI, content authoring, and save-state requirements before implementation.

### Feature-010 — Contextual music system

- **Type:** Feature / audio presentation
- **Priority:** P3
- **Status:** Proposed
- **Depends on:** None
- **Desired behavior:** Provide music appropriate to major game modes, initially including the main menu, dungeon exploration, and combat, with room for victory, defeat, boss, safe-area, and special-event cues.
- **Scope notes:** Define track ownership and licensing, transition and crossfade rules, looping, interruption/resume behavior, scene and game-state triggers, volume controls, saveable settings, and how rapidly changing states avoid restarting or stacking tracks.
- **Acceptance notes:** Music follows authoritative game state, transitions cleanly without audible overlap or abrupt unintended restarts, and can be independently adjusted or muted.

### Feature-011 — Event-driven audiovisual UI reactions

- **Type:** Feature / game-feel and UI feedback
- **Priority:** P2
- **Status:** Proposed; requires detailed scoping
- **Depends on:** Feature-001
- **Design intent:** Make important actions and consequences immediately legible and satisfying through coordinated UI motion, graphical effects, sound cues, and brief emphasis beats. The system must work with placeholder assets so interaction design can be developed before final artwork exists.
- **Initial examples:** Flash a monster portrait red and play a slash effect when it is hit; shake or flash the main UI when the player takes damage; distinguish trap triggering from trap disarming; acknowledge unlocking a chest; emphasize attacks, misses, critical outcomes, healing, death, and battle victory.
- **Architecture direction:** Gameplay publishes semantic presentation events such as `MonsterHit`, `PlayerDamaged`, `TrapDisarmed`, or `BattleWon`. Presentation code decides which animation, sound, overlay, and timing to use. Rules and state mutation must not depend on a particular animation or asset.
- **Scope required:** Establish an event catalog and priority tiers; feedback ownership by UI region; animation interruption and queuing rules; synchronization with dice and narration; placeholder and final asset requirements; audio routing; reduced-motion, flash-intensity, screen-shake, and volume settings; and behavior when effects are disabled.
- **Acceptance notes:** Core outcomes are understandable without relying only on the narrative log, simultaneous feedback remains readable, skipped or disabled effects cannot block gameplay, and presentation consistently reflects already-authoritative state.

### Feature-012 — Drive coded combat abilities through `CombatAbilityEffectsRegistry`

- **Type:** Feature / combat
- **Priority:** P2
- **Status:** Proposed
- **Depends on:** None
- **Notes:** The registry already exists; this cleanup pass only queries Defend `CanExecute` for the command panel. Remaining coded abilities should use the same lookup for visibility, enablement, and execution instead of duplicating cooldown/stance checks in UI.
- **Acceptance notes:** Adding a registered ability does not require a new `CommandPanel` cooldown branch.

### Feature-013 — Gate healing on `RestoreHealthEffectDefinition`

- **Type:** Feature / items
- **Priority:** P3
- **Status:** Proposed
- **Depends on:** None
- **Desired behavior:** Using a potion (and related heal rules) keys off [`RestoreHealthEffectDefinition`](../Scripts/0.Core/Item/ItemEffectDefinition.cs) on the item, not the `health_potion` definition id. Notebook **Use** already enables for any `PotionDefinition`; heal application should follow the effect type.
- **Acceptance notes:** A non-health potion id with a restore-health effect heals; a health-potion id without that effect does not.

### Feature-014 — Async orchestration sweep

- **Type:** Feature / architecture
- **Priority:** P2
- **Status:** Proposed
- **Depends on:** None
- **Desired behavior:** Delete production `Foo() => FooAsync().GetResult()` wrappers. Dice and UI waits are async-only. Share a safe `async void` logging pattern for Godot button handlers. Keep inventory/math APIs synchronous.
- **Acceptance notes:** No gameplay path blocks a thread on `.GetResult()`; unit tests still cover the async methods.

### Feature-015 — `NarrativeConstants` wording store

- **Type:** Feature / narrative
- **Priority:** P3
- **Status:** Proposed
- **Depends on:** None
- **Desired behavior:** Player-facing strings live in `NarrativeConstants` behind [`NarrativeService`](../Scripts/3.Game/Narrative/NarrativeService.cs). Callers still only use `_narrative.For…` and never reference constants.
- **Acceptance notes:** Combat/UI log lines are unchanged for players; constants are the single wording store.

### Feature-016 — Real backpack capacity

- **Type:** Feature / inventory
- **Priority:** P2
- **Status:** Proposed
- **Depends on:** None
- **Desired behavior:** Enforce a real backpack capacity (not only 16 visible notebook rows). `AddOrStack` reports leftover quantity; loot and harvest handle a full pack without silently dropping items.
- **Acceptance notes:** Overflow is visible in UI and logs; taking loot when full does not destroy remaining stacks.

### Feature-017 — Character-creation preview lists real grants

- **Type:** Feature / character creation
- **Priority:** P3
- **Status:** Proposed
- **Depends on:** None
- **Desired behavior:** The creation preview lists actual ability grants, starting equipment, and abilities the player will receive on apply—not placeholder or incomplete summaries.
- **Acceptance notes:** Preview matches `CharacterCreationService.ApplyToPlayer` for the selected race/class/background.

## Cross-cutting sequencing principle

Unless a roll is explicitly one multi-die mechanic, presentation and state progression should follow this cycle:

1. Commit and display the action that caused the check.
2. Present exactly that check's die or dice.
3. Briefly hold the readable result.
4. Remove its dice.
5. Apply and display narration, state consequences, and main-view updates.
6. Start the next queued action or check.

This principle should guide the eventual orchestration design for room entry, initiative, combat, harvesting, inspection, traps, containers, and other checks.

## Completed

### Bug-001 — Resolve room entry before initiative presentation

- **Type:** Bug / sequencing
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-14
- **Depends on:** Bug-002
- **Problem:** Entering a room containing a monster could begin the initiative roll before the move was visibly resolved. The main view and narrative log lagged behind the player's action.
- **Outcome:** The move is committed, logged, and reflected in the main view before `TryBeginCombatIfHostileAsync`. The player sees the destination room and its contents before initiative dice.
- **Implementation evidence (2026-08-14):** `ExplorationUiPresenter` refreshes MainView/Log/Command/Map/Character after the move log and before combat begin on forward and floor-down. A sequencing test records a MainView refresh before combat begin.
- **Acceptance evidence:** The user requested the item be closed after the initiative overlay/strip work landed on this sequencing.

### Issue-002 — Make initiative order explicit before combat actions

- **Type:** UX / combat clarity
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-14
- **Depends on:** Bug-002, Feature-001
- **Problem:** If monsters won initiative, an enemy attack could begin immediately after the initiative dice, before the player understood that order was set or who acted first.
- **Outcome:** After turn order is committed, Game awaits `OrderRevealed`. The UI shows the rolled order on an overlay (readable beat, then fade out) and then the persistent initiative strip before any combatant acts, including when a monster acts first. Combat end awaits `CombatEnded` so the strip can fade out.
- **Implementation evidence (2026-08-14):** `ICombatTurnPresentationSink` is kind-only; UI maps `session.Combat` to names and current index. Deferred-sink tests prove Game waits on `OrderRevealed`, `ActiveTurnChanged`, and `CombatEnded`.
- **Acceptance evidence:** The user requested the item be closed; remaining art/audio polish is Improvement-004.

### Feature-001 — Main-view turn and action indicators

- **Type:** Feature / combat UX
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-14
- **Depends on:** Bug-002
- **Problem:** The narrative log recorded turns and actions, but the main view did not make the active combatant or combat order sufficiently clear.
- **Outcome:** Functional main-view chrome is in: initiative overlay for the rolled order, a strip of combatants with current-turn styling, restyle on `ActiveTurnChanged`, and fade-out on `CombatEnded`. Action-specific juice (hit flashes, attack FX) remains Feature-011; asset polish is Improvement-004.
- **Implementation evidence (2026-08-14):** `GameUiCoordinator.PresentCombatTurnAsync` drives overlay and `InitiativeStripView`. The strip is exported on `MainViewPanel` (instanced scene). Mapper tests cover player/monster names and empty combat.
- **Acceptance evidence:** The user confirmed the functional work is done and asked that the item be marked implemented.

### Bug-002 — React after each resolved roll, not after the entire queue

- **Type:** Bug / orchestration
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-14
- **Depends on:** None
- **Problem:** A queue of rolls could defer narration, main-view refreshes, and state consequences until later rolls finished.
- **Outcome:** Each resolved roll is now a presentation boundary. After its readable result pause, narration and applicable state/UI changes publish before the next independent roll begins. A true multi-die roll remains one resolution boundary.
- **Implementation evidence (2026-08-13):** A UI-agnostic `IResolvedRollReactionSink` is awaited after each initiative, hit, damage, flee, potion, trap, inspect, and harvest result commits its narration and applicable state. Deferred regression tests prove a hit reaction completes before damage presentation and each initiative reaction completes before the next combatant's roll.
- **Acceptance evidence:** The user visually verified the completed sequencing in the running game and requested that the item be closed.
- **Verification:** 384 xUnit tests passed and the Godot C# solution built with 0 errors.

### Issue-001 — Remove a resolved die before the next queued roll

- **Type:** UX / presentation sequencing
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-14
- **Depends on:** Bug-002
- **Problem:** Sequential checks could leave dice visible or overlap rolls, making independent checks look like one multi-die event.
- **Outcome:** Independent rolls now follow: roll → briefly display result → remove die → react in the game/UI → begin the next roll. Only dice belonging to one true multi-die roll share the screen.
- **Implementation evidence (2026-08-13):** `DiceRollOverlay` serializes independent presentation requests and owns each logical roll through the profile-scaled result pause, synchronous detach, and queued deletion. The unawaited `DieLingerSeconds` path was removed. True multi-die batches still share one presentation.
- **Acceptance evidence:** The user visually verified that resolved dice are removed correctly before later independent rolls and requested that the item be closed.

### Issue-003 — Improve repeated harvest-roll pacing

- **Type:** Design decision / pacing
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-14
- **Depends on:** Bug-002, Issue-001, Improvement-001
- **Problem:** Rolling separately for many harvestable parts could make a small corpse take roughly ten seconds or more to resolve.
- **Chosen design:** Retain one authoritative roll per individual item. A loot action with multiple valid harvest attempts uses the timing-only `RapidSequence` profile (`0.75x` playback cap, `0.5x` result pause); exactly one attempt uses `Standard`. Complete trajectories and exact final faces remain guaranteed.
- **Result cadence:** Each die is removed, its indexed result is logged, and any successful item is added to inventory before the UI reaction and next roll. The final attempt also commits a concise stack summary.
- **Fallback design:** If later play-testing finds rapid sequential presentation tedious, reconsider one roll per item stack with tiered difficulty thresholds.
- **Implementation evidence (2026-08-13):** Deterministic tests prove all repeated attempts select `RapidSequence`, a lone attempt selects `Standard`, and log/inventory snapshots advance after each attempt before the next presentation.
- **Acceptance evidence:** The user visually verified the repeated-harvest pacing and per-roll feedback and requested that the item be closed.

### Improvement-001 — Reduce average die-roll presentation below one second

- **Type:** Improvement / investigation
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-13
- **Acceptance evidence:** The user visually verified the implementation and reported that it works well enough to establish the new accepted baseline.
- **Outcome:** The complete 60 Hz Bepu trajectory remains authoritative, while visible predetermined playback uses a shared monotonic elapsed-time clock, linear position interpolation, normalized quaternion interpolation, and an explicit exact final pose. True multi-die rolls share one timeline. The default readability hold is `0.20s`, and non-positive playback caps still provide a recorded-speed rollback.
- **Accepted tuning:** The C# fallback cap remains `0.75s`; the user-owned `RollingDie.tscn` currently overrides **Maximum Predetermined Playback Seconds** to `1.0s`, which is the value used for visual acceptance.
- **Verification:** 370 xUnit tests passed, the Godot C# project built with 0 errors, and no Godot-serialized asset was edited by an agent.
- **Commit:** `e9b038b` (`Faster roll resolving, creation of backlog.md and tracking of bugs/issues/features`).

### Bug-003 — Make simultaneous dice collisions believable

- **Type:** Bug / investigation
- **Priority:** P2
- **Status:** Completed
- **Completed:** 2026-08-13
- **Acceptance evidence:** The user visually confirmed that simultaneously rolling dice collide and continue to roll correctly. Finished/stale dice remain non-colliding by design during frozen replay; Issue-001 removes those dice before any later independent roll.
- **Depends on:** None
- **Problem:** Visible dice in a true multi-die roll can pass through one another, breaking immersion.
- **Implementation finding:** The offscreen Bepu simulation was already producing real dynamic-die contact constraints and measurable deflection. The previous collision test was invalid because its dice never contacted, while its first diagnostic instrumentation pass also initialized after the timestep and erased the contact it intended to report. Improvement-001's shared playback clock, exact collider symmetries, and uniform trajectory sampling preserve the corrected physical response; the evidence did not justify another physics change, contact-aware time warp, or collision-avoidance fallback.
- **Desired behavior:** Dice in a true multi-die roll should visibly bounce or, if guaranteed-result collision playback cannot be made coherent, their generated trajectories should avoid visible intersections.
- **Acceptance notes:** No obvious pass-through occurs; requested faces remain guaranteed; dice do not snap or diverge from the floor/walls; sequential rolls remain single-die per Issue-001.

#### Implementation plan

1. **Reproduce against the accepted baseline.** In `dice_test_scene.tscn`, exercise the existing spawn-count control with two, three, six, and twelve same-type gameplay dice, plus repeated d100 pairs. Record at least one confirmed pass-through and distinguish it from trajectories that merely cross in screen space at different depths. Preserve the accepted `1.0s` scene playback cap during diagnosis.
2. **Add a deterministic collision test path.** Extend only the C# test harness with a clearly labeled forced-collision batch action that launches a head-on pair and a glancing pair through the same production offscreen simulation and shared playback path. Do not require `.tscn` edits and do not replace the normal randomized batch controls.
3. **Capture simulation contact evidence.** Add a narrow diagnostic result around `PredeterminedDiceTrajectorySimulator` that records dynamic-die pair, simulation step, contact count/depth or separation, contact normal, and relative velocity immediately before/after the event. Keep the existing gameplay-facing `Simulate` API compatible unless a small result wrapper is clearly cleaner.
4. **Turn the existing collision test into a real invariant.** Deterministic head-on, glancing, d100, and mixed-shape tests must assert that the intended pair generated a Bepu contact and that the recorded motion deflected or separated rather than crossed. A test whose dice never contacted is not evidence that collisions work.
5. **Prove face substitution preserves collision geometry.** For every serialized die hull, verify that each natural-to-requested display offset maps the complete convex hull onto itself within the existing scale-relative tolerance. At recorded contact frames, verify that raw and displayed hull occupancy/support extents are equivalent. If a die fails this invariant, route that shape to collision avoidance rather than pretending its remapped replay reproduces the original contact.
6. **Verify compressed shared playback around contacts.** Sample deterministic collision trajectories through `PredeterminedTrajectoryPlaybackSampler` at 30, 60, and 144 FPS, using both the `0.75s` C# default and accepted `1.0s` scene cap. Confirm every die uses identical normalized progress, exact final poses remain intact, and the visible ordering cannot jump from pre-contact to apparent pass-through.
7. **Apply the smallest evidence-based fix.** If Bepu never creates the intended contact, correct its filtering, speculative margin, material, or solver setup and lock that response with tests. If Bepu is correct but compressed playback aliases the impact, add one shared batch-only contact-aware time mapping that allocates visible frames around recorded contacts while compressing uneventful motion; never alter each die independently. If a remapped visual cannot reproduce a valid collision, reject/regenerate that batch with tested trajectory clearance so the dice visibly avoid one another.
8. **Do not enable live Godot collision during frozen replay.** The visible bodies intentionally replay an authoritative Bepu trajectory while frozen and non-colliding. Re-enabling a second physics engine would allow it to diverge from the recorded path and requested faces; consider that only as a separately approved architecture replacement.
9. **Expose narrow diagnostics and run the visual gate.** Show batch contact count/pairs and whether contact-aware playback or avoidance was used in the test-scene status. Manually verify head-on and glancing pairs, d100, three-plus mixed dice, wall-adjacent contacts, and every configured render-rate test without wrong faces, snaps, stalls, or a pacing regression.

#### Implementation evidence (2026-08-13)

- `PredeterminedDiceTrajectorySimulator.SimulateWithDiagnostics` reports die indices, simulation step/time, contact count/depth, contact normal, and relative linear velocity immediately before and after each die-pair contact step. The existing `Simulate` trajectory API remains compatible.
- The former collision test now launches a separated head-on d6 pair, proves a Bepu contact occurred, and proves the pair's relative velocity materially changed.
- Deterministic glancing d6, head-on d10/d100-shape, and mixed d6+d10 scenarios prove contact, deflection, requested final faces, and equivalent raw/displayed hull occupancy at the contact frame.
- Every natural-to-requested face pair for every serialized die hull maps the complete hull onto itself within the existing scale-relative tolerance.
- Shared compressed playback at 30, 60, and 144 FPS with both `0.75s` and `1.0s` caps preserves ordering through the head-on contact and applies exact final poses. Uniform shared playback passed, so no additional contact-aware remapping or avoidance mode was added.
- The code-built dice harness now includes **Forced collision: head-on d6 pair** and **Forced collision: glancing d6 pair**. Gameplay batches and d100 pairs report Bepu contact points/steps/pairs, maximum approach/response, `playback=shared-uniform`, and `avoidance=none`.
- Automated verification: 379 xUnit tests pass and the Godot C# project builds with 0 errors. No `.tscn`, `.tres`, or other Godot-serialized file was edited.

#### Manual acceptance gate

Open the existing dice test scene without changing editor data. Run both forced-collision buttons repeatedly; each should visibly bounce and its status must report a nonzero Bepu contact. Then repeat normal gameplay batches at counts 2, 3, 6, and 12 plus several d100 pairs. A randomized batch may legitimately report zero contacts when its paths do not meet; that is not a failure. Keep Bug-003 open until no obvious pass-through, wrong face, snap, stall, or pacing regression is observed.

#### Delivery targets

- Every deterministic contact scenario proves either a recorded physical deflection or an explicitly generated collision-free path.
- No confirmed die-to-die pass-through is visible at 30, 60, or 144 FPS sampling schedules.
- Requested-face, exact-final-pose, natural-settle, grip, wall containment, and Improvement-001 timing guarantees remain green.
- Contact handling uses one shared batch timeline and stays within the configured playback cap.
- No `.tscn`, `.tres`, or other Godot-serialized file is edited by an agent; any unavoidable editor wiring is supplied as exact instructions.
- Focused tests, the full `dotnet test` suite, the Godot C# build, and a manual multi-die visual gate pass before completion.

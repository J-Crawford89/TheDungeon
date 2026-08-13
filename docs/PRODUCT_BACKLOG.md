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

Each type has its own independent number sequence. Identifiers are stable once assigned, even if priority or implementation order changes.

## Bugs

### Bug-001 — Resolve room entry before initiative presentation

- **Type:** Bug / sequencing
- **Priority:** P1
- **Status:** Proposed
- **Depends on:** Bug-002
- **Problem:** Entering a room containing a monster can begin the initiative roll before the move is visibly resolved. The main view and narrative log therefore lag behind the player's action.
- **Desired behavior:** Commit the move into the room, refresh the main view, and log room entry before initiative dice appear.
- **Acceptance notes:** The player must see that they entered the destination room and encountered its contents before combat initiative begins. Backend state, main view, and narrative order must agree.

### Bug-002 — React after each resolved roll, not after the entire queue

- **Type:** Bug / orchestration
- **Priority:** P1
- **Status:** Proposed
- **Depends on:** None
- **Problem:** A queue of rolls can defer narration, main-view refreshes, and state consequences until later rolls finish.
- **Desired behavior:** Treat each resolved roll as a presentation boundary. After its brief result linger, publish its narration and applicable state/UI changes before starting the next queued roll.
- **Acceptance notes:** When harvesting several different parts, the result for part A appears immediately after part A's die resolves, while part B has not started rolling yet. The same rule applies to combat and exploration checks. A true multi-die roll remains one resolution boundary.

### Bug-003 — Make simultaneous dice collisions believable

- **Type:** Bug / investigation
- **Priority:** P2
- **Status:** Planned
- **Depends on:** None
- **Problem:** Visible dice in a true multi-die roll can pass through one another, breaking immersion.
- **Current technical context:** The offscreen Bepu simulation already places every die in one world, permits dynamic-body contact generation, and creates contact constraints. Whole-trajectory face substitution uses rotations verified as symmetries of each convex hull, so it should preserve the collider's occupied volume. Improvement-001 also replaced independent playback tasks with one shared batch clock. However, the existing test named `Simulate_BatchPreservesCollisionsAndDisplaysEachRequestedFace` proves only that the batch settles and displays the requested faces; it does not prove that a die-to-die contact occurred or produced visible deflection. The original pass-through report must therefore be re-established against the accepted playback baseline before choosing a fix.
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

#### Delivery targets

- Every deterministic contact scenario proves either a recorded physical deflection or an explicitly generated collision-free path.
- No confirmed die-to-die pass-through is visible at 30, 60, or 144 FPS sampling schedules.
- Requested-face, exact-final-pose, natural-settle, grip, wall containment, and Improvement-001 timing guarantees remain green.
- Contact handling uses one shared batch timeline and stays within the configured playback cap.
- No `.tscn`, `.tres`, or other Godot-serialized file is edited by an agent; any unavoidable editor wiring is supplied as exact instructions.
- Focused tests, the full `dotnet test` suite, the Godot C# build, and a manual multi-die visual gate pass before completion.

### Bug-004 — Define physical result semantics for tetrahedral d4 dice

- **Type:** Bug / physics-model investigation
- **Priority:** P2
- **Status:** Needs manual confirmation
- **Problem:** The shared predetermined-roll acceptance rule expects a labeled face normal to finish nearly parallel to world up. A tetrahedral d4 normally rests on one triangular face while its readable result is associated with the opposite vertex; it has no horizontal top face equivalent to a d6. A deterministic all-shape timing experiment could not produce a d4 trajectory satisfying the common `dot(up) >= 0.95` rule, even though other serialized shapes did.
- **Desired behavior:** Define the d4 result from its physically resting support face/opposite result vertex, preserve readable label orientation, and validate it without weakening the flat-face rule for other dice.
- **Acceptance notes:** First reproduce and visually confirm the issue in the dice test scene. Then add d4-specific physical-settle/result tests before changing simulation or calibration behavior. Do not fold this investigation into presentation-speed tuning.

## Issues

### Issue-001 — Remove a resolved die before the next queued roll

- **Type:** UX / presentation sequencing
- **Priority:** P1
- **Status:** Proposed
- **Depends on:** Bug-002
- **Problem:** Sequential checks can leave dice visible or overlap rolls, making independent checks look like one multi-die event.
- **Desired behavior:** Roll → briefly display the result → remove the resolved die → let the game react → begin the next queued roll.
- **Acceptance notes:** Only dice belonging to one true multi-die roll may share the screen. Sequential harvest, attack, damage, initiative, inspect, disarm, and similar checks must appear separately.

### Issue-002 — Make initiative order explicit before combat actions

- **Type:** UX / combat clarity
- **Priority:** P1
- **Status:** Proposed
- **Depends on:** Bug-002, Feature-001
- **Problem:** If monsters win initiative, an enemy attack can begin immediately after the initiative dice. The player may take damage or die before understanding that initiative was resolved or who acts first.
- **Desired behavior:** After initiative resolves, clearly announce that the order is set and show the resulting order before any combatant acts.
- **Acceptance notes:** Both the narrative log and main view identify the order. There is a readable presentation beat before the first turn begins, including when a monster acts first.

### Issue-003 — Improve repeated harvest-roll pacing

- **Type:** Design decision / pacing
- **Priority:** P1
- **Status:** Proposed
- **Depends on:** Bug-002, Issue-001, Improvement-001
- **Problem:** Rolling separately for many harvestable parts can make a small corpse take roughly ten seconds or more to resolve.
- **Preferred path:** First evaluate rapid sequential rolls after per-roll reactions and faster presentation are implemented. The target experience is several clearly resolved checks within only a few seconds.
- **Fallback design:** If rapid sequential presentation remains tedious, use one roll per item stack with tiered difficulty thresholds determining how many items in that stack are harvested.
- **Decision criteria:** Preserve meaningful uncertainty and clear per-result feedback while avoiding repetitive spectacle. Document the chosen harvesting rule in the GDD before implementation.

## Improvements

No active improvements.

## Features

### Feature-001 — Main-view turn and action indicators

- **Type:** Feature / combat UX
- **Priority:** P1
- **Status:** Proposed
- **Depends on:** Bug-002
- **Problem:** The narrative log records turns and actions, but the main view does not make the active combatant or current action sufficiently clear.
- **Desired behavior:** Add persistent or animated main-view indicators for whose turn it is and what action is being taken.
- **Acceptance notes:** At a glance, the player can identify the active combatant and action without reading historical log entries. Indicators remain synchronized with initiative, dice presentation, resolution, death, and turn advancement.

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

### Improvement-001 — Reduce average die-roll presentation below one second

- **Type:** Improvement / investigation
- **Priority:** P1
- **Status:** Completed
- **Completed:** 2026-08-13
- **Acceptance evidence:** The user visually verified the implementation and reported that it works well enough to establish the new accepted baseline.
- **Outcome:** The complete 60 Hz Bepu trajectory remains authoritative, while visible predetermined playback uses a shared monotonic elapsed-time clock, linear position interpolation, normalized quaternion interpolation, and an explicit exact final pose. True multi-die rolls share one timeline. The default readability hold is `0.20s`, and non-positive playback caps still provide a recorded-speed rollback.
- **Accepted tuning:** The C# fallback cap remains `0.75s`; the user-owned `RollingDie.tscn` currently overrides **Maximum Predetermined Playback Seconds** to `1.0s`, which is the value used for visual acceptance.
- **Verification:** 370 xUnit tests passed, the Godot C# project built with 0 errors, and no Godot-serialized asset was edited by an agent.
- **Commit:** Pending; the implementation and documentation remain in the current working tree.

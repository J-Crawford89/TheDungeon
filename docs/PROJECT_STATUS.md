# TheDungeon — Development Baseline (updated 2026-08-10)

This is a dated re-entry map for resuming development after the project was paused. The [Game Design Document](./GAME_DESIGN_DOCUMENT.md) remains canonical for player-facing intent, and the [Technical Design Document](./TECHNICAL_DESIGN_DOCUMENT.md) remains canonical for implementation and architecture.

## What the project is

TheDungeon is an old-school, turn-based dungeon-crawl RPG built with Godot 4.6 and C#. A run currently combines procedural room exploration, hidden discoveries and traps, turn-based monster combat, character progression, equipment, treasure, corpses, containers, and deliberate loot transfer.

The main runtime state is `GameSessionState`. `GameRoot` constructs the services and repositories for a run and packages them in `GameRunContext`. Code is split into downward-only tiers: Core → State / Game.Contracts → Game → UI, with Godot scene composition above those libraries.

## Re-established baseline

- Branch before this work: `main_development`; last committed baseline was `069576f` (`dice tweaks`, 2026-05-22).
- Automated baseline after the predetermined-trajectory slice: 342 xUnit tests passing and the Godot C# project building successfully.
- Container/corpse/loot work is substantially implemented; [`LOOT_CONTAINERS_ROADMAP.md`](./LOOT_CONTAINERS_ROADMAP.md) retains the delivery history and remaining polish.
- Save/load is not implemented (`GameRoot.HandleLoadGame()` is still empty).
- The GDD is useful as a system index but intentionally still contains major product-design TODOs: player fantasy, run structure, pacing, progression, item tiers, accessibility, and balance targets.

## Current dice milestone

The intended architecture is now explicit:

- Backend rolls are authoritative. Physics is presentation, never the source of gameplay truth.
- Forced/gameplay rolls are simulated to completion offscreen at fixed 60 Hz with BepuPhysics 2.4. The simulation uses each visual's exact convex collider, the floor, all four walls, and every die in the batch.
- Dice spawn at random usable points within the play area. The baseline throw is `6-9` units/second horizontally with no intended upward speed. Initial roll is calculated from that speed and the current hull radius at `0.82-0.98` coupling, plus at most `1.25` radians/second of off-axis tumble. Surface contact refines the roll instead of being expected to create rotation from a sliding release.
- The simulator identifies the naturally landed face, then applies the local symmetry from that face to the requested face to every recorded orientation, starting at frame zero. The visible roll therefore follows a natural trajectory and never shows a wrong result before correction.
- Visible forced rolls replay the recorded poses on a frozen, non-colliding Godot body. The predictive torque, braking, stall recovery, and catastrophic single-frame snap systems have been removed.
- Free test rolls remain live Godot rigid-body simulations and retain only a deadlock timeout.
- The target face is measured against world up, never the camera. Direct snapping exists only as an explicit calibration test button.
- Hit, damage, initiative, flee, potion, trap-disarm, inspect, and harvest flows continue to await dice presentation before narration or consequential state mutation.
- The test harness reports launch speed, effective rolling radius, coupling, theoretical release slip, measured average/final contact slip, time-to-grip, initial/max spin, accumulated rotation, natural/displayed face, duration, final face dot, and whether the trajectory settled naturally. Play-area walls remain available as translucent debug meshes.

## Godot Editor values to verify

Calibration wiring and the previously requested visual, floor, and wall values are complete. Verify the revised exported script values below after Godot reloads the C# assembly; agents must not edit these `.tscn` files.

### `RollingDie.tscn`

Open `Scenes/Components/Dice/RollingDie.tscn` and select the root `RollingDie`. The scene currently has no local overrides for these properties, so the new C# defaults should appear automatically; only set a value manually if Godot displays something different:

| Group | Inspector field | Value |
|---|---|---:|
| Spawn | Spawn Position | `(0, 1.35, 0)` |
| Natural Toss | Min Throw Speed | `6.0` |
| Natural Toss | Max Throw Speed | `9.0` |
| Natural Toss | Min Roll Coupling | `0.82` |
| Natural Toss | Max Roll Coupling | `0.98` |
| Natural Toss | Max Tumble Jitter | `1.25` |
| Predetermined Surface | Surface Grip | `1.0` |
| Presentation | Post Roll Display Seconds | `0.5` |

The old impulse, upward launch, Euler range, targeting, raw spin, damping, timeout, and settle exports have been removed. `SimulationBoundsHalfExtents` is assigned by the overlay/test parent in code, while the remaining solver values are internal constants. If `RollingDie.tscn` has no local overrides, the C# defaults already provide the compact control set above.

### Every die visual scene

These previously applied values remain the baseline for `d3_visual.tscn`, `d4_visual.tscn`, `d6_visual.tscn`, `d8_visual.tscn`, `d10_visual.tscn`, `d10_percentile_visual.tscn`, `d12_visual.tscn`, and `d20_visual.tscn`:

1. Select the root `DieVisualBody`.
2. Set **Mass** to `0.25`, **Linear Damp** to `0.35`, and **Angular Damp** to `0.35`.
3. Select its `CollisionShape3D`; verify **Transform > Scale** is `(1.01, 1.01, 1.01)`.
4. Keep the existing convex shape, `Calibration` reference, and face entries unchanged. Save the scene.

### Random spawn settings

- In `dice_test_scene.tscn`, select the scripted root and verify **Spawn Bounds Half Extents** `(7, 0, 3.5)`, **Spawn Height** `1.35`, **Spawn Edge Margin** `0.75`, **Minimum Spawn Separation** `1.25`, and **Random Spawn Attempts** `16`.
- In `dice_roll_overlay.tscn`, select the scripted root and verify the same bounds, height, and edge margin, plus **Minimum Spawn Separation** `1.25` and **Random Spawn Attempts** `16`.

### Dice floors and walls

- In `dice_test_scene.tscn`, select `Floor`, expand **Physics Material Override**, and set **Friction** to `0.65` and **Bounce** to `0.20`.
- In `dice_roll_overlay.tscn`, select `SubViewportContainer/SubViewport/DiceWorld/Floor`. Set **Physics Material Override** to a new `PhysicsMaterial`, then set **Friction** to `0.65` and **Bounce** to `0.20`.
- Those Godot materials govern live/free rolls. Predetermined gameplay rolls use the independent offscreen baseline: floor grip `1.0` and wall friction `0.15`, so the floor converts residual slip into roll without making wall contact sticky.
- Keep `PlayAreaWalls.WallHeight = 20` in both scenes. Keep `ShowDebugWallMeshes = true` in the test scene while diagnosing containment and `false` in the production overlay.

## Manual verification checklist

1. Build C# in Godot, then run `dice_test_scene.tscn`.
2. Confirm four translucent red walls appear, including the top edge, with no visible corner gaps.
3. Use **Spawn + free roll** repeatedly for every die. Free rolls should remain entirely physics-driven and the status should say `natural settle`.
4. Confirm each spawn position and travel direction varies while remaining inside the walls. Dice should begin close to the floor, drop into it, and spend most of the roll in surface contact rather than following an airborne arc.
5. Use **Spawn + gameplay roll** repeatedly for every face of every die type. The requested face must be the visible result throughout the coherent roll; there must be no wrong-face pause, corrective acceleration, second flick, or final snap.
6. Watch the status text: launch speed should be `6.0-9.0`, coupling `0.82-0.98`, and theoretical release slip normally below about `1.6`. A d6 initial spin will usually be roughly `7-12` radians/second because it is now derived from travel speed and radius, not independently invented. Forced rolls should report grip within `1.0s` of accumulated floor contact, average slip at most `2.0`, final slip at most `0.4`, at least about `0.5` total turns for the automated d6 launch matrix, `settled=True`, and final face `dot` near `1.0`. These are diagnostic bounds, not a substitute for judging the visible motion.
7. Use **Snap in place** and **Verify calibration** for every face. These are manual diagnostics only; the requested face should rank first with `dot(up)` near `1.0`.
8. Test simultaneous multi-die and d100 rolls, including visible die-to-die contact and repeated throws near all four walls. Both d100 dice should begin and finish together rather than rolling serially.
9. In the main game, verify combat order: hit die resolves -> attack narration; damage die resolves -> HP/death/corpse/turn advancement. The resolved dice should still be visible briefly during narration.

## Likely follow-up work

- Play-test the baseline motion values above across every die shape, then tune from recorded examples rather than changing several variables at once.
- If the transparent production overlay still lacks a convincing contact cue, evaluate lighting/shadows or a slight camera tilt separately from physics.
- If visual tuning is still needed, change one high-level family at a time: throw speed, roll coupling, tumble jitter, surface grip, or presentation linger. Preserve the automated slip bounds while tuning.
- Complete the unresolved product decisions listed in the GDD and design save/load before implementing persistence.

## AI editing boundary

Agent rules live in [`.cursor/rules/`](../.cursor/rules/). Agents may edit C# and documentation, must add/run tests for behavior changes, and must not edit `.tscn`, `.tres`, or other Godot-serialized assets. Required scene/resource changes must be given as exact Godot Editor instructions.

# Feature-003 — UI scaling plan and chat-mode handoff

Status: planning complete; no Godot scene or project settings have been changed.

## Outcome to target

Deliver Feature-003 in two deliberately separate stages:

1. **Safe scalable-window baseline:** author the game against a 1280×720 logical canvas and let Godot scale that canvas uniformly to larger windows. Keep a 16:9 presentation area and letterbox/pillarbox other aspect ratios. This directly fixes the current problem without first rebuilding every screen.
2. **Optional true responsive layout:** after the baseline is visually accepted, allow the logical canvas to expand on 16:10 and ultrawide windows, then make the few fixed-layout regions consume that extra space intentionally.

Stage 1 is sufficient to call Feature-003 complete if the desired product scope is “the current UI, but usable at 1080p, 1440p, and 4K.” Stage 2 should only be included in Feature-003 if native use of non-16:9 space is a requirement.

## Backlog item

`Feature-003 — UI scaling` is currently Proposed, P2, accessibility-related, and asks the project to define:

- supported resolutions;
- scaling behavior;
- minimum readable sizes; and
- global versus region-specific scaling.

The backlog does not yet prescribe an implementation.

## Recommended product decisions

### Stage 1 support policy

- **Logical design size:** 1280×720.
- **Minimum supported content area:** 1280×720 logical units.
- **Larger supported 16:9 windows:** 1600×900, 1920×1080, 2560×1440, and 3840×2160.
- **Other aspect ratios:** supported safely through letterboxing/pillarboxing. No controls may be clipped, even though the game will not use the extra screen area yet.
- **Window behavior:** windowed and resizable; maximizing must work.
- **Scaling policy:** one global automatic UI scale. Do not give the character, log, map, notebook, or overlays independent scale sliders.
- **Stretch policy:** `Canvas Items`, `Keep`, and fractional scaling.
- **User-adjustable scale:** defer until the Options screen is real. If added, expose a single global setting, not per-panel scaling.

This policy matches the current art and layout. The main-view composition and dice overlay are both authored as fixed 16:9-era canvases, so `Keep` is the safe first setting. It also keeps text crisper than rendering the entire game to one low-resolution viewport and enlarging that texture.

### Minimum readability policy

At the 1280×720 logical baseline:

- ordinary and button text: **14 px minimum**;
- compact secondary text: **12 px minimum**, and it must not carry the only copy of essential information;
- panel titles: **18 px minimum**;
- actionable controls: aim for **40×40 logical units** or larger;
- overflow content belongs in a `ScrollContainer`; it must not be solved by shrinking essential text.

The current 9 px notebook action-button theme and 9 px harvest hint fail this target and should be raised during Stage 1 polish.

## What the project currently does

### Project-level behavior

`project.godot` has no explicit `Display > Window` size or stretch settings. Under Godot 4.6 defaults this means:

- viewport/design size: 1152×648;
- resizable window: enabled by default;
- stretch mode: disabled;
- stretch aspect: keep, but it has no effect while stretch mode is disabled.

With stretch disabled, one logical UI unit remains one screen pixel. Resizing the OS window therefore gives containers more pixels, but it does not enlarge fixed-size artwork, fonts, icons, or controls. This is the main reason a larger window currently looks like the small layout surrounded by additional space.

### What is already in good shape

- `GameRoot`, `ScreenHost`, the three main screens, the full-screen overlays, and the outer `MainUI` margin all use full-rect anchors.
- The main HUD uses nested `VBoxContainer`/`HBoxContainer` nodes and stretch ratios.
- The log, character notebook page, loot list, and long text surfaces already use scroll containers in the most important places.
- `MainUi` listens for viewport size changes, although full-rect anchors already perform most of that job.
- The command panel uses a `FlowContainer` for dynamically generated target buttons, which is a good responsive pattern.

### Fixed-layout hotspots

| Surface | Current constraint | Consequence |
|---|---|---|
| Main view | `Stage` and `BackgroundRect` are exactly 744×404; door and feature positions are absolute | The room composition centers but does not grow with a larger logical panel |
| Main-view art | `Background.png` is 744×404 and wall/creature assets are raster images | Arbitrary enlargement can soften prototype art |
| Dice overlay | `SubViewport` is exactly 1280×720; its container does not have Stretch enabled | Native use of an expanded ultrawide canvas needs an explicit viewport/aspect decision |
| Notebook equipment page | Equipment slots use many absolute offsets inside `EquipmentSlotArea` | The equipment silhouette behaves like a fixed design canvas, not a fluid layout |
| Notebook typography | Six action buttons use a local Theme with a 9 px default font | Essential actions are below the proposed readable minimum |
| Backpack and loot | Fixed six-column backpack and five-column loot grids | They scale globally, but do not choose columns from available width |
| Map | Cell, room, padding, and line sizes are fixed; drawing begins from the discovered minimum coordinate | It does not center or fit the discovered map into extra logical space |
| Overlays | Loot panel is at least 475×361; game-over panel is at least 800×375 | Fine for the baseline, but not yet governed by a shared modal width policy |

## Stage 1 — safe scalable-window baseline

### 1. Apply the project settings in the Godot editor

Do not hand-edit `project.godot`. In Godot 4.6:

1. Open **Project > Project Settings**.
2. Select **General > Display > Window > Size**.
3. Set **Viewport Width** to `1280`.
4. Set **Viewport Height** to `720`.
5. Leave **Window Width Override** and **Window Height Override** at `0` for a 1280×720 startup window. If that is awkward on the development monitor, use `1152` and `648` as development-only startup overrides; the logical design size remains 1280×720.
6. Ensure **Resizable** is enabled and **Maximize Disabled** is disabled.
7. Select **Display > Window > Stretch**.
8. Set **Mode** to `Canvas Items`.
9. Set **Aspect** to `Keep`.
10. Set **Scale** to `1.0`.
11. Set **Scale Mode** to `Fractional`.
12. Select **Display > Window > DPI** and keep **Allow HiDPI** enabled.
13. Close Project Settings and save the project.

If the game runs inside Godot's embedded game view, use the editor's embedded-game controls to test several sizes, or temporarily run it as a separate window so the OS resize and maximize controls can be tested directly.

### 2. Perform the first visual gate before changing scenes or code

Run the complete path at 1280×720, 1920×1080, 2560×1440, and maximized/fullscreen:

1. Start menu.
2. Character creation, including long character descriptions.
3. Main exploration HUD.
4. Combat with the initiative overlay and persistent initiative strip.
5. One single-die roll and a true multi-die roll.
6. Character notebook page.
7. Inventory notebook page, including a selected item and every action button.
8. Container/corpse loot overlay with enough items to scroll.
9. Game-over overlay.

Expected result: the same 16:9 composition occupies progressively more physical pixels, all text and controls enlarge together, and non-16:9 windows receive bars instead of exposing broken layout.

This gate determines how much of the remaining Stage 1 work is actually necessary. Do not refactor fixed canvases merely because they are fixed if uniform global scaling already produces the desired result.

### 3. Fix baseline readability in the editor

Only after the first visual gate:

#### `Scenes/Menus/NotebookOverlay.tscn`

1. Open the scene.
2. Select each of `EquipButton`, `UnequipButton`, `UseButton`, `DropButton`, `MoveToBeltButton`, and `CompareButton`.
3. In **Theme > Theme**, remove the local 9 px theme override or change its default font size to at least `14`.
4. Confirm the `ItemDetailButtonsHBox` still fits. If it does not, replace that HBox with a `FlowContainer` or wrap the actions into two rows; do not reduce the font again.
5. Save the scene.

#### `Scenes/Components/LootableItemControl.tscn`

1. Open the scene.
2. Select `HarvestHint`.
3. Change **Theme Overrides > Font Sizes > Font Size** from `9` to at least `12`.
4. Increase the hint's vertical budget if the text clips; the hint may wrap or the card's minimum height may grow.
5. Verify `QuantityLabel` and `ItemName` at the baseline. They are currently 12 px and may remain compact secondary text.
6. Save the scene.

#### Action target sizes

Inspect notebook equipment slots and other small controls at the baseline. Increase any essential interactive slot below roughly 40×40 where doing so does not destroy the equipment diagram. Backpack slots are already overridden to 45×45.

### 4. Optional small C# cleanup

`MainUi` is full-rect anchored but also manually assigns its position and size and subscribes to `Viewport.SizeChanged`. Once Stage 1 is accepted, test whether those assignments can be removed without changing behavior. This is cleanup, not a prerequisite for scaling.

If any C# behavior changes, add focused tests where a pure seam is practical and run the full suite. Editor-only layout changes require the manual matrix, not artificial xUnit tests.

## Stage 1 acceptance criteria

- A 1280×720 window is the authored minimum and contains every essential control.
- The same session remains fully usable at 1920×1080, 2560×1440, and 3840×2160.
- Resizing and maximizing do not leave the UI physically tiny.
- Non-16:9 windows preserve the 16:9 UI with bars; nothing is clipped or stretched non-uniformly.
- Start menu, character creation, HUD, map, log, notebook pages, loot, game-over, initiative, and dice presentation have all passed the visual matrix.
- Essential text is at least 14 logical px; compact secondary text is at least 12 logical px.
- No `.tscn`, `.tres`, or other serialized scene/resource file is changed outside the Godot editor.
- The C# solution builds and all xUnit tests pass after any code changes.

## Stage 2 — true responsive use of additional aspect-ratio space

Do this only if “no bars on 16:10 or ultrawide” is a product requirement.

### 1. Change only the aspect policy first

In **Project Settings > Display > Window > Stretch**, change **Aspect** from `Keep` to `Expand`. Keep the 1280×720 design size and `Canvas Items` mode.

Test 1920×1200 and 2560×1080 immediately. Record problems by surface before editing anything.

### 2. Make the main-view stage fit its allocated region

The room artwork is a coordinated fixed composition, so do not independently anchor the three doors and feature row. Treat the entire 744×404 `Stage` as one design canvas.

Preferred C# behavior in `MainViewPanel`:

- derive one uniform fit scale from `ViewArea.Size` and the unscaled `Stage.Size`;
- preserve aspect ratio;
- center the scaled stage;
- never use separate X/Y scales;
- keep targeting hit regions inside the stage so visuals and input remain aligned.

Rename `CenterStage` to reflect that it now fits and centers. Keep the calculation in a small pure helper so scale/position cases can be unit tested without Godot.

### 3. Treat notebook equipment as a centered design canvas

Do not convert every body slot to an unrelated percentage anchor. The positions represent a diagram.

In `NotebookOverlay.tscn`:

1. Add a `CenterContainer` where `EquipmentSlotArea` currently sits.
2. Reparent `EquipmentSlotArea` into it.
3. Give `EquipmentSlotArea` an explicit design minimum large enough for the current coordinates (approximately 472×430, verified in the editor rather than typed into the scene file).
4. Keep current slot offsets inside that design canvas.
5. Confirm the canvas remains centered when the equipped-items column gains width.
6. Use scrolling or a breakpoint layout if the region becomes smaller than the design canvas; do not overlap the inventory column.

If the goal is for the diagram itself to grow inside extra space, add one uniform fit-scale owner for the whole canvas. Never scale individual slots independently.

### 4. Make the dice viewport aspect-safe

The production dice world is physically designed around an 18×10 play area and a 1280×720 subviewport. Do not casually stretch its texture or change its simulation bounds.

Choose and visually verify one policy:

- **Recommended:** keep the dice presentation on a centered 16:9 canvas while the HUD beneath it may expand; or
- resize the SubViewport with its container and add a full-screen dimmer, accepting that the orthographic camera will expose more horizontal world space on ultrawide screens.

Whichever policy is chosen must preserve the accepted dice trajectory, camera readability, play-area bounds, and requested final faces. Re-run the existing single, d100, multi-die, and forced-collision visual gates.

### 5. Fit and center the map

Extract a pure layout calculation that receives:

- available control size;
- discovered-room coordinate bounds;
- base cell/room/padding sizes.

It should return a uniform scale and centered origin. `MapView` should redraw on resize. Unit-test empty, one-room, wide, tall, and large maps.

### 6. Add responsive breakpoints only where evidence requires them

Potential candidates:

- character creation: two columns at wide sizes; stacked columns plus scrolling when narrow;
- main HUD: retain the current two rows on wide screens; consider a different panel arrangement only if the minimum width fails;
- backpack and loot grids: compute columns from available width only if extra space or clipping is visibly poor;
- modal panels: use consistent edge margins and sensible maximum widths so they do not become excessively wide.

Godot does not provide CSS-style breakpoints automatically. If breakpoints are necessary, keep the threshold decision in a small scene-level layout controller and let containers own the child sizing.

## Stage 2 acceptance criteria

- Stage 1 acceptance remains green.
- 1920×1200 and 2560×1080 use the extra area without nonuniform stretching.
- The room composition, equipment diagram, and dice remain aspect-correct.
- Map content is centered and remains readable.
- No essential control overlaps, clips, or becomes unreachable at the supported aspect ratios.
- Resize behavior is stable while overlays are open and while dice/initiative presentation is active.

## Verification matrix

| Window/client size | Aspect | Stage 1 expectation | Stage 2 expectation |
|---|---:|---|---|
| 1280×720 | 16:9 | Native baseline | Native baseline |
| 1600×900 | 16:9 | Uniformly larger | Uniformly larger |
| 1920×1080 | 16:9 | Uniformly larger | Uniformly larger |
| 2560×1440 | 16:9 | Uniformly larger | Uniformly larger |
| 3840×2160 | 16:9 | Uniformly larger; check raster softness | Uniformly larger; check raster softness |
| 1920×1200 | 16:10 | Letterboxed | Extra vertical canvas used intentionally |
| 2560×1080 | 21:9 | Pillarboxed | Extra horizontal canvas used intentionally |

For every row, verify startup, character creation, exploration, combat/initiative, dice, notebook character page, notebook inventory page, loot overlay, and game-over overlay.

## Relevant files

- `project.godot` — currently inherits Godot's disabled-stretch defaults.
- `Scenes/game_root.tscn` — full-rect screen host.
- `Scenes/MainUI/main_ui.tscn` — primary container layout and modal overlays.
- `Scenes/MainUI/MainUi.cs` — manual root-size update on viewport resize.
- `Scenes/Components/main_view_panel.tscn` — fixed 744×404 room stage.
- `Scenes/Components/MainViewPanel.cs` — currently centers, but does not fit-scale, the stage.
- `Assets/PrototypeAssets/Background.png` — 744×404 source art.
- `Scenes/Components/MapView.cs` — fixed-size, top-left-origin map drawing.
- `Scenes/Components/Dice/dice_roll_overlay.tscn` — fixed 1280×720 3D subviewport.
- `Scenes/Menus/NotebookOverlay.tscn` — absolute equipment-slot canvas, fixed grids, and 9 px action-button theme.
- `Scenes/Menus/CharacterPage.tscn` — two-panel notebook page with scroll containers.
- `Scenes/Menus/character_creation_screen.tscn` — container-based two-column creation layout.
- `Scenes/Components/InventorySlotControl.tscn` — 35×35 base slot with 45×45 backpack overrides.
- `Scenes/Components/LootableItemControl.tscn` — fixed 85×102 loot card and 9 px harvest hint.
- `Scenes/Components/LogEntryControl.tscn` — wrapping log row with a 200 px minimum.
- `docs/GAME_DESIGN_DOCUMENT.md` — UX and accessibility intent.
- `docs/TECHNICAL_DESIGN_DOCUMENT.md` — scene ownership, UI architecture, and testing constraints.

## Current repository baseline observed during planning

- Branch: `main_development`, three commits ahead of `origin/main_development` at the time of inspection.
- Existing uncommitted change: `docs/PRODUCT_BACKLOG.md`; it was not modified by this planning pass.
- Automated baseline: 413 xUnit tests passed on 2026-08-14. Existing nullable warnings remain.
- No scene, resource, project setting, or C# behavior was changed while preparing this plan.

## Godot references

- [Godot 4.6 — Multiple resolutions](https://docs.godotengine.org/en/4.6/tutorials/rendering/multiple_resolutions.html)
- [Godot 4.6 — Using containers](https://docs.godotengine.org/en/4.6/tutorials/ui/gui_containers.html)
- [Godot 4.6 — Using viewports](https://docs.godotengine.org/en/4.6/tutorials/rendering/viewports.html)
- [Godot 4.6 — Project settings reference](https://docs.godotengine.org/en/4.6/classes/class_projectsettings.html)

## Suggested chat-mode kickoff

Use this message to continue outside work mode:

> We are working from `docs/FEATURE_003_UI_SCALING_PLAN.md`. Start with Stage 1 only. Walk me through the Godot 4.6 Project Settings changes one screen at a time, then help me run the visual gate before we decide whether any scene or C# work is necessary. Do not edit `.tscn` or `.tres` files directly. Treat my current uncommitted backlog changes as user-owned.

# TheDungeon — Game Design Document

This document is the **canonical reference for gameplay and product design** (fantasy, loops, rules-as-intended, economy, itemization philosophy, UX principles, and balance assumptions, etc.). **Implementation** lives in code and in the [Technical Design Document](./TECHNICAL_DESIGN_DOCUMENT.md). When design and code disagree, treat it as either a bug or explicitly documented technical debt in the TDD.

---

## Game overview

**TODO:** Genre, tone, target audience, and high-level pitch.

The project is an **old-school dungeon-crawl exploration RPG** built in **Godot 4.x** with **C#** (see [README](../README.md)). Expand this section with the player-facing identity of the game (e.g. difficulty posture, session length, narrative framing).

---

## Player fantasy

**TODO:** What the player is meant to feel (e.g. prepared delver, desperate survivor, power collector). Align future systems (death, economy, encounter pacing) with this.

---

## Core loop

**Intent:** A clear cycle of **explore → discover threats/opportunities → resolve** (combat, traps, loot, narrative) **→ grow in power or knowledge → repeat**, with meaningful risk/reward.

**TODO:** Nail session-level goals (how a run starts and ends, what “progress” means in one sitting). The codebase today supports exploration and combat flows via `GameSessionState` and `DungeonMode`; document the **intended** loop independent of implementation gaps.

---

## Dungeon structure

**Design intent:** Dungeons are **procedurally generated** — layout, room connectivity, and placement of challenges should feel varied across runs and support the core loop above.

- **Not a design target:** Fixed **hand-built** floors used in development or automated tests (see [TDD — Dungeon generation](./TECHNICAL_DESIGN_DOCUMENT.md#dungeon-generation-implementation)) are **implementation details**, not a player-facing mode or pillar.

**TODO:** Design targets for floor count, room density, biome/tile themes, escalation per floor, and how procedural parameters express difficulty.

---

## Combat and damage

**Intent:** Turn-based combat with clear initiative, player actions, and enemy actions; exploration and combat are distinct modes.

**Armor and damage reduction (player-facing rules):** Effects apply in this precedence order (full technical ADR: [TDD — ADR-0007](./TECHNICAL_DESIGN_DOCUMENT.md#adr-0007-armor-damage-reduction-targeting-and-lookup)):

1. If a **specific damage type** is set on the effect, it applies only to that type.
2. Else if a **damage family** is set, it applies to that family.
3. Else it applies to **all** damage.

**TODO:** Document intended combat cadence (flee, defend, items in combat), monster roles, and any difficulty knobs exposed to design.

### Dice and outcome presentation

**Design decision:** Gameplay rolls are decided by the rules backend; the 3D dice communicate that already-authoritative result. Physics should make the toss feel tactile and variable, but it must not silently change a result or become dependent on camera angle, frame rate, collision quirks, or a die leaving the play area.

A roll should read as one coherent beat:

1. The die appears at a random usable point with its lowest hull point just above the play surface and receives a bounded horizontal throw. Release rotation is derived from throw speed and the vertical center-to-contact lever arm, so it reaches the floor at nearly the no-slip rate; a small bounded off-axis component adds tumble without creating uncontrolled spin. Floor and wall contact then vary and dissipate that motion instead of being expected to create rotation from a sliding body.
2. For an authoritative gameplay result, the game first runs the complete rigid-body throw in a fast offscreen simulation. The simulation uses the die's convex collider plus the same floor, walls, and other dice in that batch. No result-seeking force or torque is applied.
3. A candidate is accepted only after it changes its upward face at least once and settles flat on a legitimate collider face. An unstable tip/ridge balance or no-tumble result is discarded and naturally re-thrown offscreen, up to a small bounded number of attempts.
4. Once the simulated die settles naturally, its upward physical face is identified. Approximate label directions are projected onto the collider's support faces through valid hull symmetries, which makes shapes such as the d10 visually unambiguous.
5. The requested face is substituted for that natural face by applying a valid local symmetry rotation to every recorded orientation, beginning with the first frame. Position, timing, collisions, and angular motion remain those of the natural throw.
6. The complete recorded trajectory is then replayed visibly. Long trajectories may be time-compressed for pacing, but no physical frame is used as an early cutoff: visible playback samples the whole path and always ends on the exact settled pose. Dice from the same true multi-die roll share one presentation clock. Because the result mapping exists for the whole animation, a die never displays one result and then corrects, snaps, or receives an invisible second flick.
7. Free/test rolls may still use live Godot physics when no outcome has been predetermined.
8. After the face becomes readable, the result remains visible for a brief profile-controlled pause. All dice belonging to that logical roll are then removed before presentation completes.
9. The game commits narration and consequences such as damage, death, discovery, healing, or loot, then refreshes the affected UI before another independent roll begins.
10. Only dice belonging to one true multi-die roll may share the screen. Independent checks are always presented as separate beats.

The camera is presentational, not part of the rule for deciding "up." A gameplay face is oriented to the play surface. A simulation timeout is a diagnostic guard: it may end recording a pathological trajectory, but it must never trigger a visible result snap. Normal rolls are not cut off by a randomized animation duration; presentation pacing may compress only a complete, already-resolved trajectory.

#### Repeated harvest checks

**Design decision:** Harvesting retains one authoritative check per individual item. A stack of six harvestable teeth therefore produces six distinct checks rather than one roll with tiered thresholds.

- A lone harvest check uses normal dice pacing.
- When one loot action contains multiple harvest checks, those checks use rapid sequential pacing: each complete trajectory is displayed at `0.75x` the normal playback cap and its readable result pause is `0.5x` the normal pause.
- Timing compression never truncates physics. Each rapid roll still replays the complete recorded trajectory and exact final face.
- Each attempt is presented alone, removed, logged with its indexed success or failure, applied to inventory when successful, and reflected in the UI before the next attempt starts.
- A concise stack summary follows the final attempt. True multi-die rolls remain simultaneous and are not converted into rapid independent rolls.

The one-roll-per-stack tiered-DC alternative is not the current rule. Revisit it only if visually accepted rapid sequential pacing still proves tedious in play.

---

## Progression

**TODO:** Leveling curve intent, experience sources, equipment tiers, and ability unlock pacing. Current systems include experience services and character creation grants; document **targets** here.

---

## Economy and currency

### Model

- **Coin types:** Copper, Silver, Gold, Platinum.
- **Rates:** 100 Copper = 1 Silver; 100 Silver = 1 Gold (10,000 Copper); 100 Gold = 1 Platinum (1,000,000 Copper equivalent).
- **Purse:** Four non-negative counts per denomination. Coins **do not auto-consolidate** across denominations (e.g. 150 Copper stays 150 Copper until a future exchange/bank flow exists).
- **Wealth comparisons:** Total equivalent copper is used as a scalar for comparisons and afford checks; it does not change how coins are stored.

### Authoring (design-facing)

- Treasure and backgrounds grant currency via data definitions; item sell/compare baseline uses a copper-equivalent value on items.
- Serialized `.tres` authoring for grants and balances is **editor-owned**; see [TDD — Godot integration](./TECHNICAL_DESIGN_DOCUMENT.md#godot-integration-boundaries-serialized-assets).

### UI

- The player sees denomination lines when count ≥ 1 (character surface and inventory surfaces share the same presentation pattern).

Implementation references: [`CoinPurse`](../Scripts/0.Core/Economy/CoinPurse.cs), [`CurrencyMath`](../Scripts/0.Core/Economy/CurrencyMath.cs).

---

## Itemization philosophy

**Design intent**

- Some loot is **disposable junk** — only meaningful as loot pressure or **vendor fodder** (sell value), not as part of crafting, quests, narrative hooks, or shared systems.
- Items that **matter across systems** (recipes, other drop sources, NPCs, repeated identity) should be the **same item everywhere** in the player’s mind: one definition, consistent name and behavior, not one-off silos per creature.

**Authoring and content pipeline (where files live, `ItemDatabase`, repository merge order)** are **technical** rules: [Technical Design Document — Monster death loot authoring](./TECHNICAL_DESIGN_DOCUMENT.md#monster-death-loot-authoring-embedded-vs-standalone), and [ADR-0009](./TECHNICAL_DESIGN_DOCUMENT.md#adr-0009-monster-death-loot--embedded-vs-standalone-items).

### Broader itemization

**TODO:** Rarity tiers, affix philosophy, stack rules, identify rules, and how consumables interact with combat/exploration pacing.

---

## Content taxonomy

**Intent:** A consistent way to think about **content types** the player interacts with:

- **Creatures** — monsters (combatants), possibly NPCs for lore/quests.
- **Gear and items** — equipment, consumables, quest objects, vendor trash.
- **Hazards** — traps, environmental costs.
- **Rewards** — treasure, chests, currency, containers.
- **Abilities** — class/race/background grants and combat abilities.

**TODO:** Map these to definition ids, player-visible categories, and authoring ownership (database vs embedded) where it affects design.

---

## UX principles

**Intent (high level):**

- **Readable narrative** — the game log carries clarity of outcomes (combat, loot, traps).
- **Explicit inventory and loot** — taking loot is deliberate; container flows should avoid silent backpack fills where possible (see roadmap).
- **Character clarity** — stats, purse, and combat-relevant state visible when relevant.
- **Responsive consequences** — important actions and outcomes receive immediate, coordinated visual and audio feedback rather than relying exclusively on the narrative log.
- **Accessible game feel** — animation, flashes, screen shake, and sound reinforce information but are never the only way essential information is communicated.

### Audiovisual feedback and game feel

The game should communicate important events through several synchronized channels: persistent state in the main view, concise narrative text, transient UI animation or graphical effects, and audio. These channels reinforce one another; none may contradict the authoritative result or make gameplay wait indefinitely for presentation.

The initial event families to design are:

- **Combat actions:** attack wind-up or declaration, hit, miss, critical outcome, damage, healing, defend, creature death, player death, initiative confirmation, turn change, and battle victory.
- **Exploration and hazards:** room entry, inspection discovery, trap discovery, trap trigger, trap disarm, lock failure, chest unlock, and meaningful loot acquisition.
- **Dice:** throw, tumble/roll, collision, and settle cues synchronized with the visible recorded trajectory, including rapid sequences and true multi-die rolls.
- **System transitions:** menu entry, exploration, combat, victory, defeat, and other major state changes that may drive music or larger presentation beats.

#### Initial feedback language

- A damaged monster may flash or tint red while a short slash, impact, or damage-type effect crosses or overlays its image.
- Player damage may use a brief UI shake, red edge flash, portrait/stat emphasis, and impact sound. Trap damage should share enough language to read as harm while retaining a distinct hazard cue.
- Successful trap disarming, chest unlocking, harvesting, healing, and battle victory should have recognizable positive confirmation cues rather than only log text.
- Misses, blocked actions, and failures need lighter feedback that remains distinct from successful impact.
- Feedback intensity should correspond to consequence. Routine events must not compete visually or sonically with critical hits, death, victory, or major discoveries.

#### Presentation rules

Gameplay state remains authoritative. It emits a semantic event after an outcome is known; presentation selects the available sound, animation, overlay, and timing. Missing or placeholder assets must degrade gracefully, and disabling an effect must never alter rules, sequencing, or state.

Presentation events need explicit policies for priority, overlap, interruption, queuing, and cancellation. A rapid sequence should not accumulate an unreadable backlog of shakes, flashes, or sounds. Dice presentation, narration, main-view updates, and reactions should share deliberate ordering boundaries, while purely decorative tails may finish asynchronously when safe.

The system should be designed and tuned using simple placeholder shapes, colors, shaders, and temporary sounds before final art is available. Final assets can replace those implementations without changing gameplay contracts or event semantics.

#### Audio direction

Sound effects and music are separate controllable layers. Dice audio should make throws, surface contact, tumbling, collisions, and settling feel physical without producing a wall of repeated samples. Variation, rate limiting, and playback-speed-aware synchronization will be necessary.

Music should respond to authoritative modes such as menu, exploration, and battle. Transitions should normally crossfade or otherwise preserve continuity, and repeated notifications of the same mode must not restart the current track. Later scoping should cover victory/defeat stingers, bosses, safe areas, special rooms, track rotation, and resume behavior.

#### Accessibility and settings requirements

- Independent music and sound-effect volume controls, including mute.
- Reduced-motion behavior for shakes and large movement.
- Adjustable or disabled screen shake.
- Reduced flash intensity and avoidance of unsafe rapid flashing.
- Persistent textual/state feedback even when audio or animation is disabled.

**TODO:** Define the complete semantic event catalog, feedback matrix, intensity tiers, accessibility defaults, asset specifications, input posture, and detailed failure feedback before implementing Feature-011.

Delivery detail for containers and loot UX: [Loot, containers, and phased delivery](./LOOT_CONTAINERS_ROADMAP.md).

---

## Balance assumptions

**TODO:** Baseline combat length, expected damage intake, resource scarcity (healing, consumables, currency), and how procedural parameters and `GameBalanceSettings` express difficulty.

---

## Open design questions

Use this list to track unresolved product decisions (ordered loosely by impact):

- Session/run structure and permadeath vs legacy saves (when persistence exists).
- Target session length and “one more floor” tension.
- Procedural dungeon pillars (breadth vs depth, secret density).
- Long-term economy sinks and rewards.

---

## Related documents

- [Technical Design Document](./TECHNICAL_DESIGN_DOCUMENT.md) — architecture, runtime, repositories, testing, ADRs.
- [Loot, containers, and phased delivery](./LOOT_CONTAINERS_ROADMAP.md) — implementation roadmap for container-backed loot.
- [Abilities: Godot editor setup](../Documentation/Abilities-Godot-setup.md) — operational wiring (not game design, but required to realize abilities in-editor).

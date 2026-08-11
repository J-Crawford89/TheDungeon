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
6. The recorded trajectory is then replayed visibly. Because the result mapping exists for the whole animation, the die never displays one result and then corrects, snaps, or receives an invisible second flick.
7. Free/test rolls may still use live Godot physics when no outcome has been predetermined.
8. Only after the face is readable and the die is physically settled does the game reveal narration and apply consequences such as damage, death, discovery, healing, or loot.
9. The resolved die may linger briefly while the player reads the outcome.

The camera is presentational, not part of the rule for deciding "up." A gameplay face is oriented to the play surface. A simulation timeout is a diagnostic guard: it may end recording a pathological trajectory, but it must never trigger a visible result snap. Normal rolls are not cut off by a randomized animation duration.

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

**TODO:** Accessibility targets, input posture, and failure feedback.

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

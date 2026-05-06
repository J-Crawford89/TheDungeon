# Monster death loot authoring

This complements **`GodotItemDefinitionRepository`** (which merges embedded death-loot `ItemResource` references from `MonsterResource.DeathLoot` so corpse validation and inventory can resolve IDs).

## Embedded vs standalone items

| Situation | Where to author the item |
|-----------|--------------------------|
| **Trash / vendor fodder only** — sell value, no crafting, alchemy, quests, or shared drops | May stay **embedded** as `ItemResource` sub-resources on the monster’s `MonsterResource` (`DeathLoot` rows). Keeps file count down and keeps drops local to the creature. |
| **Any other use** — recipes, alchemy, traps, chest tables, NPC grants, shared loot pools, or anything that should appear in a central catalog | Use a **standalone** `.tres` item under `Content/Item/…` and register it in **`ItemDatabase`**. Reference that asset from death loot (or duplicate linkage only if you intentionally override—see repository duplicate policy). |

When an embedded trash item **later** gains a non-trash use, **move** it to a standalone item resource and the database so other systems have a single source of truth.

## Operational note

At runtime, definitions from **`ItemDatabase` take precedence** over the same id merged from monster death loot.

See **`docs/ARCHITECTURE_DECISIONS.md`** (ADR-0009).

---
name: Damage types and item resources
overview: Godot damage types + item subtype resources via C# only; no agent .tres/.tscn edits. User authors serialized resources in the Editor.
todos:
  - id: damage-core-repo
    content: C# DamageTypeResource, Database, Mapper, IDamageTypeDefinitionRepository, Godot repo, GameRoot/GameRunContext; remove DamageTypes.cs when ready
    status: pending
  - id: item-resources
    content: C# EquipmentResource, ArmorResource, WeaponResource, WeaponDamageComponentResource; ItemMapper (EquipmentResource → EquipmentDefinition; no GearDefinition)
    status: pending
  - id: verify
    content: dotnet build + tests; user checklist for .tres
    status: pending
---

# Damage types + item subtype resources refactor

## Policy

- **No agent-authored or agent-edited `.tres` or `.tscn`.** Extend [`.cursor/rules/godot-no-tscn-edits.mdc`](e:/DevProjects/Godot/the-dungeon/.cursor/rules/godot-no-tscn-edits.mdc) so `.tres` matches `.tscn` (no disk edits; user uses Godot Editor). *Apply that rule change in Agent mode — Plan mode could not edit `.mdc`.*

## Current state

- [`Scripts/0.Core/Damage/DamageTypes.cs`](Scripts/0.Core/Damage/DamageTypes.cs): 17 static definitions; no other code references `DamageTypes`.
- [`Mappers/ItemMapper.cs`](Mappers/ItemMapper.cs): `PotionResource` vs base `ItemResource` only.
- Mirror abilities: `IAbilityDefinitionRepository`, `GodotAbilityDefinitionRepository`, `AbilityResourceDatabase` (existing `.tres` stay editor-owned).

## Implementation (C# only)

1. **Damage types:** `DamageTypeResource`, `DamageTypeResourceDatabase`, `DamageTypeMapper`, `IDamageTypeDefinitionRepository`, `GodotDamageTypeDefinitionRepository`; `GameRoot` export + `GameRunContext` property; delete `DamageTypes.cs` when your DB exists.
2. **Item subtypes:** `EquipmentResource` : `ItemResource`; `ArmorResource` / `WeaponResource` : `EquipmentResource`; `WeaponDamageComponentResource`; extend `ItemMapper` (order: **Weapon** → `WeaponDefinition`, **Armor** → `ArmorDefinition`, **Equipment** → `EquipmentDefinition` — `EquipmentDefinition` is concrete per your model; no `GearDefinition`). Then `PotionResource` / default.
3. **No** separate weapon/armor/equipment DBs/repos — keep `ItemResourceDatabase` + `GetDefinitionsOfType<T>()`.

## User-authored `.tres` checklist

1. Create **`DamageTypeResourceDatabase`** (resource using the new database script), e.g. `Content/Databases/DamageTypeDatabase.tres`.
2. Create **17 × `DamageTypeResource`** (ids/names/families matching old `DamageTypes`), e.g. under `Content/DamageTypes/`.
3. Fill **`DamageTypes`** array on that database with those references.
4. On **`GameRoot`**, assign **Damage Type Database** export.
5. Optionally add **`WeaponResource` / `ArmorResource` / `EquipmentResource`** entries to **`ItemDatabase.tres`** `Items`.
6. For each weapon, set damage rows on **`WeaponDamageComponentResource`** and assign **`DamageType`** to the right `DamageTypeResource`.

Full narrative + mermaid diagram: see Cursor plan file `damage_types_and_item_resources_0545354d.plan.md` (updated in your plans folder).

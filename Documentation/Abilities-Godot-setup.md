# Abilities: remaining Godot editor steps

C# adds ability grants, combat Defend, spell points (SP), and new UI exports. Apply the following in the Godot editor (do not rely on missing assignments at runtime).

## GameRoot

1. Select the **GameRoot** node (main scene).
2. In the Inspector, set **Ability Database** to `res://Content/Databases/AbilityDatabase.tres` (or your own `AbilityResourceDatabase`).

If this is left empty, the game logs a warning and **no ability id** from class/race/background grants will validate—**GrantedAbilities** will stay empty.

## CommandPanel (`main_ui.tscn` → CommandPanel)

1. Add a **Button** for **Defend** (combat only).
2. Assign it to **Defend Button** on the CommandPanel script.
3. Connect is automatic via `_Ready` once the export is set.

## CharacterPanel (`main_ui.tscn` → CharacterPanel)

1. Add a **Label** (or other **Control**) for **SP**; assign to **Sp Label** export. It is shown only when the player has spellcasting (Priest/Mage by default data).
2. Add a **Label** or **Control** for the Defend stance indicator; assign to **Defend Active Indicator**. It is visible only in combat while the Defend buff is active.

## Data already in repo

- `Content/Abilities/defend.tres`, `spellcasting.tres`
- `Content/Databases/AbilityDatabase.tres`
- `Content/World/Character/AbilityGrants/DefendAt1.tres`, `SpellcastingAt1.tres`
- **Knight** class: Defend at level 1. **Priest** / **Mage**: Spellcasting at level 1.

Other classes/races/backgrounds can add **Ability Grants** arrays on their resources the same way.

## Ability ids (must match C#)

- `defend` — `AbilityIds.Defend`
- `spellcasting` — `AbilityIds.Spellcasting`

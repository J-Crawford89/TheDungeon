#nullable enable
using System.Collections.Generic;

/// <summary>Merges starting equipment item ids from class, race, and background into the player inventory (class, then race, then background).</summary>
public static class PlayerStartingEquipmentGrantBuilder
{
	public static void ApplyToInventory(
		CharacterClassDefinition? cls,
		CharacterRaceDefinition? race,
		CharacterBackgroundDefinition? background,
		InventoryState inventory,
		IItemDefinitionRepository itemDefinitions)
	{
		void GrantList(IReadOnlyList<string>? ids)
		{
			if (ids == null)
				return;
			foreach (var raw in ids)
			{
				var id = raw?.Trim() ?? "";
				if (id.Length == 0)
					continue;
				var def = itemDefinitions.TryGetById(id);
				if (def == null)
					continue;
				var row = inventory.AddOrStackOneAndReturnRow(def);
				InventoryEquipmentOperations.TryAutoEquipStartingGearOne(inventory, row);
			}
		}

		if (cls != null)
			GrantList(cls.StartingEquipment);
		if (race != null)
			GrantList(race.StartingEquipment);
		if (background != null)
			GrantList(background.StartingEquipment);
	}
}

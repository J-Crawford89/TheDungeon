#nullable enable

/// <summary>Stable presentation keys for main view and inventory UI; resolved to textures in the Godot game layer.</summary>
public static class PresentationIconKeys
{
	public static class MainView
	{
		public const string Root = "mainview";

		public static string Monster(string monsterDefinitionId) => $"{Root}/monster/{monsterDefinitionId}";

		public static string Trap(string trapDefinitionId) => $"{Root}/trap/{trapDefinitionId}";

		public static string Treasure(string treasureDefinitionId) => $"{Root}/treasure/{treasureDefinitionId}";

		public static string Item(string inventoryItemId) => $"{Root}/item/{inventoryItemId}";

		/// <summary>
		/// Gold piles may still set <see cref="TreasureDefinition.InventoryItemId"/> for content parity; pile art is keyed
		/// by treasure id. Only <see cref="TreasureKind.InventoryItem"/> grants use the item presentation key.
		/// </summary>
		public static string ForTreasureInstance(TreasureDefinition def) =>
			def.GrantKind == TreasureKind.InventoryItem && !string.IsNullOrEmpty(def.InventoryItemId)
				? Item(def.InventoryItemId)
				: Treasure(def.Id);

		public static string Npc(string npcDefinitionId) => $"{Root}/npc/{npcDefinitionId}";

		public static string Lore(string loreDefinitionId) => $"{Root}/lore/{loreDefinitionId}";

		public static string Vertical(FloorConnectionType t) => $"{Root}/vertical/{t}";

		public static bool IsVerticalExitIcon(FloorConnectionType t) =>
			t is FloorConnectionType.Stairs or FloorConnectionType.Hole or FloorConnectionType.Ladder;
	}

	public static class Inventory
	{
		public const string Root = "inventory";

		/// <summary>Inventory notebook / UI item art (may differ from <see cref="MainView.Item"/>).</summary>
		public static string Item(string itemDefinitionId) => $"{Root}/item/{itemDefinitionId}";
	}
}

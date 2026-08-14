#nullable enable

/// <summary>Stable presentation keys for main view and inventory UI; resolved to textures in the Godot game layer.</summary>
public static class PresentationIconKeys
{
	public static class Categories
	{
		public const string Monster = "monster";
		public const string Trap = "trap";
		public const string Container = "container";
		public const string Treasure = "treasure";
		public const string Item = "item";
		public const string Npc = "npc";
		public const string Lore = "lore";
		public const string Vertical = "vertical";
	}

	public static class MainView
	{
		public const string Root = "mainview";

		public static string Monster(string monsterDefinitionId) => $"{Root}/{Categories.Monster}/{monsterDefinitionId}";

		public static string Trap(string trapDefinitionId) => $"{Root}/{Categories.Trap}/{trapDefinitionId}";

		public static string Treasure(string treasureDefinitionId) => $"{Root}/{Categories.Treasure}/{treasureDefinitionId}";

		public static string Item(string inventoryItemId) => $"{Root}/{Categories.Item}/{inventoryItemId}";

		/// <summary>
		/// Currency treasures may still set <see cref="TreasureDefinition.InventoryItemId"/> for content parity; pile art is keyed
		/// by treasure id. Only <see cref="TreasureKind.InventoryItem"/> grants use the item presentation key.
		/// </summary>
		public static string ForTreasureInstance(TreasureDefinition def) =>
			def.GrantKind == TreasureKind.InventoryItem && !string.IsNullOrEmpty(def.InventoryItemId)
				? Item(def.InventoryItemId)
				: Treasure(def.Id);

		public static string Npc(string npcDefinitionId) => $"{Root}/{Categories.Npc}/{npcDefinitionId}";

		public static string Lore(string loreDefinitionId) => $"{Root}/{Categories.Lore}/{loreDefinitionId}";

		public static string Vertical(FloorConnectionType t) => $"{Root}/{Categories.Vertical}/{t}";

		public static bool IsVerticalExitIcon(FloorConnectionType t) =>
			t is FloorConnectionType.Stairs or FloorConnectionType.Hole or FloorConnectionType.Ladder;

		public static string ContainerSalvage() => $"{Root}/{Categories.Container}/salvage";

		public static string ContainerSalvageOpened() => $"{Root}/{Categories.Container}/salvage_opened";

		public static string ContainerCorpse(string monsterDefinitionId) =>
			string.IsNullOrWhiteSpace(monsterDefinitionId)
				? $"{Root}/{Categories.Container}/corpse/unknown"
				: $"{Root}/{Categories.Container}/corpse/{monsterDefinitionId.Trim()}";

		public static string ContainerCorpseOpened(string monsterDefinitionId) =>
			string.IsNullOrWhiteSpace(monsterDefinitionId)
				? $"{Root}/{Categories.Container}/corpse_opened/unknown"
				: $"{Root}/{Categories.Container}/corpse_opened/{monsterDefinitionId.Trim()}";

		public static string ContainerChest(bool locked) =>
			locked
				? $"{Root}/{Categories.Container}/chest_locked"
				: $"{Root}/{Categories.Container}/chest";

		public static string ContainerChestOpened() => $"{Root}/{Categories.Container}/chest_opened";

		public static string ContainerOther() => $"{Root}/{Categories.Container}/other";

		public static string ForContainer(ContainerFeature cf) =>
			cf switch
			{
				SalvageFeature sf => sf.WasOpened ? ContainerSalvageOpened() : ContainerSalvage(),
				CorpseFeature corpse => corpse.WasOpened
					? ContainerCorpseOpened(corpse.SourceMonsterDefinitionId)
					: ContainerCorpse(corpse.SourceMonsterDefinitionId),
				ChestFeature ch => ch.Locked
					? ContainerChest(true)
					: ch.WasOpened
						? ContainerChestOpened()
						: ContainerChest(false),
				_ => ContainerOther()
			};
	}

	public static class Inventory
	{
		public const string Root = "inventory";

		/// <summary>Inventory notebook / UI item art (may differ from <see cref="MainView.Item"/>).</summary>
		public static string Item(string itemDefinitionId) => $"{Root}/{Categories.Item}/{itemDefinitionId}";
	}
}

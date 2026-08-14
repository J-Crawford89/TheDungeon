using System.Collections.Generic;
using Xunit;

public sealed class ContainerLootCapacityPeekTests
{
	private sealed class MapItemRepo : IItemDefinitionRepository
	{
		private readonly ItemDefinition _item;

		public MapItemRepo(ItemDefinition item) => _item = item;

		public IReadOnlyList<ItemDefinition> All => [_item];
		public ItemDefinition? TryGetById(string id) =>
			string.Equals(id?.Trim(), _item.Id, System.StringComparison.Ordinal) ? _item : null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => [];
	}

	[Fact]
	public void LikelyCrowdedAfterTake_EmptyInventoryEmptyAdditions_False()
	{
		var item = new ItemDefinition { Id = "coin", MaxStackSize = 99 };
		var crowded = ContainerLootCapacityPeek.LikelyCrowdedAfterTake(
			new InventoryState(),
			[],
			new MapItemRepo(item),
			maxUnequippedRows: 16);

		Assert.False(crowded);
	}

	[Fact]
	public void LikelyCrowdedAfterTake_WhenBackpackAlreadyOverflows_True()
	{
		var item = new ItemDefinition { Id = "gem", MaxStackSize = 1 };
		var inv = new InventoryState();
		inv.Items.Add(new ItemInstance { Definition = new ItemDefinition { Id = "a", MaxStackSize = 1 }, Quantity = 1 });
		inv.Items.Add(new ItemInstance { Definition = new ItemDefinition { Id = "b", MaxStackSize = 1 }, Quantity = 1 });
		inv.Items.Add(new ItemInstance { Definition = new ItemDefinition { Id = "c", MaxStackSize = 1 }, Quantity = 1 });

		var crowded = ContainerLootCapacityPeek.LikelyCrowdedAfterTake(
			inv,
			[],
			new MapItemRepo(item),
			maxUnequippedRows: 2);

		Assert.True(crowded);
	}
}

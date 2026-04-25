using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class PlayerStartingEquipmentGrantBuilderTests
{
	private sealed class MapItemRepo : IItemDefinitionRepository
	{
		private readonly Dictionary<string, ItemDefinition> _byId;

		public MapItemRepo(params ItemDefinition[] defs) =>
			_byId = defs.ToDictionary(d => d.Id, System.StringComparer.Ordinal);

		public IReadOnlyList<ItemDefinition> All => _byId.Values.ToList();

		public ItemDefinition? TryGetById(string id)
		{
			var k = id.Trim();
			return _byId.TryGetValue(k, out var d) ? d : null;
		}

		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToList();
	}

	[Fact]
	public void ApplyToInventory_ClassThenRaceThenBackground_StacksSameId()
	{
		var rope = new ItemDefinition { Id = "rope", Name = "Rope", MaxStackSize = 99 };
		var repo = new MapItemRepo(rope);
		var inv = new InventoryState();
		var cls = new CharacterClassDefinition { StartingEquipment = ["rope"] };
		var race = new CharacterRaceDefinition { StartingEquipment = ["rope", "rope"] };
		var bg = new CharacterBackgroundDefinition { StartingEquipment = ["rope"] };

		PlayerStartingEquipmentGrantBuilder.ApplyToInventory(cls, race, bg, inv, repo);

		Assert.Single(inv.Items);
		Assert.Equal(4, inv.Items[0].Quantity);
	}

	[Fact]
	public void ApplyToInventory_SkipsUnknownIds()
	{
		var pot = new ItemDefinition { Id = "potion", Name = "Potion", MaxStackSize = 10 };
		var repo = new MapItemRepo(pot);
		var inv = new InventoryState();
		var cls = new CharacterClassDefinition { StartingEquipment = ["missing", "  potion "] };

		PlayerStartingEquipmentGrantBuilder.ApplyToInventory(cls, null, null, inv, repo);

		Assert.Single(inv.Items);
		Assert.Equal("potion", inv.Items[0].Definition.Id);
		Assert.Equal(1, inv.Items[0].Quantity);
	}
}

using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class ContainerLootOperationsTests
{
	private sealed class MapItemRepo : IItemDefinitionRepository
	{
		private readonly Dictionary<string, ItemDefinition> _map = new();

		public MapItemRepo(params ItemDefinition[] defs)
		{
			foreach (var d in defs)
				_map[d.Id] = d;
		}

		public IReadOnlyList<ItemDefinition> All => _map.Values.ToList();
		public ItemDefinition? TryGetById(string id) =>
			string.IsNullOrWhiteSpace(id) ? null : (_map.TryGetValue(id.Trim(), out var d) ? d : null);
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToArray();
	}

	[Fact]
	public void TransferAllContents_Salvage_EmptiesAndRemovesFeature()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 3 }],
		};
		room.Features.Add(salvage);
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = ContainerLootOperations.TransferAllContents(
			session,
			room,
			salvage,
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			"salvage pile");

		Assert.Equal(1, result.StacksGranted);
		Assert.Equal(0, result.StacksSkippedMissingDefinition);
		Assert.True(result.RemovedContainerFromRoom);
		Assert.Equal(3, session.Player.InventoryState.SumQuantityForDefinitionId("coin"));
		Assert.DoesNotContain(salvage, room.Features);
	}

	[Fact]
	public void TransferAllContents_Corpse_LeavesEmptyFeatureInRoom()
	{
		var gem = new ItemDefinition { Id = "gem", Name = "Gem", MaxStackSize = 1 };
		var repo = new MapItemRepo(gem);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var corpse = new CorpseFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "gem", Quantity = 1 }],
		};
		room.Features.Add(corpse);
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = ContainerLootOperations.TransferAllContents(
			session,
			room,
			corpse,
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			"remains");

		Assert.Equal(1, result.StacksGranted);
		Assert.False(result.RemovedContainerFromRoom);
		Assert.Empty(corpse.Contents);
		Assert.Contains(corpse, room.Features);
	}

	[Fact]
	public void TransferAllContents_UnknownId_KeepsRowAndSkips()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents =
			[
				new LootableItemDefinition { ItemDefinitionId = "nope", Quantity = 1 },
				new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 },
			],
		};
		room.Features.Add(salvage);
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = ContainerLootOperations.TransferAllContents(
			session,
			room,
			salvage,
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			"heap");

		Assert.Equal(1, result.StacksGranted);
		Assert.Equal(1, result.StacksSkippedMissingDefinition);
		Assert.Single(salvage.Contents);
		Assert.Equal("nope", salvage.Contents[0].ItemDefinitionId);
		Assert.False(result.RemovedContainerFromRoom);
	}

	[Fact]
	public void ForContainerLootTransferStart_RespectsEmpty()
	{
		var n = new NarrativeService();
		Assert.Contains("nothing to take", n.ForContainerLootTransferStart("pile", 0));
		Assert.Contains("3 stack(s)", n.ForContainerLootTransferStart("pile", 3));
	}
}

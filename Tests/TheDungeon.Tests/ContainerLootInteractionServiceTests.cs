using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class ContainerLootInteractionServiceTests
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

	private static GameSessionState SessionInRoom(DungeonRoom room)
	{
		var s = new GameSessionState();
		var f = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		f.Rooms[DirectionHelper.Origin] = room;
		s.Dungeon.CurrentFloor = f;
		s.Dungeon.PlayerCoord = DirectionHelper.Origin;
		return s;
	}

	[Fact]
	public void TryBuildPanel_Ok_MapsRowsAndKind()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var gem = new ItemDefinition { Id = "gem", Name = "Gem", MaxStackSize = 1 };
		var repo = new MapItemRepo(coin, gem);
		var svc = new ContainerLootInteractionService(repo, new NarrativeService(), TestPlayerProficiencyAggregation.CreateEmpty());
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents =
			[
				new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 2 },
				new LootableItemDefinition { ItemDefinitionId = "gem", Quantity = 1 },
			],
		};
		room.Features.Add(salvage);
		var session = SessionInRoom(room);

		var result = svc.TryBuildPanel(session, 0);

		Assert.Equal(ContainerLootErrorCode.None, result.ErrorCode);
		Assert.NotNull(result.Panel);
		Assert.Equal(ContainerLootKind.Salvage, result.Panel!.Kind);
		Assert.Equal("salvage pile", result.Panel.ContainerKindLabel);
		Assert.Equal(2, result.Panel.Rows.Count);
		Assert.Equal("Coin", result.Panel.Rows[0].DisplayName);
		Assert.Equal("Gem", result.Panel.Rows[1].DisplayName);
	}

	[Fact]
	public void TryBuildPanel_UnknownItem_ShowsPlaceholderName()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var svc = new ContainerLootInteractionService(repo, new NarrativeService(), TestPlayerProficiencyAggregation.CreateEmpty());
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents =
			[
				new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 },
				new LootableItemDefinition { ItemDefinitionId = "nope", Quantity = 1 },
			],
		};
		room.Features.Add(salvage);
		var session = SessionInRoom(room);

		var result = svc.TryBuildPanel(session, 0);

		Assert.Equal(ContainerLootErrorCode.None, result.ErrorCode);
		var unknownRow = result.Panel!.Rows.Single(r => r.ItemDefinitionId == "nope");
		Assert.Equal("?", unknownRow.DisplayName);
	}

	[Fact]
	public void TryBuildPanel_OrdinalOutOfRange_Fails()
	{
		var svc = new ContainerLootInteractionService(new MapItemRepo(), new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty());
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new SalvageFeature());
		var session = SessionInRoom(room);

		var result = svc.TryBuildPanel(session, 2);

		Assert.Equal(ContainerLootErrorCode.ContainerOrdinalOutOfRange, result.ErrorCode);
		Assert.Null(result.Panel);
	}

	[Fact]
	public void TryLootAll_TransfersAndClearsSalvage()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var svc = new ContainerLootInteractionService(repo, new NarrativeService(), TestPlayerProficiencyAggregation.CreateEmpty());
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 3 }],
		};
		room.Features.Add(salvage);
		var session = SessionInRoom(room);

		var result = svc.TryLootAll(session, 0);

		Assert.Equal(ContainerLootErrorCode.None, result.ErrorCode);
		Assert.Equal(1, result.StacksGranted);
		Assert.True(result.RemovedContainerFromRoom);
		Assert.Equal(3, session.Player.InventoryState.SumQuantityForDefinitionId("coin"));
		Assert.DoesNotContain(room.Features, f => f is SalvageFeature);
	}

	[Fact]
	public void TryLootSelected_PartialRow()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var gem = new ItemDefinition { Id = "gem", Name = "Gem", MaxStackSize = 1 };
		var repo = new MapItemRepo(coin, gem);
		var svc = new ContainerLootInteractionService(repo, new NarrativeService(), TestPlayerProficiencyAggregation.CreateEmpty());
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents =
			[
				new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 },
				new LootableItemDefinition { ItemDefinitionId = "gem", Quantity = 1 },
			],
		};
		room.Features.Add(salvage);
		var session = SessionInRoom(room);

		var result = svc.TryLootSelected(session, 0, new[] { 1 });

		Assert.Equal(ContainerLootErrorCode.None, result.ErrorCode);
		Assert.Equal(1, result.StacksGranted);
		Assert.Single(salvage.Contents);
		Assert.Equal("coin", salvage.Contents[0].ItemDefinitionId);
		Assert.Equal(1, session.Player.InventoryState.SumQuantityForDefinitionId("gem"));
	}

	[Fact]
	public void TryLootSelected_InvalidRow_Fails()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var svc = new ContainerLootInteractionService(repo, new NarrativeService(), TestPlayerProficiencyAggregation.CreateEmpty());
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 }],
		};
		room.Features.Add(salvage);
		var session = SessionInRoom(room);

		var result = svc.TryLootSelected(session, 0, new[] { 5 });

		Assert.Equal(ContainerLootErrorCode.InvalidRowSelection, result.ErrorCode);
		Assert.Equal(0, session.Player.InventoryState.SumQuantityForDefinitionId("coin"));
	}

	[Fact]
	public void TryLootSelected_EmptySelection_Fails()
	{
		var svc = new ContainerLootInteractionService(new MapItemRepo(), new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty());
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new SalvageFeature { Contents = [new LootableItemDefinition { ItemDefinitionId = "x", Quantity = 1 }] });
		var session = SessionInRoom(room);

		var result = svc.TryLootSelected(session, 0, []);

		Assert.Equal(ContainerLootErrorCode.EmptyRowSelection, result.ErrorCode);
	}

	[Fact]
	public void TryBuildPanel_NoFloor_Fails()
	{
		var svc = new ContainerLootInteractionService(new MapItemRepo(), new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty());
		var session = new GameSessionState();

		var result = svc.TryBuildPanel(session, 0);

		Assert.Equal(ContainerLootErrorCode.NoCurrentFloor, result.ErrorCode);
	}

}

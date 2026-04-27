using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class TreasurePickupServiceTests
{
	private sealed class ItemRepo : IItemDefinitionRepository
	{
		private readonly Dictionary<string, ItemDefinition> _map = new();

		public ItemRepo(params ItemDefinition[] defs)
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

	private static TreasurePickupService Service(IItemDefinitionRepository items) =>
		new(new NarrativeService(), items, TestPlayerProficiencyAggregation.CreateEmpty());

	private static GameSessionState SessionWithRoom(DungeonRoom room)
	{
		var s = new GameSessionState();
		var f = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		f.Rooms[DirectionHelper.Origin] = room;
		s.Dungeon.CurrentFloor = f;
		s.Dungeon.PlayerCoord = DirectionHelper.Origin;
		return s;
	}

	[Fact]
	public void TakeAllEligibleFromCurrentRoom_NoFloor_ReturnsNoCurrentFloor()
	{
		var session = new GameSessionState();
		var svc = Service(new ItemRepo());

		Assert.Equal(TakeTreasureOutcome.NoCurrentFloor, svc.TakeAllEligibleFromCurrentRoom(session));
	}

	[Fact]
	public void TakeTreasureInstanceAtSlot_HiddenTreasure_ReturnsNothing()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = false,
					Definition = new TreasureDefinition
					{
						Id = "gold",
						Name = "Gold",
						GrantKind = TreasureKind.Gold,
						ValueInGp = 3,
					},
				}
			]
		});
		var session = SessionWithRoom(room);
		var svc = Service(new ItemRepo());

		var outcome = svc.TakeTreasureInstanceAtSlot(session, 0, 0);

		Assert.Equal(TakeTreasureOutcome.NothingToTake, outcome);
		Assert.Equal(0, session.Player.Gold);
	}

	[Fact]
	public void TakeTreasureInstanceAtSlot_Gold_AddsGold()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition
					{
						Id = "gold",
						Name = "Gold pile",
						GrantKind = TreasureKind.Gold,
						ValueInGp = 7,
					},
				}
			]
		});
		var session = SessionWithRoom(room);
		var svc = Service(new ItemRepo());

		var outcome = svc.TakeTreasureInstanceAtSlot(session, 0, 0);

		Assert.Equal(TakeTreasureOutcome.TookItems, outcome);
		Assert.Equal(7, session.Player.Gold);
	}

	[Fact]
	public void TakeAllEligible_RemovesEmptyFeature()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var tf = new TreasureFeature
		{
			RemoveFeatureWhenEmpty = true,
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition
					{
						Id = "gold",
						Name = "Gold pile",
						GrantKind = TreasureKind.Gold,
						ValueInGp = 1,
					},
				}
			]
		};
		room.Features.Add(tf);
		var session = SessionWithRoom(room);
		var svc = Service(new ItemRepo());

		var outcome = svc.TakeAllEligibleFromCurrentRoom(session);

		Assert.Equal(TakeTreasureOutcome.TookItems, outcome);
		Assert.Empty(room.Features);
	}

	[Fact]
	public void TakeAllEligible_InventoryGrantWithMissingItemId_LogsAndSkipsItem()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition
					{
						Id = "stash",
						Name = "Mystery stash",
						GrantKind = TreasureKind.InventoryItem,
						InventoryItemId = "",
					},
				}
			]
		});
		var session = SessionWithRoom(room);
		var svc = Service(new ItemRepo());

		var outcome = svc.TakeAllEligibleFromCurrentRoom(session);

		Assert.Equal(TakeTreasureOutcome.TookItems, outcome);
		Assert.Empty(session.Player.InventoryState.Items);
		Assert.Contains(session.LogEntries, e => e.Text.Contains("no linked item id", System.StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void TakeAllEligible_InventoryGrantMissingDefinition_LogsAndSkipsItem()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition
					{
						Id = "stash",
						Name = "Mystery stash",
						GrantKind = TreasureKind.InventoryItem,
						InventoryItemId = "missing_item",
					},
				}
			]
		});
		var session = SessionWithRoom(room);
		var svc = Service(new ItemRepo());

		var outcome = svc.TakeAllEligibleFromCurrentRoom(session);

		Assert.Equal(TakeTreasureOutcome.TookItems, outcome);
		Assert.Empty(session.Player.InventoryState.Items);
		Assert.Contains(session.LogEntries, e => e.Text.Contains("no item definition exists", System.StringComparison.OrdinalIgnoreCase));
	}
}

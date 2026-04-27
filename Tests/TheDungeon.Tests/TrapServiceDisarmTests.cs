using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class TrapServiceDisarmTests
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

	private static TrapService Service(IItemDefinitionRepository items, int seed = 1) =>
		new(
			new ResolutionService(new DiceRollService(new System.Random(seed))),
			new NarrativeService(),
			new PlayerVitalsService(),
			items);

	private static TrapDefinition Trap(string id, int dc, int damage, bool removeAfterTrip = true, List<LootableItemDefinition>? disarmLoot = null) =>
		new()
		{
			Id = id,
			Name = "Trap",
			DiscoverDc = 1,
			DisarmDc = dc,
			Damage = damage,
			Effect = "Ouch.",
			IsRemovedAfterTripped = removeAfterTrip,
			DisarmLoot = disarmLoot ?? [],
		};

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
	public void TryDisarm_NoFloor_ReturnsNoCurrentFloor()
	{
		var session = new GameSessionState();
		var svc = Service(new ItemRepo());

		var result = svc.TryDisarm(session);

		Assert.Equal(TrapDisarmResultCode.NoCurrentFloor, result.ResultCode);
	}

	[Fact]
	public void TryDisarmAtSlot_BadSlot_ReturnsNoTrapPresent()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var session = SessionWithRoom(room);
		var svc = Service(new ItemRepo());

		var result = svc.TryDisarmAtSlot(session, 0, 0);

		Assert.Equal(TrapDisarmResultCode.NoTrapPresent, result.ResultCode);
	}

	[Fact]
	public void TryDisarm_Success_RemovesTrap_StagesSalvageWithRope()
	{
		var ropeDef = new ItemDefinition { Id = InventoryIds.Rope, Name = "Rope", MaxStackSize = 99 };
		var svc = Service(new ItemRepo(ropeDef), seed: 2);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var trapFeature = new TrapFeature
		{
			Traps =
			[
				new TrapInstance
				{
					IsRevealed = true,
					CurrentHp = 1,
					Definition = Trap(TrapIds.Snare, dc: -100, damage: 2,
						disarmLoot:
						[new LootableItemDefinition { ItemDefinitionId = InventoryIds.Rope, Quantity = 1 }])
				}
			]
		};
		room.Features.Add(trapFeature);
		var session = SessionWithRoom(room);
		session.Player.AbilityScores.Dexterity = 0;

		var result = svc.TryDisarm(session);

		Assert.Equal(TrapDisarmResultCode.DisarmCheckResolved, result.ResultCode);
		Assert.True(result.TrapFeatureRemoved);
		Assert.Empty(result.GrantedInventoryItemDefinitionIds);
		Assert.DoesNotContain(room.Features, f => f is TrapFeature);
		Assert.Equal(0, session.Player.InventoryState.SumQuantityForDefinitionId(InventoryIds.Rope));

		var salvage = Assert.Single(room.Features.OfType<SalvageFeature>());
		var row = Assert.Single(salvage.Contents);
		Assert.Equal(InventoryIds.Rope, row.ItemDefinitionId);
		Assert.Equal(1, row.Quantity);
	}

	[Fact]
	public void TryDisarm_Success_EmptyDisarmLoot_NoSalvage()
	{
		var svc = Service(new ItemRepo(), seed: 4);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var trapFeature = new TrapFeature
		{
			Traps =
			[
				new TrapInstance
				{
					IsRevealed = true,
					CurrentHp = 1,
					Definition = Trap("bare", dc: -100, damage: 1, disarmLoot: [])
				}
			]
		};
		room.Features.Add(trapFeature);
		var session = SessionWithRoom(room);
		session.Player.AbilityScores.Dexterity = 0;

		var result = svc.TryDisarm(session);

		Assert.Equal(TrapDisarmResultCode.DisarmCheckResolved, result.ResultCode);
		Assert.Empty(room.Features.OfType<SalvageFeature>());
	}

	[Fact]
	public void TryDisarm_Success_UnknownLootId_Omitted_ValidStillStaged()
	{
		var ropeDef = new ItemDefinition { Id = InventoryIds.Rope, Name = "Rope", MaxStackSize = 99 };
		var svc = Service(new ItemRepo(ropeDef), seed: 5);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		const string unknownId = "nope_item_xyz";
		var trapFeature = new TrapFeature
		{
			Traps =
			[
				new TrapInstance
				{
					IsRevealed = true,
					CurrentHp = 1,
					Definition = Trap("mixed", dc: -100, damage: 1,
						disarmLoot:
						[
							new LootableItemDefinition { ItemDefinitionId = unknownId, Quantity = 1 },
							new LootableItemDefinition { ItemDefinitionId = InventoryIds.Rope, Quantity = 2 },
						])
				}
			]
		};
		room.Features.Add(trapFeature);
		var session = SessionWithRoom(room);
		session.Player.AbilityScores.Dexterity = 0;

		svc.TryDisarm(session);

		Assert.Contains(session.LogEntries,
			e => e.Text.Contains($"[Loot] Item definition '{unknownId}' not found.", System.StringComparison.Ordinal));

		var salvage = Assert.Single(room.Features.OfType<SalvageFeature>());
		var row = Assert.Single(salvage.Contents);
		Assert.Equal(InventoryIds.Rope, row.ItemDefinitionId);
		Assert.Equal(2, row.Quantity);
	}

	[Fact]
	public void TryDisarm_Fail_TripsAndDamagesPlayer()
	{
		var svc = Service(new ItemRepo(), seed: 3);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var trapFeature = new TrapFeature
		{
			Traps =
			[
				new TrapInstance
				{
					IsRevealed = true,
					CurrentHp = 1,
					Definition = Trap("spike", dc: 100, damage: 4)
				}
			]
		};
		room.Features.Add(trapFeature);
		var session = SessionWithRoom(room);
		session.Player.CurrentHp = 20;
		session.Player.MaxHp = 20;
		session.Player.AbilityScores.Dexterity = 0;

		var result = svc.TryDisarm(session);

		Assert.Equal(TrapDisarmResultCode.DisarmCheckResolved, result.ResultCode);
		Assert.Equal(4, result.DamageDealtToPlayer);
		Assert.Equal(16, session.Player.CurrentHp);
	}
}

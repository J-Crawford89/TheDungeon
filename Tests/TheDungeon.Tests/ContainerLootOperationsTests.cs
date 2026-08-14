using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class ContainerLootOperationsTests
{
	private sealed class QueueRandom : Random
	{
		private readonly Queue<int> _values;

		public QueueRandom(params int[] values) => _values = new Queue<int>(values);

		public override int Next(int minValue, int maxValue)
		{
			if (_values.Count == 0)
				return minValue;
			var v = _values.Dequeue();
			return Math.Clamp(v, minValue, maxValue - 1);
		}
	}

	private static ResolutionService DefaultResolution() =>
		new ResolutionService(new DiceRollService(new Random(1)));

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

	private sealed class RecordingPresenter : IDiceRollPresenter
	{
		private readonly List<string> _events;

		public RecordingPresenter(List<string> events) => _events = events;

		public List<DicePresentationProfile> Profiles { get; } = [];
		public int SingleCalls { get; private set; }
		public int BatchCalls { get; private set; }
		public List<int> BatchSizes { get; } = [];

		public Task PresentDieAsync(
			PhysicalDieRollSpec die,
			DicePresentationProfile profile = DicePresentationProfile.Standard,
			CancellationToken ct = default) =>
			RecordSingleAsync(profile);

		public Task PresentDiceBatchAsync(
			IReadOnlyList<PhysicalDieRollSpec> dice,
			DicePresentationProfile profile = DicePresentationProfile.Standard,
			CancellationToken ct = default) =>
			RecordBatchAsync(dice.Count, profile);

		private Task RecordSingleAsync(DicePresentationProfile profile)
		{
			SingleCalls++;

			Profiles.Add(profile);
			_events.Add($"present:{profile}");
			return Task.CompletedTask;
		}

		private Task RecordBatchAsync(int count, DicePresentationProfile profile)
		{
			BatchCalls++;
			BatchSizes.Add(count);
			Profiles.Add(profile);
			_events.Add($"present:{profile}");
			return Task.CompletedTask;
		}
	}

	private sealed class RecordingReactionSink : IResolvedRollReactionSink
	{
		private readonly List<string> _events;
		private readonly string _itemDefinitionId;
		private readonly GameSessionState _session;

		public RecordingReactionSink(List<string> events, string itemDefinitionId, GameSessionState session)
		{
			_events = events;
			_itemDefinitionId = itemDefinitionId;
			_session = session;
		}

		public Task NotifyAsync(CancellationToken ct = default)
		{
			var rollLogCount = _session.LogEntries.Count(entry => entry.Kind == LogEntryKind.Roll);
			var inventoryQuantity = _session.Player.InventoryState.SumQuantityForDefinitionId(_itemDefinitionId);
			_events.Add($"react:{rollLogCount}:{inventoryQuantity}");
			return Task.CompletedTask;
		}
	}

	private static (GameSessionState Session, DungeonRoom Room, SalvageFeature Salvage) BuildHarvestContainer(
		string itemDefinitionId,
		int quantity,
		int dc)
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents =
			[
				new LootableItemDefinition
				{
					ItemDefinitionId = itemDefinitionId,
					Quantity = quantity,
					Harvest = new HarvestRequirement { HarvestDc = dc, HarvestAbility = AbilityScore.Wisdom },
				},
			],
		};
		room.Features.Add(salvage);
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		return (session, room, salvage);
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
			DefaultResolution(),
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
			DefaultResolution(),
			"remains");

		Assert.Equal(1, result.StacksGranted);
		Assert.False(result.RemovedContainerFromRoom);
		Assert.Empty(corpse.Contents);
		Assert.Contains(corpse, room.Features);
	}

	[Fact]
	public void TransferSelectedContents_TakesOneStack_LeavesOther()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var gem = new ItemDefinition { Id = "gem", Name = "Gem", MaxStackSize = 1 };
		var repo = new MapItemRepo(coin, gem);
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
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = ContainerLootOperations.TransferSelectedContents(
			session,
			room,
			salvage,
			[0],
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			DefaultResolution(),
			"salvage pile");

		Assert.Equal(1, result.StacksGranted);
		Assert.Single(salvage.Contents);
		Assert.Equal("gem", salvage.Contents[0].ItemDefinitionId);
		Assert.Equal(2, session.Player.InventoryState.SumQuantityForDefinitionId("coin"));
	}

	[Fact]
	public void TransferSelectedContents_InvalidIndex_Throws()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 }],
		};
		room.Features.Add(salvage);
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		Assert.Throws<System.ArgumentOutOfRangeException>(() =>
			ContainerLootOperations.TransferSelectedContents(
				session,
				room,
				salvage,
				[9],
				repo,
				new NarrativeService(),
				TestPlayerProficiencyAggregation.CreateEmpty(),
				DefaultResolution(),
				"pile"));
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
			DefaultResolution(),
			"heap");

		Assert.Equal(1, result.StacksGranted);
		Assert.Equal(1, result.StacksSkippedMissingDefinition);
		Assert.Single(salvage.Contents);
		Assert.Equal("nope", salvage.Contents[0].ItemDefinitionId);
		Assert.False(result.RemovedContainerFromRoom);
	}

	[Fact]
	public void TransferAllContents_Harvest_PartialSuccess_GrantsSuccessCountOnly()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents =
			[
				new LootableItemDefinition
				{
					ItemDefinitionId = "coin",
					Quantity = 7,
					Harvest = new HarvestRequirement { HarvestDc = 10, HarvestAbility = AbilityScore.Wisdom },
				},
			],
		};
		room.Features.Add(salvage);
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var dice = new DiceRollService(new QueueRandom(10, 10, 10, 10, 5, 5, 5));
		var resolution = new ResolutionService(dice);

		var result = ContainerLootOperations.TransferAllContents(
			session,
			room,
			salvage,
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			resolution,
			"salvage pile");

		Assert.Equal(1, result.StacksGranted);
		Assert.Equal(4, session.Player.InventoryState.SumQuantityForDefinitionId("coin"));
		Assert.Empty(salvage.Contents);
		Assert.True(result.RemovedContainerFromRoom);
	}

	[Fact]
	public void TransferAllContents_Harvest_AllFail_GrantsNothing()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var salvage = new SalvageFeature
		{
			Contents =
			[
				new LootableItemDefinition
				{
					ItemDefinitionId = "coin",
					Quantity = 3,
					Harvest = new HarvestRequirement { HarvestDc = 15, HarvestAbility = AbilityScore.Might },
				},
			],
		};
		room.Features.Add(salvage);
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var dice = new DiceRollService(new QueueRandom(5, 5, 5));
		var resolution = new ResolutionService(dice);

		var result = ContainerLootOperations.TransferAllContents(
			session,
			room,
			salvage,
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			resolution,
			"heap");

		Assert.Equal(0, result.StacksGranted);
		Assert.Equal(0, session.Player.InventoryState.SumQuantityForDefinitionId("coin"));
		Assert.Empty(salvage.Contents);
		Assert.True(result.RemovedContainerFromRoom);
	}

	[Fact]
	public async Task TransferAllContentsAsync_RepeatedHarvest_UsesRapidProfileAndReactsAfterEachCommittedAttempt()
	{
		var item = new ItemDefinition { Id = "tooth", Name = "Rat tooth", MaxStackSize = 99 };
		var repo = new MapItemRepo(item);
		var (session, room, salvage) = BuildHarvestContainer(item.Id, quantity: 3, dc: 10);
		var events = new List<string>();
		var presenter = new RecordingPresenter(events);
		var presenterHost = new DiceRollPresenterHost { Presenter = presenter };
		var reactionHost = new ResolvedRollReactionHost
		{
			Sink = new RecordingReactionSink(events, item.Id, session),
		};
		var resolution = new ResolutionService(
			new DiceRollService(new QueueRandom(10, 5, 10)),
			presenterHost,
			reactionHost);

		var result = await ContainerLootOperations.TransferAllContentsAsync(
			session,
			room,
			salvage,
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			resolution,
			"remains");

		Assert.Equal(1, result.StacksGranted);
		Assert.Equal(2, session.Player.InventoryState.SumQuantityForDefinitionId(item.Id));
		Assert.Equal(
			[
				DicePresentationProfile.RapidSequence,
				DicePresentationProfile.RapidSequence,
				DicePresentationProfile.RapidSequence,
			],
			presenter.Profiles);
		Assert.Equal(
			[
				"present:RapidSequence",
				"react:1:1",
				"present:RapidSequence",
				"react:2:1",
				"present:RapidSequence",
				"react:3:2",
			],
			events);
		Assert.Contains(session.LogEntries, entry => entry.Text.Contains("[1/3]", StringComparison.Ordinal));
		Assert.Contains(session.LogEntries, entry => entry.Text.Contains("[2/3]", StringComparison.Ordinal));
		Assert.Contains(session.LogEntries, entry => entry.Text.Contains("[3/3]", StringComparison.Ordinal));
		Assert.Contains(session.LogEntries, entry => entry.Text.Contains("recover 2 of 3", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task TransferAllContentsAsync_SingleHarvestAttempt_UsesStandardProfile()
	{
		var item = new ItemDefinition { Id = "tail", Name = "Rat tail", MaxStackSize = 99 };
		var repo = new MapItemRepo(item);
		var (session, room, salvage) = BuildHarvestContainer(item.Id, quantity: 1, dc: 10);
		var events = new List<string>();
		var presenter = new RecordingPresenter(events);
		var resolution = new ResolutionService(
			new DiceRollService(new QueueRandom(10)),
			new DiceRollPresenterHost { Presenter = presenter },
			new ResolvedRollReactionHost { Sink = new RecordingReactionSink(events, item.Id, session) });

		await ContainerLootOperations.TransferAllContentsAsync(
			session,
			room,
			salvage,
			repo,
			new NarrativeService(),
			TestPlayerProficiencyAggregation.CreateEmpty(),
			resolution,
			"remains");

		Assert.Equal([DicePresentationProfile.Standard], presenter.Profiles);
		Assert.Equal(["present:Standard", "react:1:1"], events);
	}

	[Fact]
	public void DicePresentationTiming_RapidSequenceUsesRelativeMultipliers()
	{
		var standard = DicePresentationTiming.For(DicePresentationProfile.Standard);
		var rapid = DicePresentationTiming.For(DicePresentationProfile.RapidSequence);

		Assert.Equal(1f, standard.PlaybackDurationMultiplier);
		Assert.Equal(1f, standard.ResultPauseMultiplier);
		Assert.Equal(0.75f, rapid.PlaybackDurationMultiplier);
		Assert.Equal(0.5f, rapid.ResultPauseMultiplier);
	}

	[Fact]
	public async Task DiceRollPresentation_TrueMultiDieRollRemainsOneBatchBoundary()
	{
		var events = new List<string>();
		var presenter = new RecordingPresenter(events);
		var specs = new List<PhysicalDieRollSpec>
		{
			new()
			{
				Kind = DieRollVisualKind.Player,
				DieType = DieType.d6,
				FaceValue = 2,
			},
			new()
			{
				Kind = DieRollVisualKind.Player,
				DieType = DieType.d6,
				FaceValue = 5,
			},
		};

		await DiceRollPresentation.PresentSpecsAsync(
			presenter,
			specs,
			DicePresentationProfile.Standard);

		Assert.Equal(0, presenter.SingleCalls);
		Assert.Equal(1, presenter.BatchCalls);
		Assert.Equal([2], presenter.BatchSizes);
		Assert.Equal([DicePresentationProfile.Standard], presenter.Profiles);
	}


	[Fact]
	public void ForContainerLootTransferStart_RespectsEmpty()
	{
		var n = new NarrativeService();
		Assert.Contains("nothing to take", n.ForContainerLootTransferStart("pile", 0));
		Assert.Contains("3 stack(s)", n.ForContainerLootTransferStart("pile", 3));
	}
}

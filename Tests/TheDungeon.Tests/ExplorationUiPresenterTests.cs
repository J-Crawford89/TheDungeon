using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public sealed class ExplorationUiPresenterTests
{
	private sealed class EmptyMonsters : IMonsterDefinitionRepository { public IReadOnlyList<MonsterDefinition> All => []; }
	private sealed class EmptyTraps : ITrapDefinitionRepository { public IReadOnlyList<TrapDefinition> All => []; }
	private sealed class EmptyTreasure : ITreasureDefinitionRepository { public IReadOnlyList<TreasureDefinition> All => []; }
	private sealed class EmptyNpc : INpcDefinitionRepository { public IReadOnlyList<NpcDefinition> All => []; }
	private sealed class EmptyLore : ILoreDefinitionRepository { public IReadOnlyList<LoreDefinition> All => []; }
	private sealed class EmptyItems : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => [];
	}

	private sealed class NoopCombatService : ICombatService
	{
		public Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel) => Task.FromResult(false);
		public bool CanAcceptPlayerAction(GameSessionState session) => false;
		public bool CanExecuteCombatAbility(GameSessionState session, string abilityId) => false;
		public Task ExecutePlayerAttackAsync(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice) => Task.CompletedTask;
		public Task ExecutePlayerFleeAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerTakeTreasureAsync(GameSessionState session, TargetPayload payload) => Task.CompletedTask;
		public Task ExecutePlayerUseHealthPotionAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerDefendAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerDisarmTrapAsync(GameSessionState session, TargetPayload payload) => Task.CompletedTask;
	}

	private sealed class RecordingLootOpener : IContainerLootOverlayOpener
	{
		public int? LastOrdinal { get; private set; }

		public void OpenLootPanel(int containerOrdinal) =>
			LastOrdinal = containerOrdinal;
	}

	private sealed class RecordingCombatService : ICombatService
	{
		private readonly List<string> _timeline;
		public Func<Task<bool>>? BeginOperation { get; init; }

		public RecordingCombatService(List<string> timeline) =>
			_timeline = timeline;

		public Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel)
		{
			_timeline.Add("combat-begin");
			return BeginOperation?.Invoke() ?? Task.FromResult(true);
		}

		public bool CanAcceptPlayerAction(GameSessionState session) => false;
		public bool CanExecuteCombatAbility(GameSessionState session, string abilityId) => false;
		public Task ExecutePlayerAttackAsync(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice) => Task.CompletedTask;
		public Task ExecutePlayerFleeAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerTakeTreasureAsync(GameSessionState session, TargetPayload payload) => Task.CompletedTask;
		public Task ExecutePlayerUseHealthPotionAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerDefendAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerDisarmTrapAsync(GameSessionState session, TargetPayload payload) => Task.CompletedTask;
	}

	private static ExplorationUiPresenter CreatePresenter(
		GameSessionState session,
		System.Action<UiRefreshFlags> refreshHud,
		IContainerLootOverlayOpener? lootOverlay = null,
		ICombatService? combat = null)
	{
		var chestLoot = new ChestLootGenerator(new EmptyItems(), ChestLootGenerationParameters.Default);
		var population = new RoomFeaturePopulationService(
			new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore(), chestLoot);
		var dice = new DiceRollService(new System.Random(1));
		var narrative = new NarrativeService();
		var inspect = new InspectService(dice, new ResolutionService(dice), narrative);
		var trapService = new TrapService(new ResolutionService(dice), narrative, new PlayerVitalsService(), new EmptyItems());
		var floorGenerator = new FloorGenerator(population);
		var exploration = new ExplorationService(floorGenerator, combat ?? new NoopCombatService(), inspect, trapService);
		var treasure = new TreasurePickupService(narrative, new EmptyItems(), TestPlayerProficiencyAggregation.CreateEmpty());
		var potionFx = new PotionEffectApplicationService(dice, narrative, new EmptyItems());
		var bootstrap = new DungeonBootstrap(
			floorGenerator,
			population,
			RoomFeaturePopulationParameters.CreateDefault(),
			() => new FloorGenerationParameters
			{
				Seed = 1,
				CurrentFloorCount = 0,
				MinRooms = 6,
				MaxRooms = 12,
				RoomFeatures = RoomFeaturePopulationParameters.CreateDefault(),
			});
		var containerLoot = new ContainerLootInteractionService(new EmptyItems(), narrative,
			TestPlayerProficiencyAggregation.CreateEmpty(), new ResolutionService(dice));
		return new ExplorationUiPresenter(
			session,
			exploration,
			narrative,
			treasure,
			containerLoot,
			trapService,
			potionFx,
			bootstrap,
			refreshHud,
			lootOverlay);
	}

	[Fact]
	public async Task OnOpenContainerWithTarget_LootContainerWithOpener_DoesNotRefresh_OpensOverlay()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var opener = new RecordingLootOpener();
		var presenter = CreatePresenter(session, f => refreshes.Add(f), opener);

		await presenter.OnOpenContainerWithTargetAsync(new TargetPayload
		{
			Kind = TargetPayloadKind.LootContainerAll,
			ContainerOrdinal = 2,
		});

		Assert.Empty(refreshes);
		Assert.Equal(2, opener.LastOrdinal);
	}

	[Fact]
	public async Task OnOpenContainerWithTarget_WithoutOverlay_InvokesRefresh_IncludingMainView()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new SalvageFeature { Contents = [] });
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var presenter = CreatePresenter(session, f => refreshes.Add(f), lootOverlay: null);

		await presenter.OnOpenContainerWithTargetAsync(new TargetPayload
		{
			Kind = TargetPayloadKind.LootContainerAll,
			ContainerOrdinal = 0,
		});

		Assert.NotEmpty(refreshes);
		Assert.Contains(refreshes, f => f.HasFlag(UiRefreshFlags.MainView));
	}

	[Fact]
	public void OnTakeWithTarget_IgnoresLootContainerAll()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		presenter.OnTakeWithTarget(new TargetPayload
		{
			Kind = TargetPayloadKind.LootContainerAll,
			ContainerOrdinal = 0,
		});

		Assert.Empty(refreshes);
	}

	[Fact]
	public async Task OnPotionPressed_InCombat_DoesNotInvokeRefresh()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		await presenter.OnPotionPressedAsync();

		Assert.Empty(refreshes);
	}

	[Fact]
	public async Task OnPotionPressed_InExploration_InvokesRefreshWithExpectedFlags()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		await presenter.OnPotionPressedAsync();

		Assert.Single(refreshes);
		var flags = refreshes[0];
		Assert.True(flags.HasFlag(UiRefreshFlags.Log));
		Assert.True(flags.HasFlag(UiRefreshFlags.Character));
		Assert.True(flags.HasFlag(UiRefreshFlags.Command));
	}

	[Fact]
	public void OnTakeWithTarget_InCombat_DoesNotInvokeRefresh()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		presenter.OnTakeWithTarget(new TargetPayload { Kind = TargetPayloadKind.TakeTreasureItem });

		Assert.Empty(refreshes);
	}

	[Fact]
	public async Task OnDisarmWithTarget_InCombat_DoesNotInvokeRefresh()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		await presenter.OnDisarmWithTargetAsync(new TargetPayload { Kind = TargetPayloadKind.DisarmTrapInstance });

		Assert.Empty(refreshes);
	}

	[Fact]
	public async Task OnForwardPressed_SuccessfulMove_RefreshesMainViewBeforeCombatBegin()
	{
		var timeline = new List<string>();
		var session = BuildLinkedNorthRooms();
		var presenter = CreatePresenter(
			session,
			f =>
			{
				if (f.HasFlag(UiRefreshFlags.MainView))
					timeline.Add("refresh-main-view");
			},
			combat: new RecordingCombatService(timeline));

		await presenter.OnForwardPressedAsync();

		Assert.Contains("refresh-main-view", timeline);
		Assert.Contains("combat-begin", timeline);
		Assert.True(timeline.IndexOf("refresh-main-view") < timeline.IndexOf("combat-begin"));
	}

	[Fact]
	public async Task OnForwardPressed_RemainsBusyUntilCombatBeginCompletes()
	{
		var timeline = new List<string>();
		var beginCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var session = BuildLinkedNorthRooms();
		var presenter = CreatePresenter(
			session,
			flags => timeline.Add($"refresh:{flags}"),
			combat: new RecordingCombatService(timeline) { BeginOperation = () => beginCompletion.Task });

		var move = presenter.OnForwardPressedAsync();
		var refreshCountWhileMoving = timeline.Count;
		await presenter.OnPotionPressedAsync();

		Assert.Equal(refreshCountWhileMoving, timeline.Count);

		beginCompletion.SetResult(true);
		await move;
		await presenter.OnPotionPressedAsync();

		Assert.True(timeline.Count > refreshCountWhileMoving);
	}

	[Fact]
	public async Task OnForwardPressed_WhenCombatBeginFaults_ReleasesBusyState()
	{
		var timeline = new List<string>();
		var session = BuildLinkedNorthRooms();
		var presenter = CreatePresenter(
			session,
			flags => timeline.Add($"refresh:{flags}"),
			combat: new RecordingCombatService(timeline)
			{
				BeginOperation = () => Task.FromException<bool>(new InvalidOperationException("test fault")),
			});

		await Assert.ThrowsAsync<InvalidOperationException>(presenter.OnForwardPressedAsync);
		var refreshCountAfterFault = timeline.Count;
		await presenter.OnPotionPressedAsync();

		Assert.True(timeline.Count > refreshCountAfterFault);
	}

	private static GameSessionState BuildLinkedNorthRooms()
	{
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var origin = new DungeonRoom { Position = DirectionHelper.Origin };
		var northCoord = DungeonNavigationHelper.GetNeighborCoord(DirectionHelper.Origin, HorizontalDirection.North);
		var north = new DungeonRoom { Position = northCoord };
		floor.Rooms[origin.Position] = origin;
		floor.Rooms[north.Position] = north;
		Assert.True(DungeonFloorLayoutService.TryLinkRooms(
			floor,
			origin.Position,
			HorizontalDirection.North,
			RoomConnectionType.Passage,
			out _));
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Player.Facing = HorizontalDirection.North;
		session.Dungeon.DiscoveredRoomsByFloor[1] = [DirectionHelper.Origin];
		return session;
	}
}

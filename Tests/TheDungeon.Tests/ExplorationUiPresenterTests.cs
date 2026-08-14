using System;
using System.Collections.Generic;
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
		public bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel) => false;
		public bool CanAcceptPlayerAction(GameSessionState session) => false;
		public bool CanExecuteCombatAbility(GameSessionState session, string abilityId) => false;
		public void ExecutePlayerAttack(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice) { }
		public void ExecutePlayerFlee(GameSessionState session) { }
		public void ExecutePlayerTakeTreasure(GameSessionState session, TargetPayload payload) { }
		public void ExecutePlayerUseHealthPotion(GameSessionState session) { }
		public void ExecutePlayerDefend(GameSessionState session) { }
		public void ExecutePlayerDisarmTrap(GameSessionState session, TargetPayload payload) { }
	}

	private sealed class RecordingLootOpener : IContainerLootOverlayOpener
	{
		public int? LastOrdinal { get; private set; }

		public void OpenLootPanel(int containerOrdinal) =>
			LastOrdinal = containerOrdinal;
	}

	private static ExplorationUiPresenter CreatePresenter(
		GameSessionState session,
		System.Action<UiRefreshFlags> refreshHud,
		IContainerLootOverlayOpener? lootOverlay = null)
	{
		var chestLoot = new ChestLootGenerator(new EmptyItems(), ChestLootGenerationParameters.Default);
		var population = new RoomFeaturePopulationService(
			new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore(), chestLoot);
		var dice = new DiceRollService(new System.Random(1));
		var narrative = new NarrativeService();
		var inspect = new InspectService(dice, new ResolutionService(dice), narrative);
		var trapService = new TrapService(new ResolutionService(dice), narrative, new PlayerVitalsService(), new EmptyItems());
		var floorGenerator = new FloorGenerator(population);
		var exploration = new ExplorationService(floorGenerator, new NoopCombatService(), inspect, trapService);
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
	public void OnOpenContainerWithTarget_LootContainerWithOpener_DoesNotRefresh_OpensOverlay()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var opener = new RecordingLootOpener();
		var presenter = CreatePresenter(session, f => refreshes.Add(f), opener);

		presenter.OnOpenContainerWithTarget(new TargetPayload
		{
			Kind = TargetPayloadKind.LootContainerAll,
			ContainerOrdinal = 2,
		});

		Assert.Empty(refreshes);
		Assert.Equal(2, opener.LastOrdinal);
	}

	[Fact]
	public void OnOpenContainerWithTarget_WithoutOverlay_InvokesRefresh_IncludingMainView()
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

		presenter.OnOpenContainerWithTarget(new TargetPayload
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
	public void OnPotionPressed_InCombat_DoesNotInvokeRefresh()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		presenter.OnPotionPressed();

		Assert.Empty(refreshes);
	}

	[Fact]
	public void OnPotionPressed_InExploration_InvokesRefreshWithExpectedFlags()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		presenter.OnPotionPressed();

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
	public void OnDisarmWithTarget_InCombat_DoesNotInvokeRefresh()
	{
		var refreshes = new List<UiRefreshFlags>();
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var presenter = CreatePresenter(session, f => refreshes.Add(f));

		presenter.OnDisarmWithTarget(new TargetPayload { Kind = TargetPayloadKind.DisarmTrapInstance });

		Assert.Empty(refreshes);
	}
}

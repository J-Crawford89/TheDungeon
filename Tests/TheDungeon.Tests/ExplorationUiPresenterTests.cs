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
		public bool IsAwaitingPlayerAction(GameSessionState session) => false;
		public void ExecutePlayerAttack(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice) { }
		public void ExecutePlayerFlee(GameSessionState session) { }
		public void ExecutePlayerTakeTreasure(GameSessionState session, TargetPayload payload) { }
		public void ExecutePlayerUseHealthPotion(GameSessionState session) { }
		public void ExecutePlayerDefend(GameSessionState session) { }
		public void ExecutePlayerDisarmTrap(GameSessionState session, TargetPayload payload) { }
	}

	private static ExplorationUiPresenter CreatePresenter(GameSessionState session, System.Action<UiRefreshFlags> refreshHud)
	{
		var population = new RoomFeaturePopulationService(new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore());
		var dice = new DiceRollService(new System.Random(1));
		var narrative = new NarrativeService();
		var inspect = new InspectService(dice, new ResolutionService(dice), narrative);
		var trapService = new TrapService(new ResolutionService(dice), narrative, new PlayerVitalsService(), new EmptyItems());
		var exploration = new ExplorationService(population, new NoopCombatService(), inspect, trapService);
		var treasure = new TreasurePickupService(narrative, new EmptyItems(), TestPlayerProficiencyAggregation.CreateEmpty());
		var potionFx = new PotionEffectApplicationService(dice, narrative, new EmptyItems());
		var bootstrap = new DungeonBootstrap(population);
		return new ExplorationUiPresenter(session, exploration, narrative, treasure, trapService, potionFx, bootstrap, refreshHud);
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

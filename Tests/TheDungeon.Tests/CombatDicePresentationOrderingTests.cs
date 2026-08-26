#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class CombatDicePresentationOrderingTests
{
	private sealed class EmptyItems : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => [];
	}

	private sealed class DeferredDicePresenter : IDiceRollPresenter
	{
		private readonly object _gate = new();
		private readonly List<TaskCompletionSource> _releases = [];
		private readonly List<IReadOnlyList<PhysicalDieRollSpec>> _presentations = [];

		public int CallCount
		{
			get
			{
				lock (_gate)
					return _releases.Count;
			}
		}

		public Task PresentDieAsync(PhysicalDieRollSpec die, DicePresentationProfile profile = DicePresentationProfile.Standard, CancellationToken ct = default) =>
			Enqueue([die], ct);

		public Task PresentDiceBatchAsync(IReadOnlyList<PhysicalDieRollSpec> dice, DicePresentationProfile profile = DicePresentationProfile.Standard, CancellationToken ct = default) =>
			Enqueue(dice, ct);

		public IReadOnlyList<PhysicalDieRollSpec> GetPresentation(int zeroBasedCallIndex)
		{
			lock (_gate)
				return _presentations[zeroBasedCallIndex];
		}

		public async Task WaitForCallCountAsync(int expected)
		{
			for (var attempt = 0; attempt < 1_000; attempt++)
			{
				lock (_gate)
				{
					if (_releases.Count >= expected)
						return;
				}
				await Task.Delay(1);
			}

			throw new TimeoutException($"Expected {expected} dice presentation call(s).");
		}

		public void Complete(int zeroBasedCallIndex)
		{
			TaskCompletionSource release;
			lock (_gate)
				release = _releases[zeroBasedCallIndex];
			release.TrySetResult();
		}

		private Task Enqueue(IReadOnlyList<PhysicalDieRollSpec> dice, CancellationToken ct)
		{
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			if (ct.CanBeCanceled)
				ct.Register(() => release.TrySetCanceled(ct));
			lock (_gate)
			{
				_presentations.Add(dice.ToArray());
				_releases.Add(release);
			}
			return release.Task;
		}
	}

	private sealed class DeferredReactionSink : IResolvedRollReactionSink
	{
		private readonly object _gate = new();
		private readonly List<TaskCompletionSource> _releases = [];

		public async Task WaitForCallCountAsync(int expected)
		{
			for (var attempt = 0; attempt < 1_000; attempt++)
			{
				lock (_gate)
				{
					if (_releases.Count >= expected)
						return;
				}
				await Task.Delay(1);
			}

			throw new TimeoutException($"Expected {expected} reaction call(s).");
		}

		public void Complete(int zeroBasedCallIndex)
		{
			TaskCompletionSource release;
			lock (_gate)
				release = _releases[zeroBasedCallIndex];
			release.TrySetResult();
		}

		public Task NotifyAsync(CancellationToken ct = default)
		{
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			if (ct.CanBeCanceled)
				ct.Register(() => release.TrySetCanceled(ct));
			lock (_gate)
				_releases.Add(release);
			return release.Task;
		}
	}

	[Fact]
	public async Task ExecutePlayerAttackAsync_WaitsForHitAndDamageDiceBeforeNarrationAndHpMutation()
	{
		var presenter = new DeferredDicePresenter();
		var host = new DiceRollPresenterHost { Presenter = presenter };
		var dice = new DiceRollService(new Random(42));
		var reaction = new DeferredReactionSink();
		var reactionHost = new ResolvedRollReactionHost { Sink = reaction };
		var resolution = new ResolutionService(dice, host, reactionHost);
		var narrative = new NarrativeService();
		var items = new EmptyItems();
		var vitals = new PlayerVitalsService();
		var combat = new CombatService(
			dice,
			resolution,
			narrative,
			vitals,
			new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>()),
			new TreasurePickupService(narrative, items, TestPlayerProficiencyAggregation.CreateEmpty()),
			new PotionEffectApplicationService(dice, narrative, items, resolution),
			new TrapService(resolution, narrative, vitals, items),
			items);
		var (session, monster) = BuildCombatSession();
		var hpBefore = monster.CurrentHp;

		var action = combat.ExecutePlayerAttackAsync(session, 0, PlayerAttackChoice.Unarmed);
		await presenter.WaitForCallCountAsync(1);

		Assert.All(presenter.GetPresentation(0), die => Assert.Equal(DieRollVisualKind.Player, die.Kind));
		Assert.Equal(hpBefore, monster.CurrentHp);
		Assert.DoesNotContain(session.LogEntries, entry => entry.Kind == LogEntryKind.Roll);
		Assert.False(combat.CanAcceptPlayerAction(session));

		presenter.Complete(0);
		await reaction.WaitForCallCountAsync(1);

		Assert.Equal(hpBefore, monster.CurrentHp);
		Assert.Contains(session.LogEntries, entry => entry.Kind == LogEntryKind.Roll);
		Assert.Equal(1, presenter.CallCount);

		reaction.Complete(0);
		await presenter.WaitForCallCountAsync(2);
		Assert.All(presenter.GetPresentation(1), die => Assert.Equal(DieRollVisualKind.Player, die.Kind));
		presenter.Complete(1);
		await reaction.WaitForCallCountAsync(2);

		Assert.True(monster.CurrentHp < hpBefore);
		Assert.Contains(session.LogEntries, entry => entry.Text.Contains("damage", StringComparison.OrdinalIgnoreCase));
		Assert.False(action.IsCompleted);

		reaction.Complete(1);
		await action;

		Assert.True(combat.CanAcceptPlayerAction(session));
	}

	[Fact]
	public async Task RollInitiativeOrderAsync_ReactsBeforePresentingTheNextCombatant()
	{
		var presenter = new DeferredDicePresenter();
		var presenterHost = new DiceRollPresenterHost { Presenter = presenter };
		var reaction = new DeferredReactionSink();
		var reactionHost = new ResolvedRollReactionHost { Sink = reaction };
		var dice = new DiceRollService(new Random(7));
		var resolution = new ResolutionService(dice, presenterHost, reactionHost);
		var initiative = new CombatInitiative(dice, new NarrativeService(), resolution);
		var session = new GameSessionState();
		var feature = new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 1,
					Definition = new MonsterDefinition { Name = "Rat" },
				},
			],
		};

		var action = initiative.RollInitiativeOrderAsync(session, feature);
		await presenter.WaitForCallCountAsync(1);
		Assert.All(presenter.GetPresentation(0), die => Assert.Equal(DieRollVisualKind.Player, die.Kind));
		presenter.Complete(0);
		await reaction.WaitForCallCountAsync(1);

		Assert.Equal(1, presenter.CallCount);
		Assert.Single(session.LogEntries, entry => entry.Kind == LogEntryKind.Roll);

		reaction.Complete(0);
		await presenter.WaitForCallCountAsync(2);
		Assert.All(presenter.GetPresentation(1), die => Assert.Equal(DieRollVisualKind.Monster, die.Kind));
		presenter.Complete(1);
		await reaction.WaitForCallCountAsync(2);

		Assert.Equal(2, session.LogEntries.Count(entry => entry.Kind == LogEntryKind.Roll));
		Assert.False(action.IsCompleted);

		reaction.Complete(1);
		var order = await action;

		Assert.Equal(2, order.Count);
	}

	private static (GameSessionState Session, MonsterInstance Monster) BuildCombatSession()
	{
		var monster = new MonsterInstance
		{
			CurrentHp = 500,
			Definition = new MonsterDefinition
			{
				Id = "ordering_target",
				Name = "Ordering Target",
				MaxHp = 500,
				Defense = -100,
				Attacks = [],
			},
		};
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature { Monsters = [monster] });
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;

		var session = new GameSessionState();
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = new CombatState
		{
			TurnOrder = [new CombatTurnSlot { IsPlayer = true }],
			CurrentTurnIndex = 0,
		};
		return (session, monster);
	}
}

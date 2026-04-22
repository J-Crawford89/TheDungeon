using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class TrapTripOnExitTests
{
	private sealed class EmptyItemRepository : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => All.OfType<T>().ToArray();
	}

	private static TrapService CreateTrapService() =>
		new(
			new ResolutionService(new DiceRollService(new System.Random(1))),
			new NarrativeService(),
			new PlayerVitalsService(),
			new EmptyItemRepository());

	private static TrapDefinition MakeTrap(string id = "t", int damage = 2) =>
		new()
		{
			Id = id,
			Name = "Test trap",
			DiscoverDc = 0,
			DisarmDc = 10,
			Damage = damage,
			Effect = "Sting.",
		};

	[Fact]
	public void ProcessTripwiresWhenExempt_Backtracking_NoDamageAndTrapRemains()
	{
		var trapService = CreateTrapService();
		var session = new GameSessionState();
		session.Player.CurrentHp = 50;

		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var trapDef = MakeTrap(damage: 5);
		var inst = new TrapInstance { Definition = trapDef, CurrentHp = 1, IsRevealed = true };
		room.Features.Add(new TrapFeature { Traps = new List<TrapInstance> { inst } });

		trapService.ProcessTrapsOnRoomExit(session, room, exemptFromTripBecauseBacktracking: true);

		Assert.Equal(50, session.Player.CurrentHp);
		Assert.Single(room.Features);
		Assert.Single(((TrapFeature)room.Features[0]).Traps);
	}

	[Fact]
	public void ProcessTripwires_NotExempt_AppliesTripAndRemovesTrap()
	{
		var trapService = CreateTrapService();
		var session = new GameSessionState();
		session.Player.CurrentHp = 50;

		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var trapDef = MakeTrap(damage: 5);
		var inst = new TrapInstance { Definition = trapDef, CurrentHp = 1, IsRevealed = true };
		room.Features.Add(new TrapFeature { Traps = new List<TrapInstance> { inst } });

		trapService.ProcessTrapsOnRoomExit(session, room, exemptFromTripBecauseBacktracking: false);

		Assert.Equal(45, session.Player.CurrentHp);
		Assert.Empty(room.Features);
		var joined = string.Join(" ", session.LogEntries.Select(e => e.Text));
		Assert.Contains("trigger", joined, System.StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ProcessTripwires_RevealsHiddenThenTripsAndRemoves()
	{
		var trapService = CreateTrapService();
		var session = new GameSessionState();
		session.Player.CurrentHp = 50;

		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var trapDef = MakeTrap(damage: 1);
		var inst = new TrapInstance { Definition = trapDef, CurrentHp = 1, IsRevealed = false };
		room.Features.Add(new TrapFeature { Traps = new List<TrapInstance> { inst } });

		trapService.ProcessTrapsOnRoomExit(session, room, exemptFromTripBecauseBacktracking: false);

		Assert.Equal(49, session.Player.CurrentHp);
		Assert.Empty(room.Features);
		var joined = string.Join(" ", session.LogEntries.Select(e => e.Text));
		Assert.Contains("hidden until now", joined, System.StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void SetIngressAfterHorizontalEnter_SetsEnteredFromOppositeFacing()
	{
		var dungeon = new DungeonState();
		dungeon.SetIngressAfterHorizontalEnter(HorizontalDirection.North);
		Assert.Equal(HorizontalDirection.South, dungeon.EnteredFromCompass);
		Assert.Equal(VerticalIngressKind.None, dungeon.VerticalIngress);
	}

	[Fact]
	public void ProcessTrapsOnExit_WhenIsRemovedAfterTrippedFalse_TrapStaysInRoom()
	{
		var trapService = CreateTrapService();
		var session = new GameSessionState();
		session.Player.CurrentHp = 50;

		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var trapDef = MakeTrap(damage: 5);
		trapDef.IsRemovedAfterTripped = false;
		var inst = new TrapInstance { Definition = trapDef, CurrentHp = 1, IsRevealed = true };
		var tf = new TrapFeature { Traps = new List<TrapInstance> { inst } };
		room.Features.Add(tf);

		trapService.ProcessTrapsOnRoomExit(session, room, exemptFromTripBecauseBacktracking: false);

		Assert.Equal(45, session.Player.CurrentHp);
		Assert.Single(room.Features);
		Assert.Single(tf.Traps);
		Assert.Same(inst, tf.Traps[0]);
	}

	[Fact]
	public void ProcessTripwires_TwoTraps_BothApply()
	{
		var trapService = CreateTrapService();
		var session = new GameSessionState();
		session.Player.CurrentHp = 50;

		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var tf = new TrapFeature
		{
			Traps =
			[
				new TrapInstance { Definition = MakeTrap("a", damage: 2), CurrentHp = 1, IsRevealed = true },
				new TrapInstance { Definition = MakeTrap("b", damage: 3), CurrentHp = 1, IsRevealed = true },
			],
		};
		room.Features.Add(tf);

		trapService.ProcessTrapsOnRoomExit(session, room, exemptFromTripBecauseBacktracking: false);

		Assert.Equal(45, session.Player.CurrentHp);
		Assert.Empty(room.Features);
	}
}

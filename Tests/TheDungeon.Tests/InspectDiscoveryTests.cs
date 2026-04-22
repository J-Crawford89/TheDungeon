using System.Collections.Generic;
using Xunit;

public sealed class InspectDiscoveryTests
{
	private sealed class FixedDiceRoll : IDiceRollRequestExecutor
	{
		private readonly DiceRollResult _result;

		public FixedDiceRoll(DiceRollResult result) => _result = result;

		public DiceRollResult Roll(DiceRollRequest request) => _result;
	}

	private static DiceRollResult BuildRoll(int total, int resolvedD20)
	{
		return new DiceRollResult
		{
			SummaryText = "fixed",
			DetailText = "1d20 [10] + 5",
			RollTotal = total,
			ModifierTotal = 0,
			Total = total,
			ResolvedD20CheckValue = resolvedD20,
			ExpressionResults = new List<DiceExpressionResult>(),
			ModifiersWithSources = new List<ModifierWithSource>(),
		};
	}

	[Fact]
	public void RunInspectDiscovery_OneRoll_ComparedToEachDiscoverDc()
	{
		var dice = new FixedDiceRoll(BuildRoll(total: 15, resolvedD20: 10));
		var resolution = new ResolutionService(dice);
		var narrative = new NarrativeService();
		var inspect = new InspectService(dice, resolution, narrative);

		var session = new GameSessionState();
		session.Player.AbilityScores.Intelligence = 2;
		session.Player.AbilityScores.Wisdom = 1;

		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var coord = DirectionHelper.Origin;
		var treasureFeature = new TreasureFeature
		{
			RemoveFeatureWhenEmpty = true,
			TreasureItems =
			[
				new TreasureInstance
				{
					Definition = new TreasureDefinition
					{
						Id = "a",
						Name = "Pouch A",
						DiscoverDc = 12,
						GrantKind = TreasureKind.Gold,
						ValueInGp = 1,
					},
					IsRevealed = false,
				},
				new TreasureInstance
				{
					Definition = new TreasureDefinition
					{
						Id = "b",
						Name = "Pouch B",
						DiscoverDc = 14,
						GrantKind = TreasureKind.Gold,
						ValueInGp = 1,
					},
					IsRevealed = false,
				},
				new TreasureInstance
				{
					Definition = new TreasureDefinition
					{
						Id = "c",
						Name = "Pouch C",
						DiscoverDc = 18,
						GrantKind = TreasureKind.Gold,
						ValueInGp = 1,
					},
					IsRevealed = false,
				},
			],
		};
		room.Features.Add(treasureFeature);

		var floor = new DungeonFloor { Level = 1, Entrance = coord };
		floor.Rooms[coord] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = coord;

		inspect.RunInspectDiscovery(session, room);

		Assert.Equal(1, room.InspectAttemptCount);
		Assert.True(treasureFeature.TreasureItems[0].IsRevealed);
		Assert.True(treasureFeature.TreasureItems[1].IsRevealed);
		Assert.False(treasureFeature.TreasureItems[2].IsRevealed);
	}

	[Fact]
	public void ResolveOutcomeAgainstTarget_Natural20_IsCriticalSuccess_VsHighDc()
	{
		var dice = new DiceRollService(new System.Random());
		var resolution = new ResolutionService(dice);

		var roll = BuildRoll(total: 5, resolvedD20: 20);

		Assert.Equal(ResolutionOutcome.CriticalSuccess, resolution.ResolveOutcomeAgainstTarget(roll, 99));
	}
}

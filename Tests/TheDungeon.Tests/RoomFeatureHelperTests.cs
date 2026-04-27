using Xunit;

public sealed class RoomFeatureHelperTests
{
	[Fact]
	public void GetFirstTrapFeatureOrdered_ReturnsFirstWithRevealedTrap()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var hiddenFirst = new TrapFeature
		{
			Traps = [new TrapInstance { IsRevealed = false, Definition = new TrapDefinition { Id = "a", Name = "A" } }]
		};
		var revealedSecond = new TrapFeature
		{
			Traps = [new TrapInstance { IsRevealed = true, Definition = new TrapDefinition { Id = "b", Name = "B" } }]
		};
		room.Features.Add(hiddenFirst);
		room.Features.Add(revealedSecond);

		var found = RoomFeatureHelper.GetFirstTrapFeatureOrdered(room);

		Assert.Same(revealedSecond, found);
	}

	[Fact]
	public void GetFeatureAndHasFeature_WorkForTypedFeatures()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature());

		Assert.True(RoomFeatureHelper.HasFeature<TreasureFeature>(room));
		Assert.NotNull(RoomFeatureHelper.GetFeature<TreasureFeature>(room));
	}
}

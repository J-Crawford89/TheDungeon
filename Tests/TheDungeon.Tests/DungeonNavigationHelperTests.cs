using Xunit;

public sealed class DungeonNavigationHelperTests
{
	private static DungeonNavigationHelper.FloorTraversalContext Ctx(bool hasRope) =>
		new() { HasRope = hasRope };

	[Fact]
	public void Hole_BlockWhenUnanchoredNoRope_NoPaired()
	{
		var exit = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = false };
		Assert.False(DungeonNavigationHelper.IsVerticalConnectionTraversable(exit, Ctx(hasRope: false)));
	}

	[Fact]
	public void Hole_AllowWhenHasRope_Unanchored()
	{
		var exit = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = false };
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(exit, Ctx(hasRope: true)));
	}

	[Fact]
	public void Hole_AllowWhenAnchored_NoRope()
	{
		var exit = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = true };
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(exit, Ctx(hasRope: false)));
	}

	[Fact]
	public void Hole_PairedOppositeAnchored_AllowsWithoutRope()
	{
		var upper = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = false };
		var lower = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = true };
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(upper, lower, Ctx(hasRope: false)));
	}
}

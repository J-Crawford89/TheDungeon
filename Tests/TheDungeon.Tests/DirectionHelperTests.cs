using Xunit;

public sealed class DirectionHelperTests
{
	[Fact]
	public void TryGetFacingAfterTurn_InvalidTurn_ReturnsFalseAndKeepsFacing()
	{
		var ok = DirectionHelper.TryGetFacingAfterTurn(HorizontalDirection.North, (DirectionTurned)999, out var next);

		Assert.False(ok);
		Assert.Equal(HorizontalDirection.North, next);
	}

	[Fact]
	public void TurnAround_North_BecomesSouth()
	{
		Assert.Equal(HorizontalDirection.South, DirectionHelper.TurnAround(HorizontalDirection.North));
	}

	[Fact]
	public void GetVerticalExitDirection_OriginIsUp_NonOriginIsDown()
	{
		Assert.Equal(VerticalDirection.Up, DirectionHelper.GetVerticalExitDirection(DirectionHelper.Origin));
		Assert.Equal(VerticalDirection.Down, DirectionHelper.GetVerticalExitDirection(new RoomCoord(1, 0)));
	}
}

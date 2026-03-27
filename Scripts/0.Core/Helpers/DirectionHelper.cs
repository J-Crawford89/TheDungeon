using System;

public static class DirectionHelper
{
	public static readonly RoomCoord Origin = new RoomCoord(0, 0);

	public static bool TryGetFacingAfterTurn(HorizontalDirection currentFacing, DirectionTurned turn, out HorizontalDirection nextFacing)
	{
		switch (turn)
		{
			case DirectionTurned.Left:
				nextFacing = TurnLeft(currentFacing);
				return true;
			case DirectionTurned.Right:
				nextFacing = TurnRight(currentFacing);
				return true;
			default:
				nextFacing = currentFacing;
				return false;
		}
	}

	public static HorizontalDirection TurnLeft(HorizontalDirection currentFacing)
	{
		var index = (int)currentFacing;
		var counterClockwiseQuarterTurns = 3;
		var countOfDirections = 4;
		return (HorizontalDirection)((index + counterClockwiseQuarterTurns) % countOfDirections);
	}

	public static HorizontalDirection TurnRight(HorizontalDirection currentFacing)
	{
		var index = (int)currentFacing;
		var clockwiseQuarterTurns = 1;
		var countOfDirections = 4;
		return (HorizontalDirection)((index + clockwiseQuarterTurns) % countOfDirections);
	}

	public static HorizontalDirection TurnAround(HorizontalDirection currentFacing)
	{
		var index = (int)currentFacing;
		var halfTurnSteps = 2;
		var countOfDirections = 4;
		return (HorizontalDirection)((index + halfTurnSteps) % countOfDirections);
	}

	public static HorizontalDirection GetOpposite(HorizontalDirection currentFacing) => TurnAround(currentFacing);

	public static VerticalDirection GetVerticalExitDirection(RoomCoord coord) => coord.Equals(Origin) ? VerticalDirection.Up : VerticalDirection.Down;
}

using System.Collections.Generic;

public sealed class RoomExits
{
	private readonly RoomConnectionType[] _connectionByDirectionIndex = new RoomConnectionType[4];

	public RoomConnectionType Get(HorizontalDirection direction) => _connectionByDirectionIndex[(int)direction];

	public void Set(HorizontalDirection direction, RoomConnectionType connection) =>
		_connectionByDirectionIndex[(int)direction] = connection;

	public IEnumerable<RoomExit> All()
	{
		const int numberOfCardinalDirections = 4;
		for (var directionIndex = 0; directionIndex < numberOfCardinalDirections; directionIndex++)
		{
			var side = (HorizontalDirection)directionIndex;
			yield return new RoomExit(side, _connectionByDirectionIndex[directionIndex]);
		}
	}
}

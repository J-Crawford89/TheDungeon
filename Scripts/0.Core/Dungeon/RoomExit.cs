public readonly struct RoomExit
{
	public HorizontalDirection Side { get; }
	public RoomConnectionType Connection { get; }

	public RoomExit(HorizontalDirection side, RoomConnectionType connection)
	{
		Side = side;
		Connection = connection;
	}
}

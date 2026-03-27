public sealed class ExplorationServiceResult
{
	public bool Success { get; private init; }
	public ExplorationErrorCode ErrorCode { get; private init; }
	public string? DiagnosticDetail { get; private init; }
	public RoomCoord? DestinationAfterMove { get; private init; }
	public int? FloorAfterMove { get; private init; }
	public HorizontalDirection? FacingAfterRotation { get; private init; }
	public InspectRoomData? InspectData { get; private init; }

	public static ExplorationServiceResult OkMoveForward(RoomCoord destination) =>
		new()
		{
			Success = true,
			ErrorCode = ExplorationErrorCode.None,
			DestinationAfterMove = destination
		};

	public static ExplorationServiceResult OkRotation(HorizontalDirection newFacing) =>
		new()
		{
			Success = true,
			ErrorCode = ExplorationErrorCode.None,
			FacingAfterRotation = newFacing
		};

	public static ExplorationServiceResult OkInspect(InspectRoomData data) =>
		new()
		{
			Success = true,
			ErrorCode = ExplorationErrorCode.None,
			InspectData = data
		};

	public static ExplorationServiceResult OkChangeFloor(int level, RoomCoord destination) =>
		new()
		{
			Success = true,
			ErrorCode = ExplorationErrorCode.None,
			FloorAfterMove = level,
			DestinationAfterMove = destination
		};

	public static ExplorationServiceResult Fail(
		ExplorationErrorCode errorCode,
		string? diagnosticDetail = null) =>
		new()
		{
			Success = false,
			ErrorCode = errorCode,
			DiagnosticDetail = diagnosticDetail
		};
}
